using System.Collections.Generic;
using Speckle.ConnectorUnity.NativeCache;
using UnityEngine;

#if UNITY_EDITOR
using Speckle.ConnectorUnity.NativeCache.Editor;
#endif

namespace Speckle.ConnectorUnity
{
    #nullable enable
    public static class NativeCacheFactory
    {

        public static List<AbstractNativeCache> GetDefaultNativeCacheSetup(bool generateAssets = false)
        {
#if UNITY_EDITOR
            if (generateAssets)
            {
                return GetEditorCacheSetup();
            }
#endif
            return GetStandaloneCacheSetup();

        }

        public static List<AbstractNativeCache> GetStandaloneCacheSetup()
        {
            return new List<AbstractNativeCache>()
            {
                ScriptableObject.CreateInstance<ResourcesNativeCache>(),
                ScriptableObject.CreateInstance<MemoryNativeCache>(),
            };
        }
        
#if UNITY_EDITOR
        public static List<AbstractNativeCache> GetEditorCacheSetup()
        {
            return new List<AbstractNativeCache>()
            {
                ScriptableObject.CreateInstance<ResourcesNativeCache>(),
                ScriptableObject.CreateInstance<AssetDBNativeCache>(),
                ScriptableObject.CreateInstance<MemoryNativeCache>(),
            };
        }
#endif
    }
}

