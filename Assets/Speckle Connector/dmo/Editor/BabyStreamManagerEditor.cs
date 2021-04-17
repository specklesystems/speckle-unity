using UnityEditor;
using UnityEngine;

namespace Speckle.ConnectorUnity {
#if UNITY_EDITOR
    [CustomEditor( typeof( BabyStreamManager ) )]
    [CanEditMultipleObjects]
    public class BabyStreamManagerEditor : Editor {

        private SerializedProperty _property;
        private int _selectedStreamIndex, _selectedBranchIndex, _selectedCommitIndex;
        
        private string[ ] _streamNames;
        
        private bool _auto;

        private void OnEnable( )
            {
                _property = serializedObject.FindProperty( "StreamListByName" );
 
            }


      

        public override void OnInspectorGUI( )
            {
                serializedObject.Update( );

                // display values that are not being modified
                DrawDefaultInspector( );

                BabyStreamManager script = (BabyStreamManager) target;

                if ( script.StreamList != null && script.StreamList.Count > 0 ) {
                    _streamNames = new string[ script.StreamList.Count ];
                    for ( var i = 0; i < script.StreamList.Count; i++ ) {
                        _streamNames[ i ] = script.StreamList[ i ].name;
                    }
                    

                    var index = EditorGUILayout.Popup( "Streams", _selectedStreamIndex, _streamNames, GUILayout.ExpandWidth( true ) );

                    if ( index < 0 )
                        index = 0;

                    _selectedStreamIndex = index;
                    script.SetSelectedStream = _selectedStreamIndex;
                  
                    // GUILayout.Space( 5 );

                    index = EditorGUILayout.Popup( "Branches", _selectedBranchIndex, script.StreamBranchesByName.ToArray(  ), GUILayout.ExpandWidth( true ) );
                    if ( index < 0 )
                        index = 0;
                    
                    _selectedBranchIndex = index;
                    // complete 
                    script.SetSelectedBranch = _selectedBranchIndex;

                    index = EditorGUILayout.Popup( "Commits", _selectedCommitIndex, script.CommitByNames.ToArray(  ), GUILayout.ExpandWidth( true ) );
                    if ( index < 0 )
                        index = 0;
                    
                    _selectedCommitIndex = index;


                    GUILayout.BeginHorizontal( );

                    var auto = GUILayout.Toggle( _auto, "AutoLoad", GUILayout.ExpandWidth( false ) );
                    if ( auto != _auto ) {
                        _auto = auto;
                        script.AutoUpdate = _auto;
                    }
                    GUILayout.Space( 10 );

                    if ( GUILayout.Button( "Load Stream" ) ) {
                        script.LoadStream( );
                    }
                    GUILayout.EndHorizontal( );
                }

                serializedObject.ApplyModifiedProperties( );
            }

#endif

    }
}