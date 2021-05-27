using System;
using System.Collections;
using Speckle.Core.Api;
using Speckle.Core.Credentials;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Speckle_Connector.dmo;
using UnityEngine;
using UnityEngine.Events;

namespace Speckle.ConnectorUnity {
    [ExecuteAlways]
    [RequireComponent( typeof( Dispatcher ) )]
    public class BabyStreamManager : MonoBehaviour {

        [Header( "||  Speckle Account Stuff ||" )]
        [Tooltip( "WIP - If working from Editor use a scriptable object to import streams" ),
         SerializeField] private StreamSO streamObj;

        private List<Receiver> ActiveReceivers;
        private UnityEvent<GameObject> StreamObjectLoadedEvent;

        public int ListLimit { get; set; }

        public Account Account {
            get => Stash.Account;
            set => Stash.Account = value;
        }

        public Stream SelectedStream {
            get => Stash.SelectedStream;
            set => Stash.SelectedStream = value;
        }

        public List<Stream> Streams {
            get => Stash.Streams;
            set => Stash.Streams = value;
        }

        public List<Branch> Branches {
            get => Stash.Branches;
            set => Stash.Branches = value;
        }

        public List<Commit> Commits {
            get => Stash.Commits;
            set => Stash.Commits = value;
        }

        private Fetcher _fetcher;
        public Fetcher Fetch {
            get {
                _fetcher ??= new Fetcher( this );
                return _fetcher;
            }
        }

        public bool AutoUpdate { get; set; }
        public bool InProcess { get; set; }

        public bool StreamReady => SelectedStream != null;

        public bool IsReady => Account != null && Streams?.Count > 0 && Branches?.Count > 0;

        private Dispatcher Dispatcher {
            get {
                // NOTE required for converting and setting up files
                if ( Dispatcher.Instance == null && gameObject.GetComponent<Dispatcher>( ) == null )
                    gameObject.AddComponent<Dispatcher>( );

                return Dispatcher.Instance;
            }
        }

        private int _selectedStreamIndex, _selectedBranchIndex, _selectedCommitIndex;

        public int SetSelectedBranch {
            set {
                if ( Branches != null )
                    _selectedBranchIndex = SetIndex( value, Fetch.BranchesByName );
            }
        }

        public int SetSelectedCommit {
            set {
                if ( Commits != null )
                    _selectedCommitIndex = SetIndex( value, Fetch.CommitsById );
            }
        }

        public async void SelectStreamFromList( int value )
            {
                if ( Streams == null ) return;

                ClearSelectedStreamData( );

                _selectedStreamIndex = SetIndex( value, Fetch.StreamsByName );

                var selectedStream = Streams[ _selectedStreamIndex ];

                if ( Account == null ) {
                    Debug.Log( "No account set to pull from :(" );
                    return;
                }

                if ( selectedStream == null ) {
                    Debug.Log( "No Stream to Select from ya fool!" );
                    return;
                }

                var client = new Client( Account );

                var branches = await client.StreamGetBranches( selectedStream.id );
                Branches = branches;

                var commits = await client.StreamGetCommits( selectedStream.id );
                Commits = commits;

                SelectedStream = selectedStream;
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

        internal static class Stash {

            public static Account Account { get; internal set; }

            public static Stream SelectedStream { get; internal set; }

            public static List<Stream> Streams { get; internal set; }

            public static List<Branch> Branches { get; internal set; }

            public static List<Commit> Commits { get; internal set; }

        }

        public class Fetcher {

            public Fetcher( BabyStreamManager manager )
                {
                    Manager = manager;
                }

            public string AccountName {
                get {
                    var value = "nada";
                    if ( Manager?.Account != null )
                        value = Manager.Account.userInfo.name;
                    return value;
                }
            }

            public string ServerName {
                get {
                    var value = "nada";
                    if ( Manager?.Account != null )
                        value = Manager.Account.serverInfo.name;
                    return value;
                }
            }

            public string ServerURL {
                get {
                    var value = "nada";
                    if ( Manager?.Account != null )
                        value = Manager.Account.serverInfo.url;
                    return value;
                }
            }

            public IEnumerable<Account> Accounts => AccountManager.GetAccounts( );

            public List<string> BranchesByName => GetByName( Manager?.Branches.GetInfo( ) );

            public List<string> StreamsByName => GetByName( Manager?.Streams.GetInfo( ) );

            public List<string> CommitsById => GetByName( Manager?.Commits.GetInfo( ) );

            private static List<string> GetByName( IReadOnlyCollection<SpeckleSimpleInfo> list )
                {
                    var names = new List<string>( );

                    if ( list != null && list.Count > 0 )
                        names = ( from i in list select i.name ).ToList( );

                    return names;
                }

            private BabyStreamManager Manager { get; }

            private bool DispatcherActive => Manager?.Dispatcher != null;

            public int SelectedStreamIndex => Manager == null ? 0 : Manager._selectedStreamIndex;

        }

        public async void GetSpeckleAccountData( )
            {
                InProcess = false;
                var speckleAccount = AccountManager.GetAccounts( ).FirstOrDefault( );

                if ( speckleAccount == null ) {
                    Debug.Log( "Please set a default account in SpeckleManager" );
                    return;
                }

                Account = speckleAccount;

                ClearData( true );
                var client = new Client( Account );

                var streams = await client.StreamsGet( ListLimit );

                Streams = streams;
            }

        private void ClearData( bool BranchesAndStreams )
            {
                Streams = new List<Stream>( );
                if ( !BranchesAndStreams ) return;

                ClearSelectedStreamData( );
            }

        private void ClearSelectedStreamData( )
            {
                SelectedStream = null;
                Branches = new List<Branch>( );
                Commits = new List<Commit>( );
            }

        public void LoadStream( )
            {
                if ( Account == null ) {
                    Debug.Log( "No account set to pull from :(" );
                    return;
                }

                if ( SelectedStream == null ) {
                    Debug.Log( "No Stream to Select from ya fool!" );
                    return;
                }

                var branchName = Fetch.BranchesByName[ _selectedBranchIndex ];

                var instance = new GameObject( $"{branchName} Receiver" ).AddComponent<Receiver>( );

                instance.Init( SelectedStream.id, AutoUpdate, false, Account,
                    onDataReceivedAction: go => {
                        go.transform.SetParent( instance.transform );
                        StreamObjectLoadedEvent?.Invoke( go );
                    },
                    onTotalChildrenCountKnown: count => { instance.TotalChildrenCount = count; },
                    onProgressAction: dict => { Dispatcher.Enqueue( ( ) => { Debug.Log( dict.Values.Average( ) / instance.TotalChildrenCount ); } ); } );

                instance.ReceiveCompleteAction += ( ) => { Debug.Log( "Receive Complete" ); };

                instance.Receive( branchName );

                ActiveReceivers ??= new List<Receiver>( );
                ActiveReceivers.Add( instance );
                InProcess = false;
            }

    }
}