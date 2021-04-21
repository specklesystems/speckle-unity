using System.Collections.Generic;
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
}