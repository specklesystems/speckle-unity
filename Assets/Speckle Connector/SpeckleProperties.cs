using System;
using System.Collections;
using Speckle.Core.Api;
using Speckle.Core.Models;
using UnityEngine;

namespace Speckle.ConnectorUnity
{

  /// <summary>
  /// This class gets attached to GOs and is used to store Speckle's metadata when sending / receiving
  /// </summary>
  public class SpeckleProperties : MonoBehaviour, ISerializationCallbackReceiver
  {

    [SerializeField, HideInInspector]
    private SpeckleObservablePropertiesCollection observablePropertiesCollection;

    [SerializeField, HideInInspector]
    private string _serializedData;

    public object this[string key]
    {
      get => observablePropertiesCollection[key];
      set => observablePropertiesCollection[key] = value;
    }

    public Dictionary<string, object> Data
    {
      get { return observablePropertiesCollection.ToDictionary(); }
      set { observablePropertiesCollection.Set(value); }
    }

    public void OnBeforeSerialize()
    {
      _serializedData = observablePropertiesCollection.serializedData;
    }

    public void OnAfterDeserialize()
    {
      observablePropertiesCollection.Set(_serializedData);
      _serializedData = null;
    }
    
    private void Awake()
    {
      observablePropertiesCollection = new SpeckleObservablePropertiesCollection();
    }

  }

}