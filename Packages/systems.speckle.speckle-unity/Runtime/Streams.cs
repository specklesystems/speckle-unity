using System.Collections.Generic;
using System.Threading.Tasks;
using Speckle.Core.Api;
using Speckle.Core.Credentials;

namespace Speckle.ConnectorUnity
{
    public static class Streams
    {
        public static async Task<List<Stream>> List(int limit = 10)
        {
            var account = AccountManager.GetDefaultAccount();
            if (account == null)
                return new List<Stream>();
            var client = new Client(account);

            var res = await client.StreamsGet(limit);

            return res;
        }

        public static async Task<Stream> Get(string streamId, int limit = 10)
        {
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
