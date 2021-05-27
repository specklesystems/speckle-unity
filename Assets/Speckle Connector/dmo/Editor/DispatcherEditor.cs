using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Speckle.Core.Api;
using Speckle.Core.Credentials;
using UnityEditor;
using UnityEngine;

namespace Speckle.ConnectorUnity.GUI {
    public class FunBatch {

        public Action<bool> MightHaveWork;
        public async void Init( )
            {
                var speckleAccount = AccountManager.GetAccounts( ).FirstOrDefault( );

                if ( speckleAccount == null ) {
                    Debug.Log( "Please set a default account in SpeckleManager" );
                    return;
                }
                var client = new Client( speckleAccount );

                var streams = await client.StreamsGet( 10 );
                var stream = streams.FirstOrDefault( s => s.name.Equals( "WhiteDoe" ) );

                var instance = new GameObject( stream.name ).AddComponent<Receiver>( );

                instance.Init( stream.id, false, false, speckleAccount,
                    onDataReceivedAction: go => { go.transform.SetParent( instance.transform ); },
                    onTotalChildrenCountKnown: count => { instance.TotalChildrenCount = count; },
                    onProgressAction: dict => { Dispatcher.Instance.Enqueue( ( ) => { Debug.Log( dict.Values.Average( ) / instance.TotalChildrenCount ); } ); } );

                instance.ReceiveCompleteAction += ( ) => {
                    MightHaveWork?.Invoke( true );
                    Debug.Log( "Receive Complete" );
                };
                instance.Receive( "main" );
            }

    }

    [CustomEditor( typeof( Dispatcher ) )]
    public class DispatcherEditor : Editor {

        [InitializeOnLoadMethod]
        private static void Initialize( ) => EditorApplication.update += Ayuda.ExecuteContinuations;


        public override async void OnInspectorGUI( )
            {
                DrawDefaultInspector( );
                serializedObject.Update( );

                Dispatcher script = (Dispatcher) target;

                GUILayout.BeginHorizontal( );

                if ( GUILayout.Button( "Owner Batch" ) ) {
                    Dispatcher.Ownerless = false;
                    // await Task.Run(script.EnqueueAsync( DoTimeConsumingStuff ) );
                    Ayuda.SyncCall( _ => script.Enqueue( DoTimeConsumingStuff ) );
                }

                if ( GUILayout.Button( "Ownerless Batch" ) ) {
                    Dispatcher.Ownerless = true;
                    Ayuda.SyncCall( _ => script.Enqueue( DoTimeConsumingStuff ) );
                }

                GUILayout.EndHorizontal( );

                // GUILayout.BeginHorizontal( );
                //
                // if ( GUILayout.Button( "Async Owner Batch" ) ) {
                //     if ( !_doingThings ) {
                //         Dispatcher.Ownerless = false;
                //         await script.EnqueueAsync( DoTimeConsumingStuff );
                //         return;
                //     }
                //
                //     Debug.Log( "Doing Things!" );
                // }
                //
                // if ( GUILayout.Button( "Async Ownerless Batch" ) ) {
                //     if ( !_doingThings ) {
                //         Dispatcher.Ownerless = true;
                //         await script.EnqueueAsync( DoTimeConsumingStuff );
                //         return;
                //     }
                //
                //     Debug.Log( "Doing Things!" );
                // }
                //
                // GUILayout.EndHorizontal( );

                serializedObject.ApplyModifiedProperties( );
            }

        private void DoTimeConsumingStuff( )
            {
                Debug.Log( "doing..." );
                Thread.Sleep( 200 );
                Debug.Log( "done: " + Thread.CurrentThread.ManagedThreadId );
                // force refresh 
                Repaint( );
            }

    }
}