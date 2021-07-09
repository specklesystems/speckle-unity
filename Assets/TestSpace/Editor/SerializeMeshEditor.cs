#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace Speckle.ConnectorUnity {

#if UNITY_EDITOR
    [CustomEditor( typeof( SerializeMesh ) )]
    internal class SerializeMeshEditor : Editor {
        private SerializeMesh obj;

        private void OnSceneGUI( )
            {
                obj = (SerializeMesh) target;
            }

        public override void OnInspectorGUI( )
            {
                base.OnInspectorGUI( );


                if ( GUILayout.Button( "Rebuild" ) ) {
                    if ( obj ) {
                        obj.gameObject.SafeMeshSet( obj.Rebuild( ) );
                    }
                }

                if ( GUILayout.Button( "Serialize" ) ) {
                    if ( obj ) {
                        obj.Serialize( );
                    }
                }
            }
    }
#endif
}