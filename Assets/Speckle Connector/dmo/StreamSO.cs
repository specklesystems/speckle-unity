using UnityEngine;

namespace Speckle_Connector.dmo {
    [CreateAssetMenu( fileName = "SpeckleStream", menuName = "Speckle2/Stream Scriptable Object", order = 0 )]

    public class StreamSO : ScriptableObject {

        public string streamId;
        public string streamName;
        
        

    }
}