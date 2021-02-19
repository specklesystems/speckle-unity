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
  /// A Speckle Sender, it's a wrapper around a basic Speckle Client
  /// that handles conversions for you
  /// </summary>
  public class Sender : MonoBehaviour
  {
    /// <summary>
    /// Converts and sends the data of the last commit on the Stream
    /// </summary>
    /// <returns></returns>
    public void Send(string streamId, List<GameObject> gameObjects, Account account = null,
      Action<string> onDataSentAction = null,
      Action<ConcurrentDictionary<string, int>> onProgressAction = null,
      Action<string, Exception> onErrorAction = null)
    {
      try
      {
        var data = ConvertRecursivelyToSpeckle(gameObjects);
        Task.Run(async () =>
        {
          var client = new Client(account ?? AccountManager.GetDefaultAccount());
          var transports = new List<ITransport>();
          transports.Add(new ServerTransport(client.Account, streamId));

          var objectId = await Operations.Send(data, transports,
            onErrorAction: onErrorAction,
            onProgressAction: onProgressAction);

          var branchName = "main";

          Tracker.TrackPageview(Tracker.SEND);
          var res = await client.CommitCreate(
            new CommitCreateInput
            {
              streamId = streamId,
              branchName = branchName,
              objectId = objectId,
              message = "Data from unity!"
            });

          onDataSentAction?.Invoke(res);
        });
      }
      catch (Exception e)
      {
        Log.CaptureAndThrow(e);
      }
    }

    #region private methods

    private Base ConvertRecursivelyToSpeckle(List<GameObject> gos)
    {
      if (gos.Count == 1)
      {
        return RecurseTreeToNative(gos[0]);
      }

      var @base = new Base();
      @base["objects"] = gos.Select(x => RecurseTreeToNative(x)).Where(x => x != null).ToList();
      return @base;
    }

    private Base RecurseTreeToNative(GameObject go)
    {
      var converter = new ConverterUnity();
      if (converter.CanConvertToSpeckle(go))
      {
        try
        {
          return converter.ConvertToSpeckle(go);
        }
        catch (Exception e)
        {
          Debug.LogError(e);
          return null;
        }
      }

      if (go.transform.childCount > 0)
      {
        var @base = new Base();
        var objects = new List<Base>();
        for (var i = 0; i < go.transform.childCount; i++)
        {
          var goo = RecurseTreeToNative(go.transform.GetChild(i).gameObject);
          if (goo != null)
            objects.Add(goo);
        }

        if (objects.Any())
        {
          @base["objects"] = objects;
          return @base;
        }
      }

      return null;
    }

    #endregion
  }
}