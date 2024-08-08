using System.Collections;
using System.Threading.Tasks;
using Speckle.ConnectorUnity;
using Speckle.ConnectorUnity.Components;
using Speckle.Core.Api;
using Speckle.Core.Api.GraphQL.Models;
using Speckle.Core.Credentials;
using Speckle.Core.Models;
using Speckle.Core.Transports;
using UnityEngine;

[AddComponentMenu("Speckle/Extras/Manual Receiver")]
[RequireComponent(typeof(RecursiveConverter))]
public class ManualReceive : MonoBehaviour
{
    public string authToken;
    public string serverUrl;
    public string streamId,
        objectId;

    private RecursiveConverter receiver;

    void Awake()
    {
        receiver = GetComponent<RecursiveConverter>();
    }

    IEnumerator Start()
    {
        Debug.developerConsoleVisible = true;
        if (Time.timeSinceLevelLoad > 20)
            yield return null;
        Receive();
    }

    [ContextMenu(nameof(Receive))]
    public void Receive()
    {
        var account = new Account()
        {
            token = authToken,
            serverInfo = new ServerInfo() { url = serverUrl },
        };

        Task.Run(async () =>
        {
            using ServerTransport transport = new(account, streamId);
            MemoryTransport localTransport = new();

            Base speckleObject = await Operations.Receive(
                objectId,
                remoteTransport: transport,
                localTransport: localTransport
            );

            Dispatcher
                .Instance()
                .Enqueue(() =>
                {
                    var parentObject = new GameObject(name);

                    receiver.RecursivelyConvertToNative_Sync(speckleObject, parentObject.transform);

                    Debug.Log($"Receive {objectId} completed");
                });
        });
    }
}
