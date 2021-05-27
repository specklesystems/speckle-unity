using System.Collections.Generic;
using System.Linq;
using Speckle.Core.Credentials;
using Speckle.Core.Models;

namespace Speckle.ConnectorUnity {
    public class BabyBase : Base {

        [DetachProperty]
        [Chunkable( 20000 )]
        public List<uint> values { get; set; } = new List<uint>( );

        [DetachProperty]
        [Chunkable( 20000 )]
        public List<double> vector { get; set; } = new List<double>( );

    }

    public static class BabyHelper {

        public static IEnumerable<string> GetNames( this IEnumerable<Account> accounts )
            {
                List<string> names = new List<string>( );
                if ( accounts != null ) {
                    names.AddRange(
                        from account in accounts
                        where account != null
                        select account.userInfo.name
                    );
                }
                return names;
            }

    }
}