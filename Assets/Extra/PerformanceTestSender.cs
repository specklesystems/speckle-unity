using System.Threading;
using System.Threading.Tasks;
using Speckle.ConnectorUnity.Components;
using Speckle.Core.Api;
using Speckle.Core.Models;
using Speckle.Core.Transports;
using UnityEngine;

namespace Extra
{
    /// <summary>
    /// Script used to generate streams for performance benchmarking in other hostApps.
    /// Will send several several commits with a varying number of copies on a GameObject (with children)
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(SpeckleSender))]
    public sealed class PerformanceTestSender : MonoBehaviour
    {
        [Range(0, 100)]
        public int numberOfIterations = 10;
    
        public Vector3 translation = Vector3.forward * 100;

        public GameObject objectToSend;

        private SpeckleSender sender;
        private void Awake()
        {
            sender = GetComponent<SpeckleSender>();
        }
        
        public async Task SendIterations()
        {
            GameObject go = new GameObject();
            for (int i = 0; i < numberOfIterations; i++)
            {
                Instantiate(objectToSend, translation * i, Quaternion.identity, go.transform);
            
                Base b = sender.Converter.RecursivelyConvertToSpeckle(go, _ => true);
                await Send(b, $"{i}");
            }
            Debug.Log("Done!");
        }

        private async Task<string> Send(Base data, string branchName)
        {
            var client = sender.Account.Client;
            var stream = sender.Stream.Selected;
            ServerTransport transport = new ServerTransport(sender.Account.Selected, stream!.id);
            
            await client.BranchCreate(new BranchCreateInput(){streamId = stream.id, name = branchName});
            
            return await SpeckleSender.SendDataAsync(CancellationToken.None,
                remoteTransport: transport,
                data: data,
                client: client!,
                branchName: branchName,
                createCommit: true,
                onProgressAction: null,
                onErrorAction: (m, e) => throw e);
        }
    }

#if UNITY_EDITOR
    [UnityEditor.CustomEditor(typeof(PerformanceTestSender))]
    public sealed class PerformanceTestSenderEditor : UnityEditor.Editor
    {
        public override async void OnInspectorGUI()
        {
            DrawDefaultInspector();
            
            if (GUILayout.Button("Create and send"))
            {
                await ((PerformanceTestSender)target).SendIterations();
            }
        }
    }
#endif
}
