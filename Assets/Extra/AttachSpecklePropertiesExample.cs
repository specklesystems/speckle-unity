using System.Collections;
using System.Collections.Generic;
using Objects.Converter.Unity;
using Speckle.ConnectorUnity.Utils;
using Speckle.ConnectorUnity.Wrappers;
using Speckle.Core.Api;
using Speckle.Core.Credentials;
using Speckle.Core.Models;
using Speckle.Core.Transports;
using UnityEngine;

/// <summary>
/// Example script for grabbing speckle properties for a specific object "on-the-fly"
/// </summary>
/// <remarks>
/// see discussion https://speckle.community/t/reloading-assemblies-takes-too-long/6708
/// </remarks>
[AddComponentMenu("Speckle/Extras/" + nameof(AttachSpecklePropertiesExample))]
public class AttachSpecklePropertiesExample : MonoBehaviour
{
    public string streamId;
    public string objectId;

    public virtual void Start()
    {
        Client speckleClient = new(AccountManager.GetDefaultAccount()!);

        StartCoroutine(AttachSpeckleProperties(speckleClient, streamId, objectId));
    }

    public IEnumerator AttachSpeckleProperties(
        Client speckleClient,
        string streamId,
        string objectId
    )
    {
        //Fetch the object from Speckle
        ServerTransport remoteTransport = new(speckleClient.Account, streamId);
        Utils.WaitForTask<Base> operation =
            new(async () => await Operations.Receive(objectId, remoteTransport));

        //yield until task completes
        yield return operation;
        Base speckleObject = operation.Result;

        //Do something with the properties. e.g. attach SpeckleProperties component
        DoSomething(speckleObject);
    }

    protected virtual void DoSomething(Base speckleObject)
    {
        //GetProperties will filter "useful" properties
        Dictionary<string, object> properties = ConverterUnity.GetProperties(
            speckleObject,
            typeof(SpeckleObject)
        );

        var sd = this.gameObject.AddComponent<SpeckleProperties>();
        sd.Data = properties;
        sd.SpeckleType = speckleObject.GetType();
    }
}
