using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Speckle.Core;
using Speckle.Core.Models;
using Speckle.Core.Api;
using Speckle.Core.Credentials;
using System;
using System.Threading.Tasks;
using System.Linq;
using Speckle.Core.Transports;
using Objects.Converter.Unity;
using Objects.Geometry;
using UnityEditor;
using System.Text.RegularExpressions;
using System.IO;

namespace Speckle.ConnectorUnity
{
  public class SpeckleExample : MonoBehaviour
  {
    async void Start()
    {
      var receiver = ScriptableObject.CreateInstance<Receiver>();
      receiver.Init("cd83745025", () => Debug.Log("Callback called"));
      await receiver.GetStreamLatestCommit();
    }

    private void OnReceivedData()
    {
      
    }
    
  }
}