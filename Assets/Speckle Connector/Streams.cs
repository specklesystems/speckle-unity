using System;
using System.Collections.Generic;
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
  }
}

