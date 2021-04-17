using System.Collections.Generic;
using UnityEngine;
using Speckle.Core.Credentials;
using System.Linq;
using UnityEngine.UI;
using Stream = Speckle.Core.Api.Stream;

namespace Speckle.ConnectorUnity {
    public class SpeckleExamples : MonoBehaviour {

        public Text SelectStreamText;
        public Text DetailsStreamText;
        public Dropdown StreamSelectionDropdown;
        public Button AddReceiverBtn;
        public Toggle AutoReceiveToggle;
        public Button AddSenderBtn;
        public GameObject StreamPrefab;
        public Canvas StreamsCanvas;

        private List<Stream> StreamList = null;
        private Stream SelectedStream = null;
        private List<GameObject> StreamPrefabs = new List<GameObject>( );

        async void Start( )
            {
                if ( SelectStreamText == null || StreamSelectionDropdown == null ) {
                    Debug.Log( "Please set all input fields on _SpeckleExamples" );
                    return;
                }

                var defaultAccount = AccountManager.GetDefaultAccount( );
                if ( defaultAccount == null ) {
                    Debug.Log( "Please set a default account in SpeckleManager" );
                    return;
                }

                SelectStreamText.text = $"Select a stream on {defaultAccount.serverInfo.name}:";

                StreamList = await Streams.List( );
                if ( !StreamList.Any( ) ) {
                    Debug.Log( "There are no streams in your account, please create one online." );
                    return;
                }

                StreamSelectionDropdown.options.Clear( );
                foreach ( var stream in StreamList ) {
                    StreamSelectionDropdown.options.Add( new Dropdown.OptionData( stream.name + " - " + stream.id ) );
                }

                StreamSelectionDropdown.onValueChanged.AddListener( StreamSelectionChanged );
                //trigger ui refresh, maybe there's a better method
                StreamSelectionDropdown.value = -1;
                StreamSelectionDropdown.value = 0;

                AddReceiverBtn.onClick.AddListener( AddReceiver );
                AddSenderBtn.onClick.AddListener( AddSender );
            }

        public void StreamSelectionChanged( int index )
            {
                if ( index == -1 )
                    return;

                SelectedStream = StreamList[ index ];
                DetailsStreamText.text =
                    $"Description: {SelectedStream.description}\n" +
                    $"Link sharing on: {SelectedStream.isPublic}\n" +
                    $"Role: {SelectedStream.role}\n" +
                    $"Collaborators: {SelectedStream.collaborators.Count}\n" +
                    $"Id: {SelectedStream.id}";
            }

        // Shows how to create a new Receiver from code and then pull data manually
        // Created receivers are added to a List of Receivers for future use
        private void AddReceiver( )
            {
                var streamId = SelectedStream.id;
                var autoReceive = AutoReceiveToggle.isOn;

                var streamPrefab = Instantiate( StreamPrefab, new Vector3( 0, 0, 0 ),
                    Quaternion.identity );
                streamPrefab.name = $"receiver-{streamId}";
                streamPrefab.transform.SetParent( StreamsCanvas.transform );
                var rt = streamPrefab.GetComponent<RectTransform>( );
                rt.anchoredPosition = new Vector3( -10, -110 - StreamPrefabs.Count * 110, 0 );

                var receiver = streamPrefab.AddComponent<Receiver>( );

                var btn = streamPrefab.transform.Find( "Btn" ).GetComponentInChildren<Button>( );
                var streamText = streamPrefab.transform.Find( "StreamText" ).GetComponentInChildren<Text>( );
                var statusText = streamPrefab.transform.Find( "StatusText" ).GetComponentInChildren<Text>( );
                var receiveProgress = btn.GetComponentInChildren<Slider>( );
                receiveProgress.gameObject.SetActive( false ); //hide

                receiver.Init( streamId, autoReceive, false,
                    onDataReceivedAction: ( go ) => {
                        statusText.text = $"Received {go.name}";
                        btn.interactable = true;
                        receiveProgress.value = 0;
                        receiveProgress.gameObject.SetActive( false );

                        AddComponents( go );
                    },
                    onTotalChildrenCountKnown: ( count ) => { receiver.TotalChildrenCount = count; },
                    onProgressAction: ( dict ) => {
                        //Run on a dispatcher as GOs can only be retrieved on the main thread
                        Dispatcher.Instance( ).Enqueue( ( ) => {
                            var val = dict.Values.Average( ) / receiver.TotalChildrenCount;
                            receiveProgress.gameObject.SetActive( true );
                            receiveProgress.value = (float) val;
                        } );
                    } );

                streamText.text = $"Stream: {SelectedStream.name}\nId: {SelectedStream.id} - Auto: {autoReceive}";
                btn.onClick.AddListener( ( ) => {
                    statusText.text = "Receiving...";
                    btn.interactable = false;
                    receiver.Receive( );
                } );

                StreamPrefabs.Add( streamPrefab );
            }

        private void AddSender( )
            {
                var streamId = SelectedStream.id;

                var streamPrefab = Instantiate( StreamPrefab, new Vector3( 0, 0, 0 ),
                    Quaternion.identity );
                streamPrefab.name = $"sender-{streamId}";
                streamPrefab.transform.SetParent( StreamsCanvas.transform );
                var rt = streamPrefab.GetComponent<RectTransform>( );
                rt.anchoredPosition = new Vector3( -10, -110 - StreamPrefabs.Count * 110, 0 );

                var sender = streamPrefab.AddComponent<Sender>( );

                var btn = streamPrefab.transform.Find( "Btn" ).GetComponentInChildren<Button>( );
                var streamText = streamPrefab.transform.Find( "StreamText" ).GetComponentInChildren<Text>( );
                var statusText = streamPrefab.transform.Find( "StatusText" ).GetComponentInChildren<Text>( );

                btn.GetComponentInChildren<Text>( ).text = "Send";
                statusText.text = "Ready to send";

                var sendProgress = btn.GetComponentInChildren<Slider>( );
                sendProgress.gameObject.SetActive( false ); //hide

                streamText.text = $"Stream: {SelectedStream.name}\nId: {SelectedStream.id}";

                btn.onClick.AddListener( ( ) => {
                        var objs = new List<GameObject>( );
                        foreach ( var index in SelectionManager.selectedObjects ) {
                            objs.Add( SelectionManager.selectables[ index ].gameObject );
                        }

                        if ( !objs.Any( ) ) {
                            statusText.text = $"No objects selected";
                            return;
                        }

                        btn.interactable = false;
                        statusText.text = "Sending...";
                        sender.Send( SelectedStream.id, objs,
                            onProgressAction: ( dict ) => {
                                //Run on a dispatcher as GOs can only be retrieved on the main thread
                                Dispatcher.Instance( ).Enqueue( ( ) => {
                                    var val = dict.Values.Average( ) / objs.Count;
                                    sendProgress.gameObject.SetActive( true );
                                    sendProgress.value = (float) val;
                                } );
                            },
                            onDataSentAction: ( commitId ) => {
                                Dispatcher.Instance( ).Enqueue( ( ) => {
                                    btn.interactable = true;
                                    statusText.text = $"Sent {commitId}";
                                    sendProgress.gameObject.SetActive( false ); //hide
                                } );
                            } );

                        StreamPrefabs.Add( streamPrefab );
                    }
                );
            }

        /// <summary>
        /// Recursively adds custom components to all children of a GameObject
        /// </summary>
        /// <param name="go"></param>
        private void AddComponents( GameObject go )
            {
                for ( var i = 0; i < go.transform.childCount; i++ ) {
                    var child = go.transform.GetChild( i );

                    if ( child.childCount > 0 ) {
                        AddComponents( child.gameObject );
                    }

                    child.gameObject.AddComponent<Selectable>( );

                    //Add extra Components
                    //var rigidbody = child.gameObject.AddComponent<Rigidbody>();
                    //rigidbody.mass = 10;
                }
            }

    }
}