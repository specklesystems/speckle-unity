using Objects.Converter.Unity;
using Speckle.Core.Api;
using Speckle.Core.Api.SubscriptionModels;
using Speckle.Core.Credentials;
using Speckle.Core.Logging;
using Speckle.Core.Models;
using Speckle.Core.Transports;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

namespace Speckle.ConnectorUnity
{
  /// <summary>
  /// A Speckle Receiver, it's a wrapper around a basic Speckle Client
  /// that handles conversions and subscriptions for you
  /// </summary>
  public class Receiver : MonoBehaviour
  {
    public string StreamId;
    public int TotalChildrenCount = 0;
    private GameObject ReceivedData;

    private bool AutoReceive;
    private bool DeleteOld;
    private Action<ConcurrentDictionary<string, int>> OnProgressAction;
    private Action<string, Exception> OnErrorAction;
    private Action<int> OnTotalChildrenCountKnown;
    private Action<GameObject> OnDataReceivedAction;


    private ConverterUnity _converter = new ConverterUnity();
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
    /// Initializes the Receiver automatically, with t
    /// To be used when the StreamId property is set on the Unity ScriptableObject
    /// </summary>
    public void Init()
    {
      Client = new Client(AccountManager.GetDefaultAccount());
    }

    /// <summary>
    /// Gets and converts the data of the last commit on the Stream
    /// </summary>
    /// <returns></returns>
    public void Receive()
    {
      Task.Run(async () =>
      {
        try
        {
          var branches = await Client.StreamGetBranches(StreamId);
          var mainBranch = branches.FirstOrDefault(b => b.name == "main");
          var commit = mainBranch.commits.items[0];
          GetAndConvertObject(commit.referencedObject, commit.id);
        }
        catch (Exception e)
        {
          Log.CaptureAndThrow(e);
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
      Debug.Log("Commit created");
      GetAndConvertObject(e.objectId, e.id);
    }


    private async void GetAndConvertObject(string objectId, string commitId)
    {
      try
      {
        Tracker.TrackPageview(Tracker.RECEIVE);

        var transport = new ServerTransport(Client.Account, StreamId);
        var @base = await Operations.Receive(
          objectId,
          remoteTransport: transport,
          onErrorAction: OnErrorAction,
          onProgressAction: OnProgressAction,
          onTotalChildrenCountKnown: OnTotalChildrenCountKnown
        );
        Dispatcher.Instance().Enqueue(() =>
        {
          var go = ConvertRecursivelyToNative(@base, commitId);
          //remove previously received object
          if (DeleteOld && ReceivedData != null)
            Destroy(ReceivedData);
          ReceivedData = go;
          OnDataReceivedAction?.Invoke(go);
        });
      }
      catch (Exception e)
      {
        Log.CaptureAndThrow(e);
      }
    }


    /// <summary>
    /// Converts a Base object to a GameObject Recursively
    /// </summary>
    /// <param name="base"></param>
    /// <returns></returns>
    private GameObject ConvertRecursivelyToNative(Base @base, string name)
    {
      // case 1: it's an item that has a direct conversion method, eg a point
      if (_converter.CanConvertToNative(@base))
      {
        var go = TryConvertItemToNative(@base);
        return go;
      }

      // case 2: it's a wrapper Base
      //       2a: if there's only one member unpack it
      //       2b: otherwise return dictionary of unpacked members
      var members = @base.GetMemberNames().ToList();
      if (members.Count() == 1)
      {
        var go = RecurseTreeToNative(@base[members.First()]);
        go.name = members.First();
        return go;
      }
      else
      {
        //empty game object with the commit id as name, used to contain all the rest
        var go = new GameObject();
        go.name = name;
        foreach (var member in members)
        {
          var goo = RecurseTreeToNative(@base[member]);
          if (goo != null)
          {
            goo.name = member;
            goo.transform.parent = go.transform;
          }
        }

        return go;
      }
    }


    /// <summary>
    /// Converts an object recursively to a list of GameObjects
    /// </summary>
    /// <param name="object"></param>
    /// <returns></returns>
    private GameObject RecurseTreeToNative(object @object)
    {
      if (IsList(@object))
      {
        var list = ((IEnumerable) @object).Cast<object>();
        var objects = list.Select(x => RecurseTreeToNative(x)).Where(x => x != null).ToList();
        if (objects.Any())
        {
          var go = new GameObject();
          go.name = "List";
          objects.ForEach(x => x.transform.parent = go.transform);
          return go;
        }
      }
      else
      {
        return TryConvertItemToNative(@object);
      }

      return null;
    }

    private GameObject TryConvertItemToNative(object value)
    {
      if (value == null)
        return null;

      //it's a simple type or not a Base
      if (value.GetType().IsSimpleType() || !(value is Base))
      {
        return null;
      }

      var @base = (Base) value;

      //it's an unsupported Base, go through each of its property and try convert that
      if (!_converter.CanConvertToNative(@base))
      {
        var members = @base.GetMemberNames().ToList();
        
        //empty game object with the commit id as name, used to contain all the rest
          var go = new GameObject();
          go.name = @base.speckle_type;
        var goos = new List<GameObject>();
        foreach (var member in members)
        {
          var goo = RecurseTreeToNative(@base[member]);
          if (goo != null)
          {
            goo.name = member;
            goo.transform.parent = go.transform;
            goos.Add(goo);
          }
        }
        //if no children is valid, return null
        if (!goos.Any())
        {
          Destroy(go);
          return null;
        }

        return go;
        
      }
      else
      {
        try
        {
          return _converter.ConvertToNative(@base) as GameObject;
        }
        catch (Exception ex)
        {
          Log.CaptureAndThrow(ex);
        }
      }

      return null;
    }


    private static bool IsList(object @object)
    {
      if (@object == null)
        return false;

      var type = @object.GetType();
      return (typeof(IEnumerable).IsAssignableFrom(type) && !typeof(IDictionary).IsAssignableFrom(type) &&
              type != typeof(string));
    }

    private static bool IsDictionary(object @object)
    {
      if (@object == null)
        return false;

      Type type = @object.GetType();
      return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Dictionary<,>);
    }

    private void OnDestroy()
    {
      Client.CommitCreatedSubscription.Dispose();
    }

    #endregion
  }
}