using System;
using System.Collections;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Speckle.ConnectorUnity.Components;
using Speckle.ConnectorUnity.Utils;
using Speckle.Core.Api;
using Speckle.Core.Credentials;
using Speckle.Core.Logging;
using Speckle.Core.Models;
using UnityEngine;

[AddComponentMenu("Speckle/Extras/Receive From Url")]
[RequireComponent(typeof(RecursiveConverter)), ExecuteAlways]
public class ReceiveFromURL : MonoBehaviour
{
    [Tooltip("Url of your speckle object/commit/branch/stream")]
    public string url;
    
    private RecursiveConverter _converter;
    
#nullable enable
    private CancellationTokenSource? _tokenSource;
    void Awake()
    {
        _converter = GetComponent<RecursiveConverter>();
    }

    [ContextMenu(nameof(Receive))]
    public void Receive()
    {
        StartCoroutine(Receive_Routine());
    }
    
    public IEnumerator Receive_Routine()
    {
        if (IsBusy()) throw new InvalidOperationException("A receive operation has already started");
        _tokenSource = new CancellationTokenSource();
        try
        {
            StreamWrapper sw = new(url);

            if (!sw.IsValid)
                throw new InvalidOperationException("Speckle url input is not a valid speckle stream/branch/commit");

            var accountTask = new Utils.WaitForTask<Account>(async () => await GetAccount(sw));
            yield return accountTask;
            
            _tokenSource.Token.ThrowIfCancellationRequested();
            using Client c = new(accountTask.Result);

            var objectIdTask = new Utils.WaitForTask<(string, Commit?)>(async () => await GetObjectID(sw, c));
            yield return objectIdTask;
            (string objectId, Commit? commit) = objectIdTask.Result;
            
            Debug.Log($"Receiving from {sw.ServerUrl}...");
            
            var receiveTask = new Utils.WaitForTask<Base>(async () => await SpeckleReceiver.ReceiveAsync(
                c,
                sw.StreamId,
                objectId,
                commit,
                cancellationToken: _tokenSource.Token));
            yield return receiveTask;
            
            Debug.Log("Converting to native...");
            _converter.RecursivelyConvertToNative_Sync(receiveTask.Result, transform);
        }
        finally
        {
            _tokenSource.Dispose();
            _tokenSource = null;
        }
    }


    private async Task<(string objectId, Commit? commit)> GetObjectID(StreamWrapper sw, Client client)
    {
        string objectId;
        Commit? commit = null;
        //OBJECT URL
        if (!string.IsNullOrEmpty(sw.ObjectId))
        {
            objectId = sw.ObjectId;
        }
        //COMMIT URL
        else if (!string.IsNullOrEmpty(sw.CommitId))
        {
            commit = await client.CommitGet(sw.StreamId, sw.CommitId).ConfigureAwait(false);
            objectId = commit.referencedObject;
        }
        //BRANCH URL OR STREAM URL
        else
        {
            var branchName = string.IsNullOrEmpty(sw.BranchName) ? "main" : sw.BranchName;

            var branch = await client.BranchGet(sw.StreamId, branchName, 1).ConfigureAwait(false);
            if (!branch.commits.items.Any())
                throw new SpeckleException("The selected branch has no commits.");

            commit = branch.commits.items[0];
            objectId = branch.commits.items[0].referencedObject;
        }

        return (objectId, commit);
    }
    [ContextMenu(nameof(Cancel))]
    public void Cancel()
    {
        if (IsNotBusy()) throw new InvalidOperationException("There are no pending receive operations to cancel");
        _tokenSource!.Cancel();
    }
    
    [ContextMenu(nameof(Cancel), true)]
    public bool IsBusy()
    {
        return _tokenSource is not null;
    }
    
    [ContextMenu(nameof(Receive), true)]
    internal bool IsNotBusy() => !IsBusy();

    private void OnDisable()
    {
        _tokenSource?.Cancel();
    }


    private async Task<Account> GetAccount(StreamWrapper sw)
    {
        Account account;
        try
        {
            account = await sw.GetAccount().ConfigureAwait(false);
        }
        catch (SpeckleException e)
        {
            if (string.IsNullOrEmpty(sw.StreamId))
                throw e;

            //Fallback to a non authed account
            account = new Account
            {
                token = "",
                serverInfo = new ServerInfo { url = sw.ServerUrl },
                userInfo = new UserInfo()
            };
        }

        return account;
    }
}
