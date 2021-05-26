using System.Collections.Generic;
using System.Linq;
using Speckle.Core.Api;

namespace Speckle.ConnectorUnity {
    public readonly struct SpeckleSimpleInfo {

        public SpeckleSimpleInfo( string name )
            {
                this.name = name;
            }

        public string name { get; }

    }

    public static class Ayuda {


        public static List<SpeckleSimpleInfo> GetInfo( this IEnumerable<Stream> input )
            {
                return (
                    from i in input
                    where i != null
                    select new SpeckleSimpleInfo( i.name ) ).ToList( );
            }

        public static List<SpeckleSimpleInfo> GetInfo( this IEnumerable<Branch> input )
            {
                return (
                    from i in input
                    where i != null
                    select new SpeckleSimpleInfo( i.name ) ).ToList( );
            }
        
        public static List<SpeckleSimpleInfo> GetInfo( this IEnumerable<Commit> input )
            {
                return (
                    from i in input
                    where i != null
                    select new SpeckleSimpleInfo( i.id ) ).ToList( );
            }
    }
}