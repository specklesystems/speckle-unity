using System.Collections;
using System.Collections.Generic;
using Speckle.ConnectorUnity;
using UnityEngine;

//This class shows how to use the ScriptableObject Receiver to receive data in game time
//From the editor navigate to Assets > Create > ScriptableObjects > Receiver
//Set the StreamId on the new DataReceiver created
//Attach it to this script's 'receiver'
//To be honest, creating the Receiver form code is just simpler...!
public class ScriptableObjectExample : MonoBehaviour
{
    public Receiver receiver;

    void Start()
    {
        return;
        receiver.Init();
        receiver.Receive();

        //... do more stuff eg set materials, subscribe to changes etc
    }


    void Update()
    {
        
    }
}
