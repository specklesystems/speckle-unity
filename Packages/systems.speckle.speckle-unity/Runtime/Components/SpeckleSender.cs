using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Speckle.ConnectorUnity.Utils;
using Speckle.ConnectorUnity.Wrappers.Selection;
using Speckle.Core.Api;
using Speckle.Core.Credentials;
using Speckle.Core.Kits;
using Speckle.Core.Logging;
using Speckle.Core.Models;
using Speckle.Core.Transports;
using UnityEngine;

namespace Speckle.ConnectorUnity.Components
{
    [ExecuteAlways]
    [AddComponentMenu("Speckle/Speckle Sender")]
    [RequireComponent(typeof(RecursiveConverter))]
    public class SpeckleSender : MonoBehaviour, ISerializationCallbackReceiver
    {
        [field: SerializeReference]
        public AccountSelection Account { get; private set; }

        [field: SerializeReference]
        public StreamSelection Stream { get; private set; }

        [field: SerializeReference]
        public BranchSelection Branch { get; private set; }

        public RecursiveConverter Converter { get; private set; }

        [Header("Events")]
        [HideInInspector]
        public BranchSelectionEvent OnBranchSelectionChange;

        [Obsolete("No longer used")]
        [HideInInspector]
        public ErrorActionEvent OnErrorAction;

        [HideInInspector]
        public OperationProgressEvent OnSendProgressAction;
#nullable enable
        protected internal CancellationTokenSource? CancellationTokenSource { get; private set; }

        //TODO runtime sending

        public async Task<string> SendDataAsync(Base data, bool createCommit)
        {
            CancellationTokenSource?.Cancel();
            CancellationTokenSource?.Dispose();
            CancellationTokenSource = new CancellationTokenSource();
            if (
                !GetSelection(
                    out Client? client,
                    out Stream? stream,
                    out Branch? branch,
                    out string? error
                )
            )
                throw new SpeckleException(error);

            using ServerTransport transport = new(client.Account, stream.id);
            transport.CancellationToken = CancellationTokenSource.Token;

            return await SendDataAsync(
                remoteTransport: transport,
                data,
                client,
                branch.id,
                createCommit,
                dict => OnSendProgressAction.Invoke(dict),
                CancellationTokenSource.Token
            );
        }

        /// <param name="remoteTransport">The transport to send to</param>
        /// <param name="data">The data to send</param>
        /// <param name="client">An authenticated Speckle Client</param>
        /// <param name="branchId">The branch name or id</param>
        /// <param name="createCommit">when <see langword="true"/> will call <see cref="Client.CommitCreate"/>, otherwise only the object data is sent</param>
        /// <param name="onProgressAction">Called every progress tick of the <see cref="Operations.Send"/></param>
        /// <param name="cancellationToken">Optional cancellation token</param>
        /// <returns>The id (hash) of the object sent</returns>
        public static async Task<string> SendDataAsync(
            ServerTransport remoteTransport,
            Base data,
            Client client,
            string branchId,
            bool createCommit,
            Action<ConcurrentDictionary<string, int>>? onProgressAction = null,
            CancellationToken cancellationToken = default
        )
        {
            string res = await Operations.Send(
                data,
                remoteTransport,
                true,
                onProgressAction,
                cancellationToken
            );

            Analytics.TrackEvent(
                client.Account,
                Analytics.Events.Send,
                new Dictionary<string, object>()
                {
                    { "mode", nameof(SpeckleSender) },
                    { "hostPlatform", Application.platform.ToString() },
                }
            );

            if (createCommit && !cancellationToken.IsCancellationRequested)
            {
                string streamId = remoteTransport.StreamId;
                string unityVer = $"Unity {Application.unityVersion.Substring(0, 6)}";
                data.totalChildrenCount = data.GetTotalChildrenCount();
                string commitMessage = $"Sent {data.totalChildrenCount} objects from {unityVer}";

                string commitId = await CreateCommit(
                    data,
                    client,
                    streamId,
                    branchId,
                    res,
                    commitMessage,
                    cancellationToken
                );

                StreamWrapper sw =
                    new()
                    {
                        ServerUrl = client.ServerUrl,
                        StreamId = streamId,
                        BranchName = branchId,
                        CommitId = commitId,
                    };
                sw.SetAccount(client.Account);

                string url = sw.ToServerUri().GetLeftPart(UriPartial.Path);
                Debug.Log($"Data successfully sent to <a href=\"{url}\">{url}</a>");
            }

            return res;
        }

        public static async Task<string> CreateCommit(
            Base data,
            Client client,
            string streamId,
            string branchName,
            string objectId,
            string message,
            CancellationToken cancellationToken
        )
        {
            string commitId = await client.CommitCreate(
                new CommitCreateInput
                {
                    streamId = streamId,
                    branchName = branchName,
                    objectId = objectId,
                    message = message,
                    sourceApplication = HostApplications.Unity.GetVersion(
                        CoreUtils.GetHostAppVersion()
                    ),
                    totalChildrenCount = (int)data.totalChildrenCount,
                },
                cancellationToken
            );

            return commitId;
        }

        public bool GetSelection(
            [NotNullWhen(true)] out Client? client,
            [NotNullWhen(true)] out Stream? stream,
            [NotNullWhen(true)] out Branch? branch,
            [NotNullWhen(false)] out string? error
        )
        {
            Account? account = Account.Selected;
            stream = Stream.Selected;
            branch = Branch.Selected;

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

            if (branch == null)
            {
                error = "Selected Branch is null";
                return false;
            }
            error = null;
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

            if (string.IsNullOrEmpty(streamId))
                return serverUrl;
            if (!string.IsNullOrEmpty(branchName))
                return $"{serverUrl}/streams/{streamId}/branches/{branchName}";
            return $"{serverUrl}/streams/{streamId}";
        }

        public void Awake()
        {
            Initialise(true);
            Converter = GetComponent<RecursiveConverter>();
        }

        protected void Initialise(bool forceRefresh = false)
        {
            CoreUtils.SetupInit();
            Account ??= new AccountSelection();
            Stream ??= new StreamSelection(Account);
            Branch ??= new BranchSelection(Stream);
            Branch.CommitsLimit = 0;
            Stream.Initialise();
            Branch.Initialise();
            Branch.OnSelectionChange = () => OnBranchSelectionChange?.Invoke(Branch.Selected);
            if (Account.Options is not { Count: > 0 } || forceRefresh)
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

        [Obsolete("use other overload")]
        public static async Task<string> SendDataAsync(
            CancellationToken cancellationToken,
            ServerTransport remoteTransport,
            Base data,
            Client client,
            string branchName,
            bool createCommit,
            Action<ConcurrentDictionary<string, int>>? onProgressAction = null,
            Action<string, Exception>? onErrorAction = null
        )
        {
            return await SendDataAsync(
                remoteTransport,
                data,
                client,
                branchName,
                createCommit,
                onProgressAction,
                cancellationToken
            );
        }
    }
}
