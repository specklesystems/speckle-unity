using System;
using System.Collections.Generic;
using Speckle.Core.Models;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Speckle.ConnectorUnity.NativeCache
{
    public class AggregateNativeCache : AbstractNativeCache
    {
        [SerializeField, SerializeReference]
        public List<AbstractNativeCache> nativeCaches;
        
        public override bool TryGetObject<T>(Base speckleObject, out T nativeObject) where T : class
        {
            foreach (var c in nativeCaches)
            {
                if (c.TryGetObject(speckleObject, out nativeObject)) return true;
            }
            nativeObject = null;
            return false;
        }

        public override bool TrySaveObject(Base speckleObject, Object nativeObject)
        {
            bool hasSavedSomewhere = false;
            
            foreach (var c in nativeCaches)
            {
                hasSavedSomewhere = hasSavedSomewhere || c.TrySaveObject(speckleObject, nativeObject);
            }
            
            return hasSavedSomewhere;
        }

        public override void BeginWrite()
        {
            base.BeginWrite();
            foreach (var c in nativeCaches)
            {
                c.BeginWrite();
            }
        }

        public override void FinishWrite()
        {
            foreach (var c in nativeCaches)
            {
                try
                {
                    c.FinishWrite();
                }
                catch (Exception e)
                {
                    Debug.LogError(e);
                }
            }
            base.FinishWrite();
        }
        
    }
}
