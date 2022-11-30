using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Specialized;
using Speckle.Core.Api;
using Speckle.Core.Models;
using UnityEngine;

namespace Speckle.ConnectorUnity.Wrappers
{
    /// <summary>
    /// This class gets attached to GOs and is used to store Speckle's metadata when sending / receiving
    /// </summary>
    [AddComponentMenu("Speckle/Speckle Properties")]
    [Serializable, DisallowMultipleComponent]
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

        [SerializeField, HideInInspector]
        private string _serializedSpeckleType;
        private Type _speckleType = typeof(Base);
        public Type SpeckleType {
            get
            {
                return _speckleType ??= typeof(Base);
            }
            set
            {
                
                Debug.Assert(typeof(Base).IsAssignableFrom(value));
                Debug.Assert(!value.IsAbstract);
                
                _speckleType = value;
                _hasChanged = true;
            }
            
        }

        public SpeckleProperties()
        {
            _data = new ObservableConcurrentDictionary<string, object>();
            _data.CollectionChanged += CollectionChangeHandler;
            _hasChanged = true;
            SpeckleType = typeof(Base);
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
            _serializedSpeckleType = SpeckleType.AssemblyQualifiedName;
        }
    
        public void OnAfterDeserialize()
        {
            Base speckleData = Operations.Deserialize(_serializedData);
            Data = speckleData.GetMembers();
            _hasChanged = false;
            
            try
            {
                _speckleType = Type.GetType(_serializedSpeckleType);
            }
            catch (Exception e)
            {
                Debug.LogWarning(e, this);
                _speckleType = typeof(Base);
            }
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