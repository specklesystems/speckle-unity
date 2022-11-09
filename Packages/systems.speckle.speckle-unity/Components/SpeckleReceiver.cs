using System;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Speckle.ConnectorUnity.Wrappers.Selection;
using Speckle.Core.Api;
using Speckle.Core.Credentials;
using Speckle.Core.Kits;
using Speckle.Core.Logging;
using Speckle.Core.Models;
using Speckle.Core.Transports;
using UnityEngine;
using UnityEngine.Events;

namespace Speckle.ConnectorUnity.Components
{
    [ExecuteAlways]
    [AddComponentMenu("Speckle/Speckle Receiver")]
    [RequireComponent(typeof(RecursiveConverter))]
    public class SpeckleReceiver : MonoBehaviour, ISerializationCallbackReceiver
    {
        [field: SerializeReference]
        public AccountSelection Account { get; protected set; }
        
        [field: SerializeReference]
        public StreamSelection Stream { get; protected set; }
        
        [field: SerializeReference]
        public BranchSelection Branch { get; protected set; }
        
        [field: SerializeReference]
        public CommitSelection Commit { get; protected set; }

        public RecursiveConverter Converter { get; protected set; }

        private CancellationTokenSource cancellationTokenSource;
        
        [Header("Events"), HideInInspector]
        public UnityEvent<Commit> OnCommitSelectionChange;
        [HideInInspector]
        public UnityEvent<ConcurrentDictionary<string, int>> OnReceiveProgressAction;
        [HideInInspector]
        public UnityEvent<string, Exception> OnErrorAction;
        [HideInInspector]
        public UnityEvent<int> OnTotalChildrenCountKnown;
        [HideInInspector]
        public UnityEvent<Base> OnComplete;

#nullable enable

        public void Awake()
        {
            Initialise(true);
            Converter = GetComponent<RecursiveConverter>();
            cancellationTokenSource = new CancellationTokenSource();
            
        }

        protected void Initialise(bool forceRefresh = false)
        {
            Account ??= new AccountSelection();
            Stream ??= new StreamSelection(Account);
            Branch ??= new BranchSelection(Stream);
            Commit ??= new CommitSelection(Branch);
            Stream.Initialise();
            Branch.Initialise();
            Commit.Initialise();
            Commit.OnSelectionChange = () => OnCommitSelectionChange.Invoke(Commit.Selected);
            if(Account.Options is not {Length: > 0} || forceRefresh)
                Account.RefreshOptions();
            
        }


    
    
        /// <summary>
        /// Receives the selected commit object using async Task
        /// </summary>
        /// <param name="token"></param>
        /// <returns>Awaitable commit object</returns>
        /// <exception cref="SpeckleException">thrown when selection is incomplete</exception>
        public async Task<Base?> ReceiveAsync(CancellationToken token)
        {
            if(!GetSelection(out Client? client, out Stream? stream, out Commit? commit, out string? error))
                throw new SpeckleException(error);
        
            return await ReceiveAsync(
                token: token,
                client: client,
                streamId: stream.id,
                objectId: commit.referencedObject,
                commitId: commit.id,
                onProgressAction: dict => OnReceiveProgressAction.Invoke(dict),
                onErrorAction: (m, e) => OnErrorAction.Invoke(m, e),
                onTotalChildrenCountKnown: c => OnTotalChildrenCountKnown.Invoke(c)
            );
        }

        /// <summary>
        /// Receives the requested <see cref="objectId"/> using async Task
        /// </summary>
        /// <param name="token"></param>
        /// <param name="client"></param>
        /// <param name="streamId"></param>
        /// <param name="objectId"></param>
        /// <param name="commitId"></param>
        /// <param name="onProgressAction"></param>
        /// <param name="onErrorAction"></param>
        /// <param name="onTotalChildrenCountKnown"></param>
        /// <returns></returns>
        public static async Task<Base?> ReceiveAsync(CancellationToken token,
            Client client,
            string streamId,
            string objectId,
            string? commitId,
            Action<ConcurrentDictionary<string, int>>? onProgressAction = null,
            Action<string, Exception>? onErrorAction = null,
            Action<int>? onTotalChildrenCountKnown = null)
        {
            ServerTransport transport = new ServerTransport(client.Account, streamId);
            transport.CancellationToken = token;
        
            Base? ret = null;
            try
            {
                Analytics.TrackEvent(client.Account, Analytics.Events.Receive);

                token.ThrowIfCancellationRequested();

                ret = await Operations.Receive(
                    objectId: objectId,
                    cancellationToken: token,
                    remoteTransport: transport,
                    onProgressAction: onProgressAction,
                    onErrorAction: onErrorAction,
                    onTotalChildrenCountKnown: onTotalChildrenCountKnown,
                    disposeTransports: true
                );

                token.ThrowIfCancellationRequested();

                //Read receipt
                try
                {
                    await client.CommitReceived(token, new CommitReceivedInput
                    {
                        streamId = streamId,
                        commitId = commitId,
                        message = $"received commit from {Application.unityVersion}",
                        sourceApplication = HostApplications.Unity.GetVersion(CoreUtils.GetHostAppVersion())
                    });
                }
                catch (Exception e)
                {
                    // Do nothing!
                    Debug.LogWarning($"Failed to send read receipt\n{e}");
                }
            }
            catch (Exception e)
            {
                onErrorAction?.Invoke(e.Message, e);
            }
            finally
            {
                transport?.Dispose();
            }
        
            return ret;
        }
    
        /// <summary>
        /// Helper method for using <see cref="RecursiveConverter"/>.
        /// Creates blank GameObjects for each property/category of the root object.
        /// </summary>
        /// <param name="base">The commitObject to convert</param>
        /// <param name="rootObjectName">The name of the parent <see cref="GameObject"/> to create</param>
        /// <param name="beforeConvertCallback">Callback for each object converted</param>
        /// <returns>The created parent <see cref="GameObject"/></returns>
        public GameObject ConvertToNativeWithCategories(Base @base, string rootObjectName,
            Action<Base>? beforeConvertCallback)
        {
            var rootObject = new GameObject(rootObjectName);

            bool Predicate(Base o)
            {
                beforeConvertCallback?.Invoke(o);
                return Converter.ConverterInstance.CanConvertToNative(o) //Accept geometry
                       || o.speckle_type == nameof(Base) && o.totalChildrenCount > 0; // Or Base objects that have children  
            }


            // For the rootObject only, we will create property GameObjects
            // i.e. revit categories
            foreach (var prop in @base.GetMembers())
            {
                var converted = Converter.RecursivelyConvertToNative(prop.Value, null, Predicate);

                //Skip empties
                if (converted.Count <= 0) continue;

                var propertyObject = new GameObject(prop.Key);
                propertyObject.transform.SetParent(rootObject.transform);
                foreach (var o in converted)
                {
                    if (o.transform.parent == null) o.transform.SetParent(propertyObject.transform);
                }
            }

            return rootObject;
        }
    
        /// <summary>
        /// 
        /// </summary>
        /// <param name="client"></param>
        /// <param name="stream"></param>
        /// <param name="commit"></param>
        /// <param name="error">error messages for </param>
        /// <returns>true if selection is complete, as we are ready to receive</returns>
        public bool GetSelection(
            [NotNullWhen(true)] out Client? client,
            [NotNullWhen(true)] out Stream? stream,
            [NotNullWhen(true)] out Commit? commit,
            [NotNullWhen(false)] out string? error)
        {
            Account? account = Account.Selected;
            stream = Stream.Selected;
            commit = Commit.Selected;
        
            if (account == null)
            {
                error = "Selected Account is null";
                client = null;
                return false;
            }
            client = Account.Client ?? new Client(account); 
        
            if (stream == null)
            {
                error = "Selected Stream is null";
                return false;
            }
        
            if (commit == null) 
            {
                error = "Selected Commit is null";
                return false;
            }
            error = null;
            return true;
        }
    
        /// <summary>
        /// Fetches the commit preview for the currently selected commit
        /// </summary>
        /// <param name="callback">Callback function to be called when the web request completes</param>
        /// <returns><see langword="false"/> if <see cref="Account"/>, <see cref="Stream"/>, or <see cref="Commit"/> was <see langword="null"/></returns>
        public bool GetPreviewImage(Action<Texture2D?> callback)
        {
            Account? account = Account.Selected;
            if (account == null) return false;
            string? streamId = Stream.Selected?.id;
            if (streamId == null) return false;
            string? commitId = Commit.Selected?.id;
            if (commitId == null) return false;
            string url = $"{account.serverInfo.url}/preview/{streamId}/commits/{commitId}";
            string authToken = account.token;
            
            StartCoroutine(Utils.Utils.GetImageRoutine(url, authToken, callback));
            return true;
        }
        
        public void OnDestroy()
        {
            cancellationTokenSource?.Cancel();
            cancellationTokenSource?.Dispose();
        }
        
        public void OnBeforeSerialize()
        {
            //pass
        }
        public void OnAfterDeserialize()
        {
            Initialise();
        }
    }
}





// using System;
// using System.Collections.Concurrent;
// using System.Diagnostics.CodeAnalysis;
// using System.Threading;
// using System.Threading.Tasks;
// using Speckle.ConnectorUnity.Wrappers.Selection;
// using Speckle.Core.Api;
// using Speckle.Core.Credentials;
// using Speckle.Core.Kits;
// using Speckle.Core.Logging;
// using Speckle.Core.Models;
// using Speckle.Core.Transports;
// using UnityEngine;
// using UnityEngine.Events;
//
// namespace Speckle.ConnectorUnity.Components
// {
//     [ExecuteAlways]
//     [AddComponentMenu("Speckle/Speckle Receiver")]
//     [RequireComponent(typeof(RecursiveConverter))]
//     public class SpeckleReceiver : MonoBehaviour
//     {
//         [field: SerializeReference]
//         public AccountSelection Account { get; protected set; }
//         
//         [field: SerializeReference]
//         public StreamSelection Stream { get; protected set; }
//         
//         [field: SerializeReference]
//         public BranchSelection Branch { get; protected set; }
//         
//         [field: SerializeReference]
//         public CommitSelection Commit { get; protected set; }
//
//         public RecursiveConverter Converter { get; protected set; }
//
//         private CancellationTokenSource cancellationTokenSource;
//         
//         [Header("Events")]
//         public UnityEvent<Commit> OnCommitSelectionChange;
//         public UnityEvent<ConcurrentDictionary<string, int>> OnReceiveProgressAction;
//         public UnityEvent<string, Exception> OnErrorAction;
//         public UnityEvent<int> OnTotalChildrenCountKnown;
//         public UnityEvent<Base> OnComplete;
//
// #nullable enable
//
//         void Awake()
//         {
//             cancellationTokenSource = new CancellationTokenSource();
//             
//             Commit = GetComponent<CommitSelection>();
//             if (Commit == null) Commit = gameObject.AddComponent<CommitSelection>();
//             
//             Converter = GetComponent<RecursiveConverter>();
//             if (Converter == null) Converter = gameObject.AddComponent<RecursiveConverter>();
//         }
//
//         protected void Start()
//         {
//             Commit.OnSelectionChange.AddListener(() => OnCommitSelectionChange?.Invoke(Commit.Selected));
//             Branch = Commit.BranchSelection;
//             Stream = Branch.StreamSelection;
//             Account = Stream.AccountSelection;
//         
//             if (Account.Options is not {Length: > 0})
//                 Account.RefreshOptions();
//             
//         }
//     
//     
//         /// <summary>
//         /// 
//         /// </summary>
//         /// <param name="client"></param>
//         /// <param name="stream"></param>
//         /// <param name="commit"></param>
//         /// <param name="error">error messages for </param>
//         /// <returns>true if selection is complete, as we are ready to receive</returns>
//         public bool GetSelection(
//             [NotNullWhen(true)] out Client? client,
//             [NotNullWhen(true)] out Stream? stream,
//             [NotNullWhen(true)] out Commit? commit,
//             [NotNullWhen(false)] out string? error)
//         {
//             Account? account = Account.Selected;
//             stream = Stream.Selected;
//             commit = Commit.Selected;
//         
//             if (account == null)
//             {
//                 error = "Selected Account is null";
//                 client = null;
//                 return false;
//             }
//             client = Account.Client ?? new Client(account); 
//         
//             if (stream == null)
//             {
//                 error = "Selected Stream is null";
//                 return false;
//             }
//         
//             if (commit == null) 
//             {
//                 error = "Selected Commit is null";
//                 return false;
//             }
//             error = null;
//             return true;
//         }
//     
//     
//         /// <summary>
//         /// Receives the selected commit object using async Task
//         /// </summary>
//         /// <param name="token"></param>
//         /// <returns>Awaitable commit object</returns>
//         /// <exception cref="SpeckleException">thrown when selection is incomplete</exception>
//         public async Task<Base?> ReceiveAsync(CancellationToken token)
//         {
//             if(!GetSelection(out Client? client, out Stream? stream, out Commit? commit, out string? error))
//                 throw new SpeckleException(error);
//         
//             return await ReceiveAsync(
//                 token: token,
//                 client: client,
//                 streamId: stream.id,
//                 objectId: commit.referencedObject,
//                 commitId: commit.id,
//                 onProgressAction: dict => OnReceiveProgressAction.Invoke(dict),
//                 onErrorAction: (m, e) => OnErrorAction.Invoke(m, e),
//                 onTotalChildrenCountKnown: c => OnTotalChildrenCountKnown.Invoke(c)
//             );
//         }
//
//         /// <summary>
//         /// Receives the requested <see cref="objectId"/> using async Task
//         /// </summary>
//         /// <param name="token"></param>
//         /// <param name="client"></param>
//         /// <param name="streamId"></param>
//         /// <param name="objectId"></param>
//         /// <param name="commitId"></param>
//         /// <param name="onProgressAction"></param>
//         /// <param name="onErrorAction"></param>
//         /// <param name="onTotalChildrenCountKnown"></param>
//         /// <returns></returns>
//         public static async Task<Base?> ReceiveAsync(CancellationToken token,
//             Client client,
//             string streamId,
//             string objectId,
//             string? commitId,
//             Action<ConcurrentDictionary<string, int>>? onProgressAction = null,
//             Action<string, Exception>? onErrorAction = null,
//             Action<int>? onTotalChildrenCountKnown = null)
//         {
//             ServerTransport transport = new ServerTransport(client.Account, streamId);
//             transport.CancellationToken = token;
//         
//             Base? ret = null;
//             try
//             {
//                 Analytics.TrackEvent(client.Account, Analytics.Events.Receive);
//
//                 token.ThrowIfCancellationRequested();
//
//                 ret = await Operations.Receive(
//                     objectId: objectId,
//                     cancellationToken: token,
//                     remoteTransport: transport,
//                     onProgressAction: onProgressAction,
//                     onErrorAction: onErrorAction,
//                     onTotalChildrenCountKnown: onTotalChildrenCountKnown,
//                     disposeTransports: true
//                 );
//
//                 token.ThrowIfCancellationRequested();
//
//                 //Read receipt
//                 try
//                 {
//                     await client.CommitReceived(token, new CommitReceivedInput
//                     {
//                         streamId = streamId,
//                         commitId = commitId,
//                         message = $"received commit from {Application.unityVersion}",
//                         sourceApplication = HostApplications.Unity.GetVersion(CoreUtils.GetHostAppVersion())
//                     });
//                 }
//                 catch (Exception e)
//                 {
//                     // Do nothing!
//                     Debug.LogWarning($"Failed to send read receipt\n{e}");
//                 }
//             }
//             catch (Exception e)
//             {
//                 onErrorAction?.Invoke(e.Message, e);
//             }
//             finally
//             {
//                 transport?.Dispose();
//             }
//         
//             return ret;
//         }
//     
//         /// <summary>
//         /// Helper method for using <see cref="RecursiveConverter"/>.
//         /// Creates blank GameObjects for each property/category of the root object.
//         /// </summary>
//         /// <param name="base">The commitObject to convert</param>
//         /// <param name="rootObjectName">The name of the parent <see cref="GameObject"/> to create</param>
//         /// <param name="beforeConvertCallback">Callback for each object converted</param>
//         /// <returns>The created parent <see cref="GameObject"/></returns>
//         public GameObject ConvertToNativeWithCategories(Base @base, string rootObjectName,
//             Action<Base>? beforeConvertCallback)
//         {
//             var rootObject = new GameObject(rootObjectName);
//
//             bool Predicate(Base o)
//             {
//                 beforeConvertCallback?.Invoke(o);
//                 return Converter.ConverterInstance.CanConvertToNative(o) //Accept geometry
//                        || o.speckle_type == nameof(Base) && o.totalChildrenCount > 0; // Or Base objects that have children  
//             }
//
//
//             // For the rootObject only, we will create property GameObjects
//             // i.e. revit categories
//             foreach (var prop in @base.GetMembers())
//             {
//                 var converted = Converter.RecursivelyConvertToNative(prop.Value, null, Predicate);
//
//                 //Skip empties
//                 if (converted.Count <= 0) continue;
//
//                 var propertyObject = new GameObject(prop.Key);
//                 propertyObject.transform.SetParent(rootObject.transform);
//                 foreach (var o in converted)
//                 {
//                     if (o.transform.parent == null) o.transform.SetParent(propertyObject.transform);
//                 }
//             }
//
//             return rootObject;
//         }
//     
//     
//         public void OnDestroy()
//         {
//             cancellationTokenSource?.Cancel();
//             cancellationTokenSource?.Dispose();
//         }
//         
//     }
// }
