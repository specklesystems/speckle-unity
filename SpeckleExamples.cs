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
using UnityEngine.UI;

namespace Speckle.ConnectorUnity
{
  public class SpeckleExamples : MonoBehaviour
  {

    public Button ReceiveBtn;
    public InputField ReceiveText;
    void Start()
    {
      if(ReceiveBtn ==null || ReceiveText==null)
        return;
      
      Button btn = ReceiveBtn.GetComponent<Button>();
      btn.onClick.AddListener(CreateReceiver);
    }

    public async void CreateReceiver()
    {
      var receiver = ScriptableObject.CreateInstance<Receiver>();
      receiver.Init(ReceiveText.text);
      receiver.OnNewData += ReceiverOnNewData;
      await receiver.Receive();
    }

    private void ReceiverOnNewData(GameObject data)
    {
      Debug.Log($"Received {data.name}");
    }
  }
}