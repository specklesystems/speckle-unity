using Speckle.Core.Api;
using Speckle.Core.Api.SubscriptionModels;
using Speckle.Core.Credentials;
using Speckle.Core.Logging;
using Speckle.Core.Transports;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Sentry;
using Speckle.ConnectorUnity.Components;
using Speckle.Core.Kits;
using UnityEngine;

namespace Speckle.ConnectorUnity
{
  /// <summary>
  /// A Speckle Receiver, it's a wrapper around a basic Speckle Client
  /// that handles conversions and subscriptions for you
  /// </summary>
  [RequireComponent( typeof( RecursiveConverter ) )]
  public class Receiver : MonoBehaviour
  {
    public string StreamId;
    public string BranchName = "main";
    public Stream Stream;
    public int TotalChildrenCount = 0;
    public GameObject ReceivedData;

    private bool AutoReceive;
    private bool DeleteOld;
    private Action<ConcurrentDictionary<string, int>> OnProgressAction;
    private Action<string, Exception> OnErrorAction;
    private Action<int> OnTotalChildrenCountKnown;
    private Action<GameObject> OnDataReceivedAction;


    private Client Client { get; set; }


    public Receiver()
    {
    }

    /// <summary>
    /// Initializes the Receiver manually
    /// </summary>
    /// <param name="streamId">Id of the stream to receive</param>
    /// <param name="autoReceive">If true, it will automatically receive updates sent to this stream</param>
    /// <param name="deleteOld">If true, it will delete previously received objects when new one are received</param>
    /// <param name="account">Account to use, if null the default account will be used</param>
    /// <param name="onDataReceivedAction">Action to run after new data has been received and converted</param>
    /// <param name="onProgressAction">Action to run when there is download/conversion progress</param>
    /// <param name="onErrorAction">Action to run on error</param>
    /// <param name="onTotalChildrenCountKnown">Action to run when the TotalChildrenCount is known</param>
    public void Init(string streamId, bool autoReceive = false, bool deleteOld = true, Account account = null,
      Action<GameObject> onDataReceivedAction = null, Action<ConcurrentDictionary<string, int>> onProgressAction = null,
      Action<string, Exception> onErrorAction = null, Action<int> onTotalChildrenCountKnown = null)
    {
      StreamId = streamId;
      AutoReceive = autoReceive;
      DeleteOld = deleteOld;
      OnDataReceivedAction = onDataReceivedAction;
      OnErrorAction = onErrorAction;
      OnProgressAction = onProgressAction;
      OnTotalChildrenCountKnown = onTotalChildrenCountKnown;

      Client = new Client(account ?? AccountManager.GetDefaultAccount());


      if (AutoReceive)
      {
        Client.SubscribeCommitCreated(StreamId);
        Client.OnCommitCreated += Client_OnCommitCreated;
      }
    }


    /// <summary>
    /// Gets and converts the data of the last commit on the Stream
    /// </summary>
    /// <returns></returns>
    public void Receive()
    {
      if (Client == null || string.IsNullOrEmpty(StreamId))
        throw new Exception("Receiver has not been initialized. Please call Init().");

      Task.Run(async () =>
      {
        try
        {
          var mainBranch = await Client.BranchGet(StreamId, BranchName, 1);
          if (!mainBranch.commits.items.Any())
            throw new Exception("This branch has no commits");
          var commit = mainBranch.commits.items[0];
          GetAndConvertObject(commit.referencedObject, commit.id, commit.sourceApplication, commit.authorId);
        }
        catch (Exception e)
        {
          throw new SpeckleException(e.Message, e, true, SentryLevel.Error);
        }
      });
    }


    #region private methods

    /// <summary>
    /// Fired when a new commit is created on this stream
    /// It receives and converts the objects and then executes the user defined _onCommitCreated action.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    protected virtual void Client_OnCommitCreated(object sender, CommitInfo e)
    {
      if (e.branchName == BranchName)
      {
        Debug.Log("New commit created");
        GetAndConvertObject(e.objectId, e.id, e.sourceApplication, e.authorId);
      }
    }


    private async void GetAndConvertObject(string objectId, string commitId, string sourceApplication, string authorId)
    {
      try
      {

        var transport = new ServerTransport(Client.Account, StreamId);
        var @base = await Operations.Receive(
          objectId,
          remoteTransport: transport,
          onErrorAction: OnErrorAction,
          onProgressAction: OnProgressAction,
          onTotalChildrenCountKnown: OnTotalChildrenCountKnown,
          disposeTransports: true
        );
        
        Analytics.TrackEvent(Client.Account, Analytics.Events.Receive, new Dictionary<string, object>()
        {
            {"mode", nameof(Receiver)},
            {"sourceHostApp", HostApplications.GetHostAppFromString(sourceApplication).Slug},
            {"sourceHostAppVersion", sourceApplication ?? ""},
            {"hostPlatform", Application.platform.ToString()},
            {"isMultiplayer", authorId != null && authorId != Client.Account.userInfo.id},
        });
        
        Dispatcher.Instance().Enqueue(() =>
        {
          var root = new GameObject()
          {
            name = commitId,
          };

          var rc = GetComponent<RecursiveConverter>();
          var go = rc.RecursivelyConvertToNative(@base, root.transform);
          //remove previously received object
          if (DeleteOld && ReceivedData != null)
            Destroy(ReceivedData);
          ReceivedData = root;
          OnDataReceivedAction?.Invoke(root);
        });
      }
      catch (Exception e)
      {
        throw new SpeckleException(e.Message, e, true, SentryLevel.Error);
      }
      
      try
      {
        await Client.CommitReceived(new CommitReceivedInput
        {
          streamId = StreamId,
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


    

    private void OnDestroy()
    {
      Client?.CommitCreatedSubscription?.Dispose();
    }

    #endregion
  }
}
