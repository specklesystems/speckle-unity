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
  /// A Speckle Sender, it's a wrapper around a basic Speckle Client
  /// that handles conversions for you
  /// </summary>
  public static class Sender
  {


    /// <summary>
    /// Converts and sends the data of the last commit on the Stream
    /// </summary>
    /// <returns></returns>
    public static async void Send(string streamId, List<GameObject> gameObjects, Account account = null)
    {
      try
      {
        var converter = new ConverterUnity();
        var data = new Base();
        
        data["objects"] = gameObjects.Select(x => converter.ConvertToSpeckle(x)).ToList();

        var client = new Client(account ?? AccountManager.GetDefaultAccount());
        var transports = new List<ITransport>();
        transports.Add(new ServerTransport(client.Account, streamId));
        
        var objectId = await Operations.Send(data, transports);
        
        var branchName =  "main" ;

        var res = await client.CommitCreate(
          new CommitCreateInput
          {
            streamId = streamId,
            branchName = branchName,
            objectId = objectId,
            message = "Data from unity!"
          });
      }
      catch (Exception e)
      {
        Log.CaptureAndThrow(e);
      }
    }

    #region private methods

    #endregion
  }
}