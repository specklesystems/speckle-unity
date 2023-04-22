using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Speckle.ConnectorUnity.Wrappers.Selection;
using Speckle.Core.Api;
using Speckle.Core.Credentials;
using Speckle.Core.Kits;
using Speckle.Core.Logging;
using Speckle.Core.Models;
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

        [Header("Events")]
        [HideInInspector]
        public CommitSelectionEvent OnCommitSelectionChange;
        [HideInInspector]
        public OperationProgressEvent OnReceiveProgressAction;
        [HideInInspector]
        public ErrorActionEvent OnErrorAction;
        [HideInInspector]
        public ChildrenCountHandler OnTotalChildrenCountKnown;
        [HideInInspector]
        public ReceiveCompleteHandler OnComplete;

#nullable enable
        protected internal CancellationTokenSource? CancellationTokenSource { get; private set; }

        //TODO runtime receiving
        public IEnumerator ReceiveAndConvertRoutine(SpeckleReceiver speckleReceiver, string rootObjectName, Action<Base>? beforeConvertCallback = null)
        {
            Task<Base?> receiveOperation = Task.Run(ReceiveAsync);
            
            yield return new WaitUntil(() => receiveOperation.IsCompleted);

            Base? b = receiveOperation.Result;
            if (b == null) yield break;

            //TODO make routine break for each catergory/object 
            GameObject go = ConvertToNativeWithCategories(b, rootObjectName, beforeConvertCallback);
            OnComplete.Invoke(go);
        }


        /// <summary>
        /// Receives the selected commit object using async Task
        /// </summary>
        /// <returns>Awaitable commit object</returns>
        /// <exception cref="SpeckleException">thrown when selection is incomplete</exception>
        public async Task<Base?> ReceiveAsync()
        {
            CancellationTokenSource?.Cancel();
            CancellationTokenSource?.Dispose();
            CancellationTokenSource = new CancellationTokenSource();
            if(!GetSelection(out Client? client, out Stream? stream, out Commit? commit, out string? error))
                throw new SpeckleException(error);
        
            return await ReceiveAsync(
                token: CancellationTokenSource.Token,
                client: client,
                streamId: stream.id,
                objectId: commit.referencedObject,
                commit: commit,
                onProgressAction: dict => OnReceiveProgressAction.Invoke(dict),
                onErrorAction: (m, e) => OnErrorAction.Invoke(m, e),
                onTotalChildrenCountKnown: c => OnTotalChildrenCountKnown.Invoke(c)
            ).ConfigureAwait(false);
        }

        /// <summary>
        /// Receives the requested <see cref="objectId"/> using async Task
        /// </summary>
        /// <param name="token"></param>
        /// <param name="client"></param>
        /// <param name="streamId"></param>
        /// <param name="objectId"></param>
        /// <param name="commit"></param>
        /// <param name="onProgressAction"></param>
        /// <param name="onErrorAction"></param>
        /// <param name="onTotalChildrenCountKnown"></param>
        /// <returns></returns>
        public static async Task<Base?> ReceiveAsync(CancellationToken token,
            Client client,
            string streamId,
            string objectId,
            Commit? commit,
            Action<ConcurrentDictionary<string, int>>? onProgressAction = null,
            Action<string, Exception>? onErrorAction = null,
            Action<int>? onTotalChildrenCountKnown = null)
        {
            using var transport = new ServerTransportV2(client.Account, streamId);
            
            transport.CancellationToken = token;
        
            Base? ret = null;
            try
            {

                token.ThrowIfCancellationRequested();

                ret = await Operations.Receive(
                    objectId: objectId,
                    cancellationToken: token,
                    remoteTransport: transport,
                    onProgressAction: onProgressAction,
                    onErrorAction: onErrorAction,
                    onTotalChildrenCountKnown: onTotalChildrenCountKnown,
                    disposeTransports: false
                ).ConfigureAwait(false);

                Analytics.TrackEvent(client.Account, Analytics.Events.Receive, new Dictionary<string, object>()
                {
                    {"mode", nameof(SpeckleReceiver)},
                    {"sourceHostApp", HostApplications.GetHostAppFromString(commit?.sourceApplication).Slug},
                    {"sourceHostAppVersion", commit?.sourceApplication ?? ""},
                    {"hostPlatform", Application.platform.ToString()},
                    {"isMultiplayer", commit != null && commit.authorId != client.Account.userInfo.id},
                });
                
                token.ThrowIfCancellationRequested();

                //Read receipt
                try
                {
                    await client.CommitReceived(token, new CommitReceivedInput
                    {
                        streamId = streamId,
                        commitId = commit?.id,
                        message = $"received commit from {Application.unityVersion}",
                        sourceApplication = HostApplications.Unity.GetVersion(CoreUtils.GetHostAppVersion())
                    }).ConfigureAwait(false);
                }
                catch (Exception e)
                {
                    // Do nothing!
                    Debug.LogWarning($"Failed to send read receipt\n{e}");
                }
            }
            catch (Exception e)
            {
                onErrorAction?.Invoke(e.Message, e);
            }

            return ret;
        }
    
        /// <summary>
        /// Helper method for using <see cref="RecursiveConverter"/>.
        /// Creates blank GameObjects for each property/category of the root object.
        /// </summary>
        /// <param name="base">The commitObject to convert</param>
        /// <param name="rootObjectName">The name of the parent <see cref="GameObject"/> to create</param>
        /// <param name="beforeConvertCallback">Callback for each object converted</param>
        /// <returns>The created parent <see cref="GameObject"/></returns>
        public GameObject ConvertToNativeWithCategories(Base @base, string rootObjectName,
            Action<Base>? beforeConvertCallback)
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
                if (converted.Count <= 0) continue;

                var propertyObject = new GameObject(prop.Key);
                propertyObject.transform.SetParent(rootObject.transform);
                foreach (var o in converted)
                {
                    if (o.transform.parent == null) o.transform.SetParent(propertyObject.transform);
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
        public bool GetSelection(
            [NotNullWhen(true)] out Client? client,
            [NotNullWhen(true)] out Stream? stream,
            [NotNullWhen(true)] out Commit? commit,
            [NotNullWhen(false)] out string? error)
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
        /// <param name="allAngles">when <see langword="true"/>, will fetch 360 degree preview image</param>
        /// <param name="callback">Callback function to be called when the web request completes</param>
        /// <returns><see langword="false"/> if <see cref="Account"/>, <see cref="Stream"/>, or <see cref="Commit"/> was <see langword="null"/></returns>
        public bool GetPreviewImage(/*bool allAngles,*/ Action<Texture2D?> callback)
        {
            Account? account = Account.Selected;
            if (account == null) return false;
            string? streamId = Stream.Selected?.id;
            if (streamId == null) return false;
            string? commitId = Commit.Selected?.id;
            if (commitId == null) return false;

            string angles = /*allAngles ? "all" :*/ "";
            string url = $"{account.serverInfo.url}/preview/{streamId}/commits/{commitId}/{angles}";
            string authToken = account.token;
            
            StartCoroutine(Utils.Utils.GetImageRoutine(url, authToken, callback));
            return true;
        }
        
#if UNITY_EDITOR
        [ContextMenu("Open Speckle Stream in Browser")]
        protected void OpenUrlInBrowser()
        {
            string url = GetSelectedUrl();
            Application.OpenURL(url);
        }
#endif
        public string GetSelectedUrl()
        {
            string serverUrl = Account.Selected!.serverInfo.url;
            string? streamId = Stream.Selected?.id;
            string? branchName = Branch.Selected?.name;
            string? commitId = Commit.Selected?.id;

            if (string.IsNullOrEmpty(streamId)) return serverUrl;
            if (!string.IsNullOrEmpty(commitId)) return $"{serverUrl}/streams/{streamId}/commits/{commitId}";
            if (!string.IsNullOrEmpty(branchName)) return $"{serverUrl}/streams/{streamId}/branches/{branchName}";
            return $"{serverUrl}/streams/{streamId}";
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
            Commit.OnSelectionChange = 
                () => OnCommitSelectionChange?.Invoke(Commit.Selected);
            if(Account.Options is not {Length: > 0} || forceRefresh)
                Account.RefreshOptions();
        }
        
        public void OnDestroy()
        {
            CancellationTokenSource?.Cancel();
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
    }
    
    [Serializable] public sealed class CommitSelectionEvent : UnityEvent<Commit?> { }
    [Serializable] public sealed class BranchSelectionEvent : UnityEvent<Branch?> { }
    [Serializable] public sealed class ErrorActionEvent : UnityEvent<string, Exception> { }
    [Serializable] public sealed class OperationProgressEvent : UnityEvent<ConcurrentDictionary<string, int>> { }
    [Serializable] public sealed class ReceiveCompleteHandler : UnityEvent<GameObject> { }
    [Serializable] public sealed class ChildrenCountHandler : UnityEvent<int> { }
}
