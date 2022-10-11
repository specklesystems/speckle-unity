using System.Diagnostics.CodeAnalysis;
using Speckle.Core.Models;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Speckle.ConnectorUnity.NativeCache
{
    #nullable enable
    /// <summary>
    /// Loads existing assets from <see cref="Resources"/>
    /// optionally accepting byName overrides
    /// </summary>
    public sealed class ResourcesNativeCache : AbstractNativeCache
    {
        public bool matchByName = true;
        
        public override bool TryGetObject<T>(Base speckleObject, [NotNullWhen(true)] out T? nativeObject) where T : class
        {
            if (matchByName)
            {
                string? speckleName = speckleObject["name"] as string ?? speckleObject["Name"] as string;
                if (!string.IsNullOrWhiteSpace(speckleName))
                {
                    nativeObject = Resources.Load<T>(speckleName);
                    if (nativeObject != null) return true;
                }
            } 
            
            nativeObject = Resources.Load<T>(AssetHelpers.GetAssetName(speckleObject, typeof(T)));
            return nativeObject != null;
        }

        public override bool TrySaveObject(Base speckleObject, Object nativeObject)
        {
            // Pass
            return false;
        }
    }
}
