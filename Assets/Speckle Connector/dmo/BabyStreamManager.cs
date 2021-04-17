using Objects.Converter.Unity;
using Speckle.Core.Api;
using Speckle.Core.Api.SubscriptionModels;
using Speckle.Core.Credentials;
using Speckle.Core.Logging;
using Speckle.Core.Models;
using Speckle.Core.Transports;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Sentry;
using Sentry.Protocol;
using UnityEngine;

namespace Speckle.ConnectorUnity {

    public class BabyBase : Base {

        [DetachProperty]
        [Chunkable( 20000 )]
        public List<uint> values { get; set; } = new List<uint>( );

        [DetachProperty]
        [Chunkable( 20000 )]
        public List<double> vector { get; set; } = new List<double>( );
        


    }
    
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

        [HideInInspector] [ReadOnly] [SerializeField]
        private List<string> StreamNames;
        [HideInInspector] [ReadOnly] [SerializeField]
        private List<string> BranchNames, CommitNames;
        [HideInInspector] [ReadOnly] [SerializeField]
        private List<Receiver> ActiveReceivers;

        public bool AutoUpdate { get; set; }

        public string StreamName => _selectedStream == null ? "No Stream" : _selectedStream.name;
        public string BranchName => _selectedStream == null && _selectedStream.branch == null ? "No Branch" : _selectedStream.branch.name;
        public string CommitName => _selectedStream == null && _selectedStream.branch == null ? "No Branch" : _selectedStream.branch.name;

        public List<string> StreamBranchesByName {
            get => BranchNames ?? new List<string>( );
            set => BranchNames = value;
        }

        public List<string> CommitByNames {
            get => CommitNames ?? new List<string>( );
            set => CommitNames = value;
        }

        public int SetSelectedStream {
            set {
                if ( StreamList != null && value >= 0 && StreamList.Count > value ) {
                    _selectedStream = StreamList[ value ];
                }
            }
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

        private string FetchSelectedStreamInfo( )
            {
                return _selectedStream == null
                    ? "Select Stream First"
                    : $"Description: {_selectedStream.description}\n" +
                      $"Link sharing on: {_selectedStream.isPublic}\n" +
                      $"Role: {_selectedStream.role}\n" +
                      $"Collaborators: {_selectedStream.collaborators.Count}\n" +
                      $"Id: {_selectedStream.id}";
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
                // _selectedStream.branch.

                _account = defaultAccount;

                var client = new Client( _account );

                StreamList = await client.StreamsGet( streamListLimit );

                var branches = await client.StreamGetBranches( _selectedStream.id );
                BranchNames = ( from i in branches select i.name ).ToList( );

                var commits = await client.StreamGetCommits( _selectedStream.id );
                CommitNames = ( from i in commits select i.id ).ToList( );
            }

        private static Receiver CreateReceiverWrapper => new GameObject( "New Receiver" ).AddComponent<Receiver>( );
        public int SetSelectedBranch { get; set; }

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