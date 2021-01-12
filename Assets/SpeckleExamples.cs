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
    public Button SendBtn;
    public InputField SendText;

    private GameObject receivedGo;

    void Start()
    {
      //hardcoded cuz I'm lazy, replace with what you need
      ReceiveText.text = "4ad65b572e";
      SendText.text = "cd83745025";
      
      if (ReceiveBtn == null || ReceiveText == null || SendBtn == null || SendText == null)
      {
        Debug.Log("Please set Send/Receive buttons and input fields");
        return;
      }
        

      Button btn = ReceiveBtn.GetComponent<Button>();
      btn.onClick.AddListener(CreateReceiver);
      
      Button btn2 = SendBtn.GetComponent<Button>();
      btn2.onClick.AddListener(SendData);
    }

    // Shows how to create a new Receiver from code
    private async void CreateReceiver()
    {
      var receiver = ScriptableObject.CreateInstance<Receiver>();
      receiver.Init(ReceiveText.text);
      
      //receive manually
      receivedGo = await receiver.Receive();
      AddClasses(receivedGo);
      
      //subscribe to new commits
      receiver.OnNewData += ReceiverOnNewData;

      ReceiveBtn.enabled = false;
      ReceiveText.enabled = false;
    }

    private void SendData()
    {
      if (!SelectionManager.selectedObjects.Any()) 
        return;
      
      var objs = new List<GameObject>();
      foreach (var index in SelectionManager.selectedObjects)
      {
        objs.Add(SelectionManager.selectables[index].gameObject);
      }
      Sender.Send(SendText.text, objs);

    }

    private void ReceiverOnNewData(GameObject go)
    {
      Debug.Log($"Received {go.name}");
      
      if(receivedGo!=null)
        Destroy(receivedGo);
      
      AddClasses(go);
      receivedGo = go;
    }

    /// <summary>
    /// Adds material and selectable script to all children of a GameObject
    /// </summary>
    /// <param name="go"></param>
    private void AddClasses(GameObject go)
    {
      var mat = new Material(Shader.Find("Standard"));

      for (var i = 0; i < go.transform.childCount; i++)
      {
        var child = go.transform.GetChild(i);
        var renderer = child.GetComponent<MeshRenderer>();
        renderer.material = mat;
        
        child.gameObject.AddComponent<Selectable>();
      }
    }
  }
}