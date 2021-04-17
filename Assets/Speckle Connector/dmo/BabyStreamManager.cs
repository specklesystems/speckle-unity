using System;
using System.Collections.Generic;
using System.Linq;
using Speckle.ConnectorUnity;
using Speckle.Core.Api;
using Speckle.Core.Credentials;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Speckle_Connector.dmo {
    public class BabyStreamManager : MonoBehaviour {

        private Stream _selectedStream = null;
        private List<Stream> _streams = null;
        private Account _account;

        // NOTE general info for editor updates
        [Header( "||  Runtime Params  || " )]
        [SerializeField, Range( 1, 20 )] private int streamListLimit = 10;

        [Header( "||  Speckle Account Stuff ||" )]
        [ReadOnly] [SerializeField] private string AccountName;
        [ReadOnly] [SerializeField] private string ServerName, ServerURL;
        
        [ReadOnly] [SerializeField] private List<string> StreamListByName;
        [ReadOnly] [SerializeField] private List<Receiver> ActiveReceivers;

        public int SetSelectedStream {
            set {
                if ( StreamList != null && value > 0 && StreamList.Count > value ) {
                    _selectedStream = StreamList[ value ];

                    Debug.Log( $"Setting Selected Stream to {_selectedStream.name}" );
                }
            }
        }

        public bool AutoUpdate { get; set; }

        public List<Stream> StreamList {
            get => _streams;
            set {
                if ( value != null ) {
                    Debug.Log( $"New Stream List coming in! {value.Count}" );
                    _streams = value;
                    StreamListByName = new List<string>( );
                    foreach ( var s in value ) {
                        StreamListByName.Add( s.name );
                    }
                }
            }
        }

        private void Start( )
            {
                FetchSpeckleAccountInfo( );
            }

        private async void FetchSpeckleAccountInfo( )
            {
                AccountName = "nada";
                ServerName = "nada";

                var defaultAccount = AccountManager.GetDefaultAccount( );

                if ( defaultAccount == null ) {
                    Debug.Log( "Please set a default account in SpeckleManager" );
                    return;
                }

                AccountName = defaultAccount.userInfo.name;
                ServerName = defaultAccount.serverInfo.name;
                ServerURL = defaultAccount.serverInfo.url;

                _account = defaultAccount;

                var client = new Client( _account );

                StreamList = await client.StreamsGet( streamListLimit );
            }

        private static Receiver CreateReceiverWrapper => new GameObject( "New Receiver" ).AddComponent<Receiver>( );

        public void LoadStream( )
            {
                Debug.Log( "Load Button Called" );

                if ( _selectedStream == null ) {
                    Debug.Log( "No Stream to Select from ya fool!" );
                    return;
                }

                if ( _account == null ) {
                    Debug.Log( "No account set to pull from :(" );
                    return;
                }

                ActiveReceivers ??= new List<Receiver>( );

                var id = _selectedStream.id;

                var instance = CreateReceiverWrapper;
                instance.Init( id, AutoUpdate, false, _account,
                    onDataReceivedAction: go => {
                        Debug.Log( "Item brought in" );
                        go.transform.SetParent( instance.transform );
                    },
                    onTotalChildrenCountKnown: count => { instance.TotalChildrenCount = count; },
                    onProgressAction: dict => {
                        // updating 
                        Dispatcher.Instance( ).Enqueue( ( ) => { Debug.Log( dict.Values.Average( ) / instance.TotalChildrenCount ); } );
                    } );

                // TODO currently is barking only at the main branch 
                instance.Receive( );
            }

    }
}