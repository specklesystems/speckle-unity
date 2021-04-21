using UnityEditor;
using UnityEngine;

namespace Speckle.ConnectorUnity {

    [CustomEditor( typeof( BabyStreamManager ) )]
    [CanEditMultipleObjects]
    public class BabyStreamManagerEditor : Editor {

        private int _selectedStreamIndex, _selectedBranchIndex, _selectedCommitIndex;

        private string[ ] _streamNames;

        public string[ ] branchNames { private get; set; }
        public string[ ] commitIds { private get; set; }

        private bool _auto;

        private void OnEnable( )
            {
                // _property = serializedObject.FindProperty( "StreamListByName" );

                // BabyStreamManager script = (BabyStreamManager) target;
                // if ( script != null ) {
                //     script.StreamSelectedEvent += delegate( object sender, StreamSelectedArgs args ) {
                //         Debug.Log( "Update on GUI" );
                //         branchNames = args.BranchNames.ToArray( );
                //         commitIds = args.CommitIds.ToArray( );
                //     };
                // }
            }

        public override void OnInspectorGUI( )
            {
                serializedObject.Update( );
                
                // display values that are not being modified
                DrawDefaultInspector( );

                BabyStreamManager script = (BabyStreamManager) target;

                if ( script.StreamList != null && script.StreamList.Count > 0 ) {
                    // index = EditorGUILayout.Popup( "Streams", _selectedStreamIndex, new string[]{"Stream 1", "Stream 2", "Stream 3", "Stream 4", "Stream 5"}, GUILayout.ExpandWidth( true ) );
                    var index = EditorGUILayout.Popup( "Streams", _selectedStreamIndex, script.StreamsByName.ToArray(  ), GUILayout.ExpandWidth( true ) );

                    _selectedStreamIndex = index;
                    script.SetSelectedStream = _selectedStreamIndex;

                    index = EditorGUILayout.Popup( "Branches", _selectedBranchIndex, script.BranchesByName.ToArray( ), GUILayout.ExpandWidth( true ) );
                 
                    _selectedBranchIndex = index;
                    script.SetSelectedBranch = _selectedBranchIndex;

                    index = EditorGUILayout.Popup( "Commits", _selectedCommitIndex, script.CommitByNames.ToArray( ), GUILayout.ExpandWidth( true ) );
          
                    _selectedCommitIndex = index;
                    script.SetSelectedCommit = _selectedCommitIndex;

                    GUILayout.BeginHorizontal( );
                    
                    var auto = GUILayout.Toggle( _auto, "AutoLoad", GUILayout.ExpandWidth( false ) );
                    if ( auto != _auto ) {
                        _auto = auto;
                        script.AutoUpdate = _auto;
                    }
                    
                    if ( GUILayout.Button( "Load Stream" ) ) {
                        script.LoadStream( );
                    }
                    GUILayout.EndHorizontal( );
                }

                serializedObject.ApplyModifiedProperties( );
            }


    }
}