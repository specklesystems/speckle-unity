using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
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
        public IDictionary<string, Object> LoadedAssets { get; set; } = new Dictionary<string, Object>();
        
        public override bool TryGetObject<T>(Base speckleObject, [NotNullWhen(true)] out T? nativeObject) where T : class
        {
            if (TryGetObject(speckleObject, out Object? e) && e is T t)
            {
                nativeObject = t;
                return true;
            }
            
            nativeObject = null;
            return false;
        }

        public bool TryGetObject(Base speckleObject, [NotNullWhen(true)] out Object? nativeObject)
        {
            return LoadedAssets.TryGetValue(speckleObject.id, out nativeObject);
        }

        public override bool TrySaveObject(Base speckleObject, Object nativeObject)
        {
            return LoadedAssets.TryAdd(speckleObject.id, nativeObject);
        }
    }
}
