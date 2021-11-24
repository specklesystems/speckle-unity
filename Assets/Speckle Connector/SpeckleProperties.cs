using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Specialized;
using Speckle.Core.Api;
using Speckle.Core.Models;
using UnityEngine;

namespace Speckle.ConnectorUnity
{
    /// <summary>
    /// This class gets attached to GOs and is used to store Speckle's metadata when sending / receiving
    /// </summary>
    [Serializable]
    public class SpeckleProperties : MonoBehaviour, ISerializationCallbackReceiver
    {

        [SerializeField, HideInInspector]
        private string _serializedData = "";
    
        private bool _hasChanged;
        private ObservableConcurrentDictionary<string, object> _data;

        public IDictionary<string, object> Data
        {
            get => _data;
            set
            {
                ((ICollection<KeyValuePair<string, object>>) _data).Clear();

                foreach (var kvp in value)
                {
                    _data.Add(kvp.Key, kvp.Value);
                }
            }
        }

        public SpeckleProperties()
        {
            _data = new ObservableConcurrentDictionary<string, object>();
            _data.CollectionChanged += CollectionChangeHandler;
            _hasChanged = true;
        }
    
        private void CollectionChangeHandler(object sender, NotifyCollectionChangedEventArgs e)
        {
            _hasChanged = true;
        }

        public void OnBeforeSerialize()
        {
            if (!_hasChanged) return;
      
            _serializedData = Operations.Serialize(new SpeckleData(Data));
            _hasChanged = false;
        }
    
        public void OnAfterDeserialize()
        {
            Base speckleData = Operations.Deserialize(_serializedData);
            Data = speckleData.GetMembers();
            _hasChanged = false;
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
    }
}