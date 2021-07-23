using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Speckle.ConnectorUnity
{
    /// <summary>
    /// This class gets attached to GOs and is used to store Speckle's metadata when sending / receiving
    /// </summary>
    public class SpeckleProperties : MonoBehaviour
    { 
        public Dictionary<string, object> Data { get; set; }
    }
}