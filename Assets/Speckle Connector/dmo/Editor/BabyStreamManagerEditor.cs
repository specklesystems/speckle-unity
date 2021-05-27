using System.Linq;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace Speckle.ConnectorUnity {
//https://forum.unity.com/threads/async-await-in-editor-script.481276/
//https://forum.unity.com/threads/follow-up-async-methods-continue-to-execute-when-you-stop-the-game-in-the-editor-potentially-dange.949671/

    [CustomEditor( typeof( BabyStreamManager ) )]
    [CanEditMultipleObjects]
    public class BabyStreamManagerEditor : Editor {

        private int _listValue = 10;
        private bool _auto,_foldOutAccount, _foldOutStreams;
        private string[ ] _streamNames,_accountNames;
        private int _selectedStreamIndex, _selectedBranchIndex, _selectedCommitIndex, _selectedAccountIndex, _selectedServerIndex;

        [InitializeOnLoadMethod]
        private static void Initialize( ) => EditorApplication.update += Ayuda.ExecuteContinuations;

        private void OnEnable( )
            {
                BabyStreamManager script = (BabyStreamManager) target;
                _accountNames = script.Fetch.Accounts.GetNames( ).ToArray( );
                script.GetSpeckleAccountData( );
            }

        public override async void OnInspectorGUI( )
            {
                DrawDefaultInspector( );
                serializedObject.Update( );

                BabyStreamManager script = (BabyStreamManager) target;

                #region Account GUI
                EditorGUILayout.BeginHorizontal( );

                _selectedAccountIndex = EditorGUILayout.Popup( "Accounts", _selectedAccountIndex, _accountNames, GUILayout.ExpandWidth( true ), GUILayout.Height( 20 ) );

                if ( GUILayout.Button( "Load", GUILayout.Width( 60 ), GUILayout.Height( 20 ) ) || _accountNames == null ) {
                    await Task.Run( ( ) => script.GetSpeckleAccountData( ) );
                    _accountNames = script.Fetch.Accounts.GetNames( ).ToArray( );
                    return;
                }

                EditorGUILayout.EndHorizontal( );

                #region Speckle Account Info
                _foldOutAccount = EditorGUILayout.BeginFoldoutHeaderGroup( _foldOutAccount, "Account Info" );

                if ( _foldOutAccount ) {
                    EditorGUI.BeginDisabledGroup( true );

                    EditorGUILayout.TextField( "Name", script.Fetch.AccountName,
                        GUILayout.Height( 20 ),
                        GUILayout.ExpandWidth( true ) );

                    EditorGUILayout.TextField( "Server", script.Fetch.ServerName,
                        GUILayout.Height( 20 ),
                        GUILayout.ExpandWidth( true ) );

                    EditorGUILayout.TextField( "URL", script.Fetch.ServerURL,
                        GUILayout.Height( 20 ),
                        GUILayout.ExpandWidth( true ) );

                    EditorGUI.EndDisabledGroup( );
                }

                EditorGUILayout.EndFoldoutHeaderGroup( );
                #endregion

                #region Stream Limit
                EditorGUILayout.Separator( );

                _listValue = EditorGUILayout.IntSlider( "Limit", _listValue, 1, 10, GUILayout.ExpandWidth( true ) );
                script.ListLimit = _listValue;
                #endregion

                EditorGUILayout.Separator( );
                #endregion

                #region Stream List
                var items = script.Fetch.StreamsByName.ToArray( );

                EditorGUILayout.BeginHorizontal( );

                _selectedStreamIndex = EditorGUILayout.Popup( "Streams",
                    _selectedStreamIndex, items, GUILayout.Height( 20 ), GUILayout.ExpandWidth( true ) );

                if ( GUILayout.Button( "Load", GUILayout.Width( 60 ), GUILayout.Height( 20 ) ) ) {
                    Debug.Log( "Loading stream " );
                    await Task.Run( ( ) => script.SelectStreamFromList( _selectedStreamIndex ) );
                    return;
                }

                EditorGUILayout.EndHorizontal( );
                #endregion

                #region Stream Branch GUI
                var streamStatus = script.StreamReady && script.Fetch.SelectedStreamIndex == _selectedStreamIndex;
                items = streamStatus ? script.Fetch.BranchesByName.ToArray( ) : new[ ] {"stream is not loaded"};

                // branch selection
                _selectedBranchIndex = EditorGUILayout.Popup( "Branches",
                    _selectedBranchIndex, items, GUILayout.Height( 20 ), GUILayout.ExpandWidth( true ) );
                script.SetSelectedBranch = _selectedStreamIndex;

                // commit selection
                items = streamStatus ? script.Fetch.CommitsById.ToArray( ) : new[ ] {"stream is not loaded"};
                _selectedCommitIndex = EditorGUILayout.Popup( "Commits",
                    _selectedCommitIndex, items, GUILayout.Height( 20 ), GUILayout.ExpandWidth( true ) );

                script.SetSelectedCommit = _selectedCommitIndex;
                #endregion

                GUILayout.BeginHorizontal( );

                var auto = GUILayout.Toggle( _auto, "AutoLoad", GUILayout.ExpandWidth( false ) );
                if ( auto != _auto ) {
                    _auto = auto;
                    script.AutoUpdate = _auto;
                }
                GUILayout.Space( 50 );

                var ready = script.IsReady && script.InProcess == false;

                EditorGUI.BeginDisabledGroup( !ready );

                if ( GUILayout.Button( !ready ? "Loading... " : "Load Stream" ) ) {
                    if ( ready )
                        script.LoadStream( );
                }
                EditorGUI.EndDisabledGroup( );

                GUILayout.EndHorizontal( );

                serializedObject.ApplyModifiedProperties( );
            }

    }
}