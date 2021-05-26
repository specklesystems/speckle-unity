using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Unity.EditorCoroutines.Editor;
using UnityEditor;
using UnityEngine;

namespace Speckle.ConnectorUnity {
//https://forum.unity.com/threads/async-await-in-editor-script.481276/
//https://forum.unity.com/threads/follow-up-async-methods-continue-to-execute-when-you-stop-the-game-in-the-editor-potentially-dange.949671/

    [CustomEditor( typeof( BabyStreamManager ) )]
    [CanEditMultipleObjects]
    public class BabyStreamManagerEditor : Editor {

        private int _selectedStreamIndex, _selectedBranchIndex, _selectedCommitIndex, _selectedAccountIndex, _selectedServerIndex;

        private string[ ] _streamNames;

        private int _listValue = 10;

        public string[ ] branchNames { private get; set; }
        public string[ ] commitIds { private get; set; }

        private bool _auto, _processing;

        private bool _foldOutAccount, _foldOutStreams;
        private string[ ] accountNames;

        [InitializeOnLoadMethod]
        private static void Initialize( ) => EditorApplication.update += ExecuteContinuations;

        private void OnEnable( )
            {
                BabyStreamManager script = (BabyStreamManager) target;
                accountNames = script.Fetch.Accounts.GetNames( ).ToArray( );
                script.GetSpeckleAccountData( );
            }
        // EditorCoroutine m_LoggerCoroutine;
        // private IEnumerator LogTimeSinceCall( )
        //     {
        //         while ( true ) {
        //             Debug.Log("Running");
        //             yield return null;
        //         }
        //     }

        public override async void OnInspectorGUI( )
            {
                DrawDefaultInspector( );
                serializedObject.Update( );

                BabyStreamManager script = (BabyStreamManager) target;

                _processing = script.InProcess;

                #region Async Test GUI
                // if ( GUILayout.Button( "Do time consuming stuff" ) ) {
                //     Debug.Log( "before: " + Thread.CurrentThread.ManagedThreadId );
                //     if ( _processing ) {
                //         Debug.Log( "In process, block dropped..." );
                //     } else {
                //         script.InProcess = true;
                //         await Task.Run( DoTimeConsumingStuff );
                //         script.InProcess = false;
                //
                //         Debug.Log( "after: " + Thread.CurrentThread.ManagedThreadId );
                //         return;
                //     }
                // }
                //
                // GUILayout.BeginHorizontal( );
                //
                // if ( GUILayout.Button( "Post" ) ) {
                //     SynchronizationContext.Current.Post( _ => Debug.Log( "Submitted via Post" ), null );
                // }
                //
                // if ( GUILayout.Button( "Send" ) ) {
                //     SynchronizationContext.Current.Send( _ => Debug.Log( "Submitted via Send" ), null );
                // }
                //
                // GUILayout.EndHorizontal( );
                #endregion

                #region Account GUI
                EditorGUILayout.BeginHorizontal( );

                _selectedAccountIndex = EditorGUILayout.Popup( "Accounts", _selectedAccountIndex, accountNames, GUILayout.ExpandWidth( true ), GUILayout.Height( 20 ) );

                if ( GUILayout.Button( "Load", GUILayout.Width( 60 ), GUILayout.Height( 20 ) ) || accountNames == null ) {
                    await Task.Run( ( ) => script.GetSpeckleAccountData( ) );
                    accountNames = script.Fetch.Accounts.GetNames( ).ToArray( );
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

                // if ( items.Length <= 0 ) {
                //     Debug.LogWarning( "No Streams available in this account, please create some streams yo" );
                //     return;
                // }

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

        private void DoTimeConsumingStuff( )
            {
                Debug.Log( "doing..." );
                Thread.Sleep( 1000 );
                Debug.Log( "done: " + Thread.CurrentThread.ManagedThreadId );
            }

        private static void ExecuteContinuations( )
            {
                var context = SynchronizationContext.Current;
                var execMethod = context.GetType( ).GetMethod( "Exec", BindingFlags.NonPublic | BindingFlags.Instance );
                execMethod.Invoke( context, null );
            }

    }
}