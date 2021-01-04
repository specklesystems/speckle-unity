using Objects.Converter.Unity;
using Speckle.Core.Api;
using Speckle.Core.Api.SubscriptionModels;
using Speckle.Core.Credentials;
using Speckle.Core.Logging;
using Speckle.Core.Models;
using Speckle.Core.Transports;
using System;
using System.Collections;
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
  public class Receiver : ScriptableObject
  {
    public string StreamId { get; private set; }

    public delegate void DataReceivedHandler(GameObject data);

    public event DataReceivedHandler OnNewData;

    private ConverterUnity _converter = new ConverterUnity();
    private Client Client { get; set; }


    public Receiver()
    {
    }

    public void Init(string streamId, Account account = null)
    {
      StreamId = streamId;

      Client = new Client(account ?? AccountManager.GetDefaultAccount());
      Client.SubscribeCommitCreated(StreamId);
      Client.OnCommitCreated += Client_OnCommitCreated;
    }

    /// <summary>
    /// Gets and converts the the data of the last commit on the Stream
    /// </summary>
    /// <returns></returns>
    public async Task<GameObject> Receive()
    {
      try
      {
        var res = await Client.StreamGet(StreamId);
        var mainBranch = res.branches.items.FirstOrDefault(b => b.name == "main");
        var commit = mainBranch.commits.items[0];
        return await GetAndConvertObject(commit.referencedObject, commit.id);
      }
      catch (Exception e)
      {
        Log.CaptureAndThrow(e);
      }

      return null;
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

      if (OnNewData == null)
        return;
      
      //Run on a dispatcher as GOs can only be created on the main thread
      Dispatcher.Instance().EnqueueAsync(async () =>
      {
        var go = await GetAndConvertObject(e.objectId, e.id);
        OnNewData?.Invoke(go);
      });
    }


    private async Task<GameObject> GetAndConvertObject(string objectId, string commitId)
    {
      try
      {
        Debug.Log("test");
        var transport = new ServerTransport(Client.Account, StreamId);
        var @base = await Operations.Receive(
          objectId,
          remoteTransport: transport
        );

        return ConvertRecursivelyToNative(@base, commitId);
      }
      catch (Exception e)
      {
        Log.CaptureAndThrow(e);
      }

      return null;
    }


    /// <summary>
    /// Converts a Base object to a parent GameObject
    /// </summary>
    /// <param name="base"></param>
    /// <returns></returns>
    private GameObject ConvertRecursivelyToNative(Base @base, string name)
    {
      var go = new GameObject();
      go.name = name;

      var convertedObjects = new List<GameObject>();
      // case 1: it's an item that has a direct conversion method, eg a point
      if (_converter.CanConvertToNative(@base))
      {
        convertedObjects = TryConvertItemToNative(@base);
      }
      else
      {
        // case 2: it's a wrapper Base
        //       2a: if there's only one member unpack it
        //       2b: otherwise return dictionary of unpacked members
        var members = @base.GetDynamicMembers().ToList();


        if (members.Count() == 1)
        {
          convertedObjects = RecurseTreeToNative(@base[members.First()]);
        }
        else
        {
          convertedObjects = members.SelectMany(x => RecurseTreeToNative(@base[x])).ToList();
        }
      }


      convertedObjects.Where(x => x != null).ToList().ForEach(x => x.transform.parent = go.transform);

      return go;
    }


    /// <summary>
    /// Converts an object recursively to a list of GameObjects
    /// </summary>
    /// <param name="object"></param>
    /// <returns></returns>
    private List<GameObject> RecurseTreeToNative(object @object)
    {
      var objects = new List<GameObject>();
      if (IsList(@object))
      {
        var list = ((IEnumerable) @object).Cast<object>();
        objects = list.SelectMany(x => RecurseTreeToNative(x)).ToList();
      }
      else
      {
        objects = TryConvertItemToNative(@object);
      }

      return objects;
    }

    private List<GameObject> TryConvertItemToNative(object value)
    {
      var objects = new List<GameObject>();

      if (value == null)
        return objects;

      //it's a simple type or not a Base
      if (value.GetType().IsSimpleType() || !(value is Base))
      {
        return objects;
      }

      var @base = (Base) value;

      //it's an unsupported Base, return a dictionary
      if (!_converter.CanConvertToNative(@base))
      {
        var members = @base.GetMembers().Values.ToList();


        objects = members.SelectMany(x => RecurseTreeToNative(x)).ToList();
      }
      else
      {
        try
        {
          var converted = _converter.ConvertToNative(@base) as GameObject;
          objects.Add(converted);
        }
        catch (Exception ex)
        {
          Log.CaptureAndThrow(ex);
        }
      }

      return objects;
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