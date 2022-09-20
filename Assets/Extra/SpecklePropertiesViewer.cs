using System;
using System.Collections.Generic;
using System.Linq;
using Speckle.ConnectorUnity;
using UnityEngine;

[RequireComponent(typeof(SpeckleProperties)), ExecuteAlways]
public class SpecklePropertiesViewer : MonoBehaviour, ISerializationCallbackReceiver
{
    [SerializeField]
    private List<KVP> Data;
    
    [SerializeField]
    private int count;

    private SpeckleProperties properties;
    public void Awake()
    {
        properties = GetComponent<SpeckleProperties>();
    }

    public void OnBeforeSerialize()
    {
        Data = properties.Data.Select(x => new KVP(x.Key.ToString(), x.Value?.ToString())).ToList();
        
        count = properties.Data.Count;
    }

    public void OnAfterDeserialize()
    {
        Data.Clear();
    }
}

[Serializable]
struct KVP
{
    public string key, value;

    public KVP(string k, string v)
    {
        key = k;
        value = v;
    }
}