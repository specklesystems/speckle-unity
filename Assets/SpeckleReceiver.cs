using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Speckle.ConnectorUnity;
using Speckle.Core.Api;
using Speckle.Core.Credentials;
using Speckle.Core.Kits;
using Speckle.Core.Logging;
using Speckle.Core.Models;
using Speckle.Core.Transports;
using UnityEditor.VersionControl;
using UnityEngine;
using UnityEngine.Events;
using Task = System.Threading.Tasks.Task;


[ExecuteAlways]
[AddComponentMenu("Speckle/Speckle Receiver")]
[RequireComponent(typeof(RecursiveConverter))]
public class SpeckleReceiver : MonoBehaviour, ISerializationCallbackReceiver
{
    [field: SerializeReference]
    public AccountSelection Account { get; protected set; }
    
    [field: SerializeReference]
    public StreamSelection Stream { get; protected set; }
    
    [field: SerializeReference]
    public BranchSelection Branch { get; protected set; }
    
    [field: SerializeReference]
    public CommitSelection Commit { get; protected set; }

    public RecursiveConverter Converter { get; protected set; }

    private CancellationTokenSource cancellationTokenSource;

    public UnityEvent<ConcurrentDictionary<string, int>> OnProgressAction;
    public UnityEvent<string, Exception> OnErrorAction;
    public UnityEvent<int> OnTotalChildrenCountKnown;
    public UnityEvent<List<GameObject>> OnComplete;
    
#nullable enable
    public void Awake()
    {
        Initialise();
        Account!.RefreshOptions();
        Converter = GetComponent<RecursiveConverter>();
        cancellationTokenSource = new CancellationTokenSource();
    }
    
    protected void Initialise()
    {
        Account ??= new AccountSelection();
        Stream ??= new StreamSelection(Account);
        Branch ??= new BranchSelection(Stream);
        Commit ??= new CommitSelection(Branch);
        Stream.Initialise();
        Branch.Initialise();
        Commit.Initialise();
        if(Account.Options is not {Length: > 0})
            Account.RefreshOptions();
    }


    public async void ReceiveAndConvert(CancellationToken token, Transform? parentDestination = null)
    {
        Account? account = Account.Selected;
        if (account == null) throw new SpeckleException("Cannot receive: Selected Account is null");
        Client client = Account.Client ?? new Client(account); 
        Stream? stream = Stream.Selected;
        if (stream == null) throw new SpeckleException("Cannot receive: Selected Stream is null");
        Commit? commit = Commit.Selected;
        if (commit == null) throw new SpeckleException("Cannot receive: Selected Commit is null");
        
        Base? commitObject = await ReceiveAsync(
            token: token,
            client: client,
            streamId: stream.id,
            objectId: commit.referencedObject,
            commitId: commit.id,
            onProgressAction: dict => OnProgressAction.Invoke(dict),
            onErrorAction: (m, e) => OnErrorAction.Invoke(m, e),
            onTotalChildrenCountKnown: c => OnTotalChildrenCountKnown.Invoke(c)
        );
        
        Dispatcher.Instance().Enqueue(() =>
        {
            var converted = Converter.RecursivelyConvertToNative(commitObject, parentDestination);
            OnComplete.Invoke(converted);
        });
    }
    
    
    public static async Task<Base?> ReceiveAsync(CancellationToken token,
        Client client,
        string streamId,
        string objectId,
        string? commitId,
        Action<ConcurrentDictionary<string, int>>? onProgressAction = null,
        Action<string, Exception>? onErrorAction = null,
        Action<int>? onTotalChildrenCountKnown = null)
    {
        Base? ret = null;
        try
        {
            Analytics.TrackEvent(client.Account, Analytics.Events.Receive);

            if (token.IsCancellationRequested) token.ThrowIfCancellationRequested();

            var transport = new ServerTransport(client.Account, streamId);
            transport.CancellationToken = token;
            
            ret = await Operations.Receive(
                objectId: objectId,
                cancellationToken: token,
                remoteTransport: transport,
                onProgressAction: onProgressAction,
                onErrorAction: onErrorAction,
                onTotalChildrenCountKnown: onTotalChildrenCountKnown,
                disposeTransports: true
            );

            if (token.IsCancellationRequested) token.ThrowIfCancellationRequested();

            try
            {
                await client.CommitReceived(token, new CommitReceivedInput
                {
                    streamId = streamId,
                    commitId = commitId,
                    message = $"received commit from {Application.unityVersion}",
                    sourceApplication = HostApplications.Unity.GetVersion(CoreUtils.GetHostAppVersion())
                });
            }
            catch
            {
                // Do nothing!
            }
        }
        catch(Exception e)
        {
            onErrorAction?.Invoke(e.Message, e);
            throw;
        }
        
        
        return ret;
    }
    
    public void OnDestroy()
    {
        cancellationTokenSource.Cancel();
        Account?.Dispose();
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
