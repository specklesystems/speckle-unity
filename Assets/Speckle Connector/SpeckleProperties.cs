using System;
using System.Collections.Generic;
using Speckle.Core.Api;
using Speckle.Core.Models;
using UnityEngine;

namespace Speckle.ConnectorUnity
{


  public class SpeckleProperties : MonoBehaviour, ISerializationCallbackReceiver
  {
    
    public Dictionary<string, object> Data { get; set; }
    
    [SerializeField, HideInInspector]
    private string _serializedData;
    
    public void OnBeforeSerialize()
    {
      Data ??= new Dictionary<string, object>();
      
      SpeckleData speckleData = new SpeckleData(Data);
      _serializedData = Operations.Serialize(speckleData);
    }

    public void OnAfterDeserialize()
    {
      Base speckleData = Operations.Deserialize(_serializedData);
      
      Data = speckleData.GetMembers();
      _serializedData = null;
    }

    [Serializable]
    private class SpeckleData : Base
    {
      public SpeckleData(IDictionary<string, object> data)
      {
        foreach (var v in data)
        {
          this[v.Key] = v.Value;
        }
      }
    }

    [ContextMenu(nameof(Test))]
    public void Test()
    {
      Debug.Log(Data.Count); //Set a breakpoint here to look at Data
    }
    
  }

}