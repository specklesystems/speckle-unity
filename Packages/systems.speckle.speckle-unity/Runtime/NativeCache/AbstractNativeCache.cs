using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using Speckle.Core.Models;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Speckle.ConnectorUnity.NativeCache
{
    #nullable enable
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
            string name = GenerateObjectName(speckleObject);

            string sanitisedName = new(name.Where(x => !InvalidChars.Contains(x)).ToArray());
            return $"{sanitisedName}{suffix}";
        }
        

        public const string OBJECT_NAME_SEPERATOR = " -- ";
        
        /// <param name="speckleObject">The object to be named</param>
        /// <returns>A human-readable Object name unique to the given <paramref name="speckleObject"/></returns>
        public static string GenerateObjectName(Base speckleObject)
        {
            var prefix = GetFriendlyObjectName(speckleObject) ?? SimplifiedSpeckleType(speckleObject);
            return $"{prefix}{OBJECT_NAME_SEPERATOR}{speckleObject.id}";
        }

        public static string? GetFriendlyObjectName(Base speckleObject)
        {
            return speckleObject["name"] as string
                ?? speckleObject["Name"] as string
                ?? speckleObject["family"] as string;
        }
        
        /// <param name="speckleObject"></param>
        /// <returns>The most significant type in a given <see cref="Base.speckle_type"/></returns>
        public static string SimplifiedSpeckleType(Base speckleObject)
        {
            return speckleObject.speckle_type.Split(':')[^1];
        }
        
        
        public static string GetAssetSuffix(Type nativeType)
        {
            if (nativeType == typeof(Material)) return ".mat";
            if (nativeType == typeof(GameObject)) return ".prefab";
            return ".asset";
        }
        
        [Obsolete("use " + nameof(GenerateObjectName))]
        public static string GetObjectName(Base speckleObject)
        {
            string objectName = speckleObject["name"] as string ?? speckleObject.speckle_type.Split(':').Last();
            return $"{objectName} - {speckleObject.id}";
        }
    }
}
