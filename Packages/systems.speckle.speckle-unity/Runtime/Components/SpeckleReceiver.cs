using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Speckle.ConnectorUnity.Utils;
using Speckle.ConnectorUnity.Wrappers.Selection;
using Speckle.Core.Api;
using Speckle.Core.Credentials;
using Speckle.Core.Kits;
using Speckle.Core.Logging;
using Speckle.Core.Models;
using Speckle.Core.Models.GraphTraversal;
using Speckle.Core.Transports;
using UnityEngine;
using UnityEngine.Events;

[assembly: InternalsVisibleTo("Speckle.ConnectorUnity.Components.Editor")]

namespace Speckle.ConnectorUnity.Components
{
    [ExecuteAlways]
    [AddComponentMenu("Speckle/Speckle Receiver")]
    [RequireComponent(typeof(RecursiveConverter))]
    public class SpeckleReceiver : MonoBehaviour, ISerializationCallbackReceiver
    {
        [field: SerializeReference]
        public AccountSelection Account { get; private set; }

        [field: SerializeReference]
        public StreamSelection Stream { get; private set; }

        [field: SerializeReference]
        public BranchSelection Branch { get; private set; }

        [field: SerializeReference]
        public CommitSelection Commit { get; private set; }

        public RecursiveConverter Converter { get; private set; }

#nullable enable
        [Header("Events")]
        [HideInInspector]
        public CommitSelectionEvent OnCommitSelectionChange = new();

        [HideInInspector]
        public OperationProgressEvent OnReceiveProgressAction = new();

        [HideInInspector]
        public ErrorActionEvent OnErrorAction = new();

        [HideInInspector]
        public ChildrenCountHandler OnTotalChildrenCountKnown = new();

        [HideInInspector]
        public ReceiveCompleteHandler OnComplete = new();

        protected CancellationTokenSource? CancellationTokenSource { get; private set; }
        public CancellationToken CancellationToken => CancellationTokenSource?.Token ?? default;
        public bool IsReceiving => CancellationTokenSource != null;

        /// <summary>
        /// Cancels any current receive operations
        /// </summary>
        /// <remarks>
        /// Note, this does not cancel any currently executing ConvertToNative, just the <see cref="Operations.Receive"/>.
        /// </remarks>
        /// <returns><see langword="true"/> if the cancellation request was made. <see langword="false"/> if there was no pending operation to cancel (see <see cref="IsReceiving"/>)</returns>
        public bool Cancel()
        {
            if (CancellationTokenSource == null)
                return false;
            CancellationTokenSource.Cancel();
            return true;
        }

        /// <inheritdoc cref="ReceiveAndConvert_Async"/>
        /// <example>
        /// This function is designed to run as a coroutine i.e.
        /// <c>StartCoroutine(mySpeckleReceiver.ReceiveAndConvert_Routine());</c>
        /// </example>
        public IEnumerator ReceiveAndConvert_Routine(
            Transform? parent = null,
            Predicate<TraversalContext>? predicate = null
        )
        {
            if (IsReceiving)
            {
                OnErrorAction.Invoke(
                    "Failed to receive",
                    new InvalidOperationException("A pending receive operation has already started")
                );
                yield break;
            }

            CancellationTokenSource?.Dispose();
            CancellationTokenSource = new();

            // ReSharper disable once MethodSupportsCancellation
            Task<Base> receiveOperation = Task.Run(async () =>
            {
                Base result = await ReceiveAsync(CancellationToken);
                CancellationToken.ThrowIfCancellationRequested();
                return result;
            });

            yield return new WaitUntil(() => receiveOperation.IsCompleted);

            if (receiveOperation.IsFaulted)
            {
                OnErrorAction.Invoke("Failed to receive", receiveOperation.Exception);
                FinishOperation();
                yield break;
            }

            Base b = receiveOperation.Result;

            foreach (var _ in Converter.RecursivelyConvertToNative_Enumerable(b, parent, predicate))
            {
                yield return null;
            }

            OnComplete.Invoke(parent);
            FinishOperation();
        }

        /// <summary>
        /// Receive the selected <see cref="Commit"/> object, and converts ToNative as children of <paramref name="parent"/>
        /// </summary>
        /// <param name="parent">Optional parent <see cref="Transform"/> for the created root <see cref="GameObject"/>s</param>
        /// <param name="predicate">A filter function to allow for selectively excluding certain objects from being converted</param>
        /// <remarks>function does not throw, instead calls <see cref="OnErrorAction"/>, and calls <see cref="OnComplete"/> upon completion</remarks>
        /// <seealso cref="ReceiveAsync(System.Threading.CancellationToken)"/>
        /// <seealso cref="RecursiveConverter.RecursivelyConvertToNative_Enumerable"/>
        public async void ReceiveAndConvert_Async(
            Transform? parent = null,
            Predicate<TraversalContext>? predicate = null
        )
        {
            try
            {
                BeginOperation();
                Base commitObject = await ReceiveAsync(CancellationToken).ConfigureAwait(true);
                Converter.RecursivelyConvertToNative_Sync(commitObject, parent, predicate);
                OnComplete.Invoke(parent);
            }
            catch (Exception ex)
            {
                OnErrorAction.Invoke("Failed to receive", ex);
            }
            finally
            {
                FinishOperation();
            }
        }

        /// <summary>
        /// Receives the selected commit object using async Task
        /// </summary>
        /// <returns>Awaitable commit object</returns>
        /// <param name="cancellationToken"></param>
        /// <exception cref="SpeckleException">thrown when selection is incomplete</exception>
        /// <remarks>
        /// This function is safe to call concurrently from any threads.
        /// For this reason we use <paramref name="cancellationToken"/> parameter, rather than use the <see cref="CancellationToken"/> property
        /// <br/>
        /// Additionally, <see cref="OnComplete"/> and <see cref="OnErrorAction"/> won't be called.
        /// </remarks>
        public async Task<Base> ReceiveAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            ValidateSelection(out Client? client, out Stream? stream, out Commit? commit);

            Base result = await ReceiveAsync(
                    client: client,
                    streamId: stream.id,
                    objectId: commit.referencedObject,
                    commit: commit,
                    onProgressAction: dict => OnReceiveProgressAction.Invoke(dict),
                    onTotalChildrenCountKnown: c => OnTotalChildrenCountKnown.Invoke(c),
                    cancellationToken: cancellationToken
                )
                .ConfigureAwait(false);

            return result;
        }

        /// <summary>
        /// Gets the current selection
        /// </summary>
        /// <param name="client">The selected Account's Client</param>
        /// <param name="stream">The selected <see cref="Stream"/></param>
        /// <param name="commit">The selected <see cref="Commit"/></param>
        /// <exception cref="InvalidOperationException">Selection was not complete or invalid</exception>
        public void ValidateSelection(out Client client, out Stream stream, out Commit commit)
        {
            Client? selectedClient = Account.Client;
            client =
                selectedClient ?? throw new InvalidOperationException("Invalid Speckle account selection");

            Stream? selectedStream = Stream.Selected;
            stream =
                selectedStream ?? throw new InvalidOperationException("Invalid Speckle project selection");

            Commit? selectedCommit = Commit.Selected;
            commit =
                selectedCommit ?? throw new InvalidOperationException("Invalid Speckle version selection");
        }

        /// <summary>
        /// Starts a new receive operation with a <see cref="CancellationToken"/>
        /// </summary>
        /// <exception cref="InvalidOperationException">already receiving</exception>
        protected internal CancellationToken BeginOperation()
        {
            if (IsReceiving)
                throw new InvalidOperationException(
                    "A pending receive operation has already started"
                );

            CancellationTokenSource?.Dispose();
            CancellationTokenSource = new();

            return CancellationTokenSource.Token;
        }

        protected internal void FinishOperation()
        {
            if (!IsReceiving)
                throw new InvalidOperationException("No pending operations to finish");

            CancellationTokenSource!.Dispose();
            CancellationTokenSource = null;
        }

        /// <summary>
        /// Receives the requested <see cref="objectId"/> using async Task
        /// </summary>
        /// <param name="client"></param>
        /// <param name="streamId"></param>
        /// <param name="objectId"></param>
        /// <param name="commit"></param>
        /// <param name="onProgressAction"></param>
        /// <param name="onTotalChildrenCountKnown"></param>
        /// <param name="cancellationToken"></param>
        /// <exception cref="Exception">Throws various types of exceptions to indicate faliure</exception>
        /// <returns>The requested Speckle object</returns>
        public static async Task<Base> ReceiveAsync(
            Client client,
            string streamId,
            string objectId,
            Commit? commit,
            Action<ConcurrentDictionary<string, int>>? onProgressAction = null,
            Action<int>? onTotalChildrenCountKnown = null,
            CancellationToken cancellationToken = default
        )
        {
            using var transport = new ServerTransport(client.Account, streamId);

            transport.CancellationToken = cancellationToken;

            cancellationToken.ThrowIfCancellationRequested();

            Base requestedObject = await Operations
                .Receive(
                    objectId,
                    transport,
                    null,
                    onProgressAction,
                    onTotalChildrenCountKnown,
                    cancellationToken
                )
                .ConfigureAwait(false);

            Analytics.TrackEvent(
                client.Account,
                Analytics.Events.Receive,
                new Dictionary<string, object>()
                {
                    { "mode", nameof(SpeckleReceiver) },
                    {
                        "sourceHostApp",
                        HostApplications.GetHostAppFromString(commit?.sourceApplication).Slug
                    },
                    { "sourceHostAppVersion", commit?.sourceApplication ?? "" },
                    { "hostPlatform", Application.platform.ToString() },
                    {
                        "isMultiplayer",
                        commit?.authorId != null && commit?.authorId != client.Account?.userInfo?.id
                    },
                }
            );

            cancellationToken.ThrowIfCancellationRequested();

            //Read receipt
            try
            {
                await client
                    .CommitReceived(
                        new CommitReceivedInput
                        {
                            streamId = streamId,
                            commitId = commit?.id,
                            message = $"received commit from {Application.unityVersion}",
                            sourceApplication = HostApplications.Unity.GetVersion(
                                CoreUtils.GetHostAppVersion()
                            )
                        },
                        cancellationToken
                    )
                    .ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                // Do nothing!
                Debug.LogWarning($"Failed to send read receipt\n{ex}");
            }

            return requestedObject;
        }

        /// <summary>
        /// Helper method for using <see cref="RecursiveConverter"/>.
        /// Creates blank GameObjects for each property/category of the root object.
        /// </summary>
        /// <param name="base">The commitObject to convert</param>
        /// <param name="rootObjectName">The name of the parent <see cref="GameObject"/> to create</param>
        /// <param name="beforeConvertCallback">Callback for each object converted</param>
        /// <returns>The created parent <see cref="GameObject"/></returns>
        [Obsolete(
            "Use "
                + nameof(RecursiveConverter)
                + " Now we have implemented support for "
                + nameof(Collection)
                + "s, receiving any collection is now the default behaviour"
        )]
        public GameObject ConvertToNativeWithCategories(
            Base @base,
            string rootObjectName,
            Action<Base>? beforeConvertCallback
        )
        {
            var rootObject = new GameObject(rootObjectName);

            bool Predicate(Base o)
            {
                beforeConvertCallback?.Invoke(o);
                return Converter.ConverterInstance.CanConvertToNative(o) //Accept geometry
                    || o.speckle_type == nameof(Base) && o.totalChildrenCount > 0; // Or Base objects that have children
            }

            // For the rootObject only, we will create property GameObjects
            // i.e. revit categories
            foreach (var prop in @base.GetMembers())
            {
                var converted = Converter.RecursivelyConvertToNative(prop.Value, null, Predicate);

                //Skip empties
                if (converted.Count <= 0)
                    continue;

                var propertyObject = new GameObject(prop.Key);
                propertyObject.transform.SetParent(rootObject.transform);
                foreach (var o in converted)
                {
                    if (o.transform.parent == null)
                        o.transform.SetParent(propertyObject.transform);
                }
            }

            return rootObject;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="client"></param>
        /// <param name="stream"></param>
        /// <param name="commit"></param>
        /// <param name="error">error messages for </param>
        /// <returns>true if selection is complete, as we are ready to receive</returns>
        [Obsolete("Use " + nameof(ValidateSelection))]
        public bool GetSelection(
            [NotNullWhen(true)] out Client? client,
            [NotNullWhen(true)] out Stream? stream,
            [NotNullWhen(true)] out Commit? commit,
            [NotNullWhen(false)] out string? error
        )
        {
            Account? account = Account.Selected;
            stream = Stream.Selected;
            commit = Commit.Selected;

            if (account == null)
            {
                error = "Selected Account is null";
                client = null;
                return false;
            }
            client = Account.Client ?? new Client(account);

            if (stream == null)
            {
                error = "Selected Stream is null";
                return false;
            }

            if (commit == null)
            {
                error = "Selected Commit is null";
                return false;
            }
            error = null;
            return true;
        }

        /// <summary>
        /// Fetches the commit preview for the currently selected commit
        /// </summary>
        /// <param name="callback">Callback function to be called when the web request completes</param>
        /// <returns>The executing <see cref="Coroutine"/> or <see langword="null"/> if <see cref="Account"/>, <see cref="Stream"/>, or <see cref="Commit"/> was <see langword="null"/></returns>
        public Coroutine? GetPreviewImage(Action<Texture2D?> callback)
        {
            Account? account = Account.Selected;
            if (account == null)
                return null;
            string? streamId = Stream.Selected?.id;
            if (streamId == null)
                return null;
            string? commitId = Commit.Selected?.id;
            if (commitId == null)
                return null;

            string angles = /*allAngles ? "all" :*/
                "";
            string url = $"{account.serverInfo.url}/preview/{streamId}/commits/{commitId}/{angles}";
            string authToken = account.token;

            return StartCoroutine(Utils.Utils.GetImageRoutine(url, authToken, callback));
        }

#if UNITY_EDITOR
        [ContextMenu("Open Speckle Model in Browser")]
        protected void OpenUrlInBrowser()
        {
            Uri url = GetSelectedUrl();
            Application.OpenURL(url.ToString());
        }
#endif

        public Uri GetSelectedUrl()
        {
            Account selectedAccount = Account.Selected!;
            StreamWrapper sw =
                new()
                {
                    ServerUrl = selectedAccount.serverInfo.url,
                    StreamId = Stream.Selected!.id,
                    BranchName = Branch.Selected?.id,
                    CommitId = Commit.Selected?.id
                };
            sw.SetAccount(selectedAccount);

            return sw.ToServerUri();
        }

        public void Awake()
        {
            Converter = GetComponent<RecursiveConverter>();
            Initialise(true);
        }

        protected void Initialise(bool forceRefresh = false)
        {
            CoreUtils.SetupInit();
            Account ??= new AccountSelection();
            Stream ??= new StreamSelection(Account);
            Branch ??= new BranchSelection(Stream);
            Commit ??= new CommitSelection(Branch);
            Stream.Initialise();
            Branch.Initialise();
            Commit.Initialise();
            Commit.OnSelectionChange = () => OnCommitSelectionChange?.Invoke(Commit.Selected);
            if (Account.Options is not { Count: > 0 } || forceRefresh)
                Account.RefreshOptions();
        }

        public void OnDisable()
        {
            CancellationTokenSource?.Cancel();
        }

        public void OnDestroy()
        {
            CancellationTokenSource?.Dispose();
        }

        public void OnBeforeSerialize()
        {
            //pass
        }

        public void OnAfterDeserialize()
        {
            Initialise();
        }

        #region Deprecated members

        [Obsolete("use " + nameof(ReceiveAndConvert_Routine), true)]
        public IEnumerator ReceiveAndConvertRoutine(
            SpeckleReceiver speckleReceiver,
            string rootObjectName,
            Action<Base>? beforeConvertCallback = null
        )
        {
            // ReSharper disable once MethodSupportsCancellation
            Task<Base> receiveOperation = Task.Run(
                async () => await ReceiveAsync(CancellationToken)
            );

            yield return new WaitUntil(() => receiveOperation.IsCompleted);

            Base? b = receiveOperation.Result;
            if (b == null)
                yield break;

            //NOTE: coroutine doesn't break for each catergory/object
            ConvertToNativeWithCategories(b, rootObjectName, beforeConvertCallback);
        }

        #endregion
    }

    [Serializable]
    public sealed class CommitSelectionEvent : UnityEvent<Commit?> { }

    [Serializable]
    public sealed class BranchSelectionEvent : UnityEvent<Branch?> { }

    [Serializable]
    public sealed class ErrorActionEvent : UnityEvent<string, Exception> { }

    [Serializable]
    public sealed class OperationProgressEvent : UnityEvent<ConcurrentDictionary<string, int>> { }

    [Serializable]
    public sealed class ReceiveCompleteHandler : UnityEvent<Transform?> { }

    [Serializable]
    public sealed class ChildrenCountHandler : UnityEvent<int> { }
}
