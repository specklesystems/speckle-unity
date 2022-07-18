
using System.Collections;
using System.Threading.Tasks;
using Speckle.ConnectorUnity;
using Speckle.Core.Api;
using Speckle.Core.Credentials;
using Speckle.Core.Transports;
using UnityEngine;

[RequireComponent(typeof(RecursiveConverter))]
public class TestManualReceive : MonoBehaviour
{

    [field: SerializeField] private string AuthToken { get; set; }
    [field: SerializeField] private string ServerUrl { get; set; }
    [field: SerializeField] private string StreamId { get; set; }
    [field: SerializeField] private string ObjectId { get; set; }
    

    private RecursiveConverter receiver;
    

    void Awake()
    {
        receiver = GetComponent<RecursiveConverter>();
    }


    IEnumerator Start()
    {
        Debug.developerConsoleVisible = true;
        if(Time.timeSinceLevelLoad > 20) yield return null;
        Receive();
    }

    public void Receive()
    {
        var account = new Account()
        {
            token = AuthToken,
            serverInfo = new ServerInfo() {url = ServerUrl},
        };

        Task.Run(async () =>
        {
            var transport = new ServerTransport(account, StreamId);
            var localTransport = new MemoryTransport();
            
            var @base = await Operations.Receive(
                ObjectId,
                remoteTransport: transport,
                localTransport: localTransport,
                onErrorAction: (m, e)=> Debug.LogError(m + e),
                disposeTransports: true
            );

            Dispatcher.Instance().Enqueue(() =>
            {

                var rc = GetComponent<RecursiveConverter>();
                var parentObject = new GameObject(name);
                rc.RecursivelyConvertToNative(@base, parentObject.transform);

                Debug.Log($"Receive {ObjectId} completed");
            });
        });
    }
}
