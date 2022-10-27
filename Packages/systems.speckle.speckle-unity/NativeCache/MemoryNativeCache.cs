using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Speckle.Core.Models;
using Object = UnityEngine.Object;

namespace Speckle.ConnectorUnity.NativeCache
{
    #nullable enable
    /// <summary>
    /// In memory native object cache
    /// </summary>
    public sealed class MemoryNativeCache : AbstractNativeCache
    {
        public IDictionary<string, List<Object>> LoadedAssets { get; set; } = new Dictionary<string, List<Object>>();
        
        public override bool TryGetObject<T>(Base speckleObject, [NotNullWhen(true)] out T? nativeObject) where T : class
        {
            if (TryGetObject(speckleObject, out List<Object>? e))
            {
                nativeObject = (T?)e.FirstOrDefault(x => x is T);
                return nativeObject != null;
            }
            
            nativeObject = null;
            return false;
        }

        public bool TryGetObject(Base speckleObject, [NotNullWhen(true)] out List<Object>? nativeObject)
        {
            return LoadedAssets.TryGetValue(speckleObject.id, out nativeObject);
        }

        public override bool TrySaveObject(Base speckleObject, Object nativeObject)
        {
            if (LoadedAssets.ContainsKey(speckleObject.id))
            {
                LoadedAssets[speckleObject.id].Add(nativeObject);
                return true;
            }
            
            LoadedAssets.Add(speckleObject.id, new List<Object>{nativeObject});
            return true;
        }
    }
}
