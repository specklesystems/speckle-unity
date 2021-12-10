using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Speckle.Core.Api;
using Speckle.Core.Credentials;
using Speckle.Core.Logging;

namespace Speckle.ConnectorUnity
{
  public static class Streams
  {
    public static async Task<List<Stream>> List(int limit = 10)
    {
      Tracker.TrackPageview(Tracker.STREAM_LIST);
      var account = AccountManager.GetDefaultAccount();
      if (account == null)
        return new List<Stream>();
      var client = new Client(account);

      var res = await client.StreamsGet(limit);

      return res;
    }

    public static async Task<Stream> Get(string streamId, int limit = 10)
    {
      Tracker.TrackPageview(Tracker.STREAM_GET);
      var account = AccountManager.GetDefaultAccount();
      if (account == null)
        return null;
      var client = new Client(account);

      var res = await client.StreamGet(streamId, limit);

      if (res.branches.items != null)
      {
        res.branches.items.Reverse();
      }

      return res;
    }
  }
}