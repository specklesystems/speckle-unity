using System;
using Objects.BuiltElements;

namespace Speckle.ConnectorUnity {


    [Serializable]
    public class UnityRevitLevel : Level { }

    [Serializable]
    public class SpeckleRevitLevel : SpeckleData<UnityRevitLevel> {

        public UnityRevitLevel RevitLevel = new UnityRevitLevel( );

    }


    [Serializable]
    public class SpeckleData<TBase> { }
}