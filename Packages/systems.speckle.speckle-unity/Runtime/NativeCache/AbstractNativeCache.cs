#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using Speckle.ConnectorUnity.Utils;
using Speckle.Core.Models;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Speckle.ConnectorUnity.NativeCache
{
    [ExecuteAlways]
    public abstract class AbstractNativeCache : ScriptableObject
    {
        
        protected bool isWriting = false;
        public abstract bool TryGetObject<T>(Base speckleObject, [NotNullWhen(true)] out T? nativeObject) where T : Object;

        public abstract bool TrySaveObject(Base speckleObject, Object nativeObject);

        /// <summary>
        /// Prepares this <see cref="AbstractNativeCache"/> for save operations
        /// </summary>
        public virtual void BeginWrite()
        {
            isWriting = true;
        }

        /// <summary>
        /// Call when finished performing save operations.
        /// Instructs the <see cref="AbstractNativeCache"/> to finish writing anything to disk
        /// </summary>
        public virtual void FinishWrite()
        {
            isWriting = false;
        }

        protected virtual void OnDisable()
        {
            FinishWrite();
        }
        
    }

    public static class AssetHelpers
    {
        public static string? GetAssetFolder(Type nativeType, string path)
        {
            const string format = "{0}/{1}";
            
            if (nativeType == typeof(Mesh))
            {
                return string.Format(format, path, "Geometry");
            }
            if (nativeType  == typeof(Material))
            {
                return string.Format(format, path, "Materials");
            }
            if (nativeType == typeof(GameObject))
            {
                return string.Format(format, path, "Prefabs");
            }
            return null;
        }
        
        
        private static readonly HashSet<char> InvalidChars = Path.GetInvalidFileNameChars().ToHashSet();
        public static string GetAssetName(Base speckleObject, Type nativeType)
        {
            string suffix = GetAssetSuffix(nativeType);
            string name = CoreUtils.GenerateObjectName(speckleObject);

            string sanitisedName = new(name.Where(x => !InvalidChars.Contains(x)).ToArray());
            return $"{sanitisedName}{suffix}";
        }

        public static string GetAssetSuffix(Type nativeType)
        {
            if (nativeType == typeof(Material)) return ".mat";
            if (nativeType == typeof(GameObject)) return ".prefab";
            return ".asset";
        }
        
        [Obsolete("use " + nameof(CoreUtils.GenerateObjectName))]
        public static string GetObjectName(Base speckleObject)
        {
            string objectName = speckleObject["name"] as string ?? speckleObject.speckle_type.Split(':').Last();
            return $"{objectName} - {speckleObject.id}";
        }
    }
}
