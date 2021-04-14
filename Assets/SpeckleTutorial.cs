using System.Collections;
using System.Collections.Generic;
using Speckle.ConnectorUnity;
using UnityEngine;

namespace Speckle.ConnectorUnity
{
    public class SpeckleTutorial : MonoBehaviour
    {
        void Start()
        {
            // instantiate a re receiver
            var go = new GameObject();
            var receiver = go.AddComponent<Receiver>();
            
            receiver.Init();
        }

        // Update is called once per frame
        void Update()
        {

        }
    }
}