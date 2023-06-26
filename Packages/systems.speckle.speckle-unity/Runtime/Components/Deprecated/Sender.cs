using Speckle.Core.Api;
using Speckle.Core.Credentials;
using Speckle.Core.Logging;
using Speckle.Core.Models;
using Speckle.Core.Transports;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Sentry;
using Speckle.ConnectorUnity.Components;
using Speckle.Core.Kits;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Speckle.ConnectorUnity
{
  /// <summary>
  /// A Speckle Sender, it's a wrapper around a basic Speckle Client
  /// that handles conversions for you
  /// </summary>
  [RequireComponent(typeof(RecursiveConverter)), ExecuteAlways]
  [Obsolete]
  public class Sender : MonoBehaviour
  {
   
    private ServerTransport transport;
    private RecursiveConverter converter;
    private CancellationTokenSource cancellationTokenSource;

    #nullable enable
    
    private void Awake()
    {
      converter = GetComponent<RecursiveConverter>();
    }

    /// <summary>
    /// Converts and sends the data of the last commit on the Stream
    /// </summary>
    /// <param name="streamId">ID of the stream to send to</param>
    /// <param name="gameObjects">List of gameObjects to convert and send</param>
    /// <param name="account">Account to use. If not provided the default account will be used</param>
    /// <param name="branchName">Name of branch to send to</param>
    /// <param name="createCommit">When true, will create a commit using the root object</param>
    /// <param name="onDataSentAction">Action to run after the data has been sent</param>
    /// <param name="onProgressAction">Action to run when there is download/conversion progress</param>
    /// <param name="onErrorAction">Action to run on error</param>
    /// <exception cref="SpeckleException"></exception>
    public void Send(string streamId,
      ISet<GameObject> gameObjects,
      Account? account = null,
      string branchName = "main",
      bool createCommit = true,
      Action<string>? onDataSentAction = null,
      Action<ConcurrentDictionary<string, int>>? onProgressAction = null,
      Action<string, Exception>? onErrorAction = null)
    {
      try
      {
        CancelOperations();
        
        cancellationTokenSource = new CancellationTokenSource();
        
        var client = new Client(account ?? AccountManager.GetDefaultAccount());
        transport = new ServerTransport(client.Account, streamId);
        transport.CancellationToken = cancellationTokenSource.Token;
        
        var rootObjects = SceneManager.GetActiveScene().GetRootGameObjects();
        
        var data = converter.RecursivelyConvertToSpeckle(rootObjects,
          o => gameObjects.Contains(o));
        
        SendData(transport, data, client, branchName, createCommit, cancellationTokenSource.Token, onDataSentAction, onProgressAction, onErrorAction);
          
      }
      catch (Exception e)
      {
        throw new SpeckleException(e.ToString(), e, true, SentryLevel.Error);
      }
    }


    public static void SendData(ServerTransport remoteTransport,
      Base data,
      Client client,
      string branchName,
      bool createCommit,
      CancellationToken cancellationToken,
      Action<string>? onDataSentAction = null,
      Action<ConcurrentDictionary<string, int>>? onProgressAction = null,
      Action<string, Exception>? onErrorAction = null)
    {

      Task.Run(async () =>
      {
      
        var res = await Operations.Send(
          data,
          cancellationToken: cancellationToken,
          new List<ITransport>() {remoteTransport},
          useDefaultCache: true,
          disposeTransports: true,
          onProgressAction: onProgressAction,
          onErrorAction: onErrorAction
        );

        Analytics.TrackEvent(client.Account, Analytics.Events.Send);

        if (createCommit && !cancellationToken.IsCancellationRequested)
        {
          long count = data.GetTotalChildrenCount();
          
          await client.CommitCreate(cancellationToken,
            new CommitCreateInput
            {
              streamId = remoteTransport.StreamId,
              branchName = branchName,
              objectId = res,
              message = $"Sent {count} objects from Unity",
              sourceApplication = HostApplications.Unity.Name,
              totalChildrenCount = (int)count,
            });
        }
        
        onDataSentAction?.Invoke(res);
      }, cancellationToken);
    }

    private void OnDestroy()
    {
      CancelOperations();
    }

    public void CancelOperations()
    {
      cancellationTokenSource?.Cancel();
      transport?.Dispose();
      cancellationTokenSource?.Dispose();
    }
    

    #region private methods
    


    #endregion
  }
}
