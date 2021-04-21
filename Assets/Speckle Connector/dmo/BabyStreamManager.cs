using System;
using System.Collections;
using Speckle.Core.Api;
using Speckle.Core.Credentials;
using System.Collections.Generic;
using System.Linq;
using Speckle_Connector.dmo;
using UnityEngine;

namespace Speckle.ConnectorUnity {
//https://forum.unity.com/threads/async-await-in-editor-script.481276/
    [RequireComponent( typeof( Dispatcher ) )]
    public class BabyStreamManager : MonoBehaviour {

        private Stream _selectedStream = null;
        private List<Stream> _streams = null;
        private Account _account;

        // NOTE general info for editor updates
        [Header( "||  Runtime Params  || " )]
        [SerializeField, Range( 1, 20 )] private int streamListLimit = 10;

        [Header( "||  Speckle Account Stuff ||" )]
        [Tooltip( "WIP - If working from Editor use a scriptable object to import streams" ),
         SerializeField] private StreamSO streamObj;
        [ReadOnly] [Tooltip( "Speckle 2.0 Account info that is populated at run time" )
                    , SerializeField] private string AccountName;
        [ReadOnly] [Tooltip( "Speckle 2.0 Server info that is populated at run time" ),
                    SerializeField] private string ServerName, ServerURL;

        [HideInInspector] [ReadOnly] [SerializeField]
        private List<string> StreamNames;
        [HideInInspector] [ReadOnly] [SerializeField]
        private List<string> BranchNames, CommitNames;
        [HideInInspector] [ReadOnly] [SerializeField]
        private List<Receiver> ActiveReceivers;

        public bool AutoUpdate { get; set; }

        public event EventHandler<StreamSelectedArgs> StreamSelectedEvent;

        public List<string> StreamsByName {
            get => StreamNames ?? new List<string>( );
            set => StreamNames = value;
        }

        public List<string> BranchesByName {
            get => BranchNames ?? new List<string>( );
            set => BranchNames = value;
        }

        public List<string> CommitByNames {
            get => CommitNames ?? new List<string>( );
            set => CommitNames = value;
        }

        private int _selectedStreamIndex, _selectedBranchIndex, _selectedCommitIndex;

        public int SetSelectedBranch {
            set {
                if ( _selectedBranchIndex == value || BranchNames == null ) return;

                _selectedBranchIndex = SetIndex( value, BranchNames );
            }
        }
        public int SetSelectedCommit {
            set {
                if ( _selectedCommitIndex == value || CommitByNames == null ) return;

                _selectedCommitIndex = SetIndex( value, CommitByNames );
            }
        }

        public int SetSelectedStream {
            set {
                if ( _selectedStreamIndex == value || StreamList == null ) return;

                _selectedStreamIndex = SetIndex( value, StreamList );
                _selectedStream = StreamList[ _selectedStreamIndex ];
                FetchSpeckleStreamInfo( );
            }
        }

        private static int SetIndex( int index, ICollection objects )
            {
                if ( objects != null ) {
                    if ( index >= objects.Count )
                        index = objects.Count - 1;
                    else if ( index < 0 )
                        index = 0;
                }
                return index;
            }

        public List<Stream> StreamList {
            get => _streams;
            set {
                if ( value != null ) {
                    Debug.Log( $"New Stream List coming in! {value.Count}" );

                    _streams = value;
                    StreamNames = ( from i in value select i.name ).ToList( );
                    // reset all names for branches and commits 
                    SetSelectedStream = 0;
                }
            }
        }

        private void Awake( )
            {
                // NOTE required for converting and setting up files
                if ( Dispatcher.Instance_Dmo == null && gameObject.GetComponent<Dispatcher>(  ) == null )
                    gameObject.AddComponent<Dispatcher>( );
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

                // TODO Figure out if storing the client as a property would make sense
                var client = new Client( _account );

                StreamList = await client.StreamsGet( streamListLimit );
                StreamNames = ( from i in StreamList select i.name ).ToList( );

                FetchSpeckleStreamInfo( client );
            }

        private async void FetchSpeckleStreamInfo( Client client = null )
            {
                client ??= new Client( _account );

                var branches = await client.StreamGetBranches( _selectedStream.id );
                BranchNames = ( from i in branches select i.name ).ToList( );
                Debug.Log( $"Found {BranchNames.Count} Branches" );

                var commits = await client.StreamGetCommits( _selectedStream.id );
                CommitNames = ( from i in commits select i.id ).ToList( );
                Debug.Log( $"Found {CommitNames.Count} Commits" );

                // TODO move into ui script
                StreamSelectedEvent?.Invoke( this, new StreamSelectedArgs( BranchNames, CommitNames ) );
            }

        private static Receiver CreateReceiverWrapper => new GameObject( "New Receiver" ).AddComponent<Receiver>( );

        public void LoadStream( )
            {
                if ( _selectedStream == null ) {
                    Debug.Log( "No Stream to Select from ya fool!" );
                    return;
                }

                if ( _account == null ) {
                    Debug.Log( "No account set to pull from :(" );
                    return;
                }

                ActiveReceivers ??= new List<Receiver>( );

                var instance = CreateReceiverWrapper;

                instance.Init( _selectedStream.id, AutoUpdate, false, _account,
                    onDataReceivedAction: go => {
                        Debug.Log( "Item brought in" );
                        go.transform.SetParent( instance.transform );
                    },
                    onTotalChildrenCountKnown: count => { instance.TotalChildrenCount = count; },
                    onProgressAction: dict => {
                        // updating 
                        Dispatcher.Instance( ).Enqueue( ( ) => { Debug.Log( dict.Values.Average( ) / instance.TotalChildrenCount ); } );
                    } );

                // NOTE this needs to be set prior to calling receive
                instance.BranchName = BranchNames[ _selectedBranchIndex ];

                instance.Receive( );
            }

    }
}