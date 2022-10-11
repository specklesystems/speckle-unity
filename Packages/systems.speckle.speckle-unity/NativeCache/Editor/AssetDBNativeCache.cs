using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using Speckle.Core.Models;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Speckle.ConnectorUnity.NativeCache.Editor
{
    /// <summary>
    /// Uses Unity's AssetDatabase to load existing assets from a given path
    /// </summary>
    public sealed class AssetDBNativeCache : AbstractNativeCache
    {
        public string path = "Assets/Resources";

        private MemoryNativeCache readCache;

        private Dictionary<Base, Object> writeBuffer = new();

#nullable enable
        private void Awake()
        {
            readCache = CreateInstance<MemoryNativeCache>();
        }
        
        public override bool TryGetObject<T>(Base speckleObject, [NotNullWhen(true)] out T? nativeObject) where T : class
        {
            if(readCache.TryGetObject(speckleObject, out nativeObject))
                return true;
            
            Type nativeType = typeof(T);
            string? folder = AssetHelpers.GetAssetFolder(nativeType, path);
            if (folder == null) return false;
            
            string assetName = AssetHelpers.GetAssetName(speckleObject, nativeType);
            string assetPath = $"{folder}/{assetName}";
            
            nativeObject = AssetDatabase.LoadAssetAtPath<T>(assetPath);
            return nativeObject != null;
        }

        public override bool TrySaveObject(Base speckleObject, Object nativeObject)
        {
            writeBuffer.TryAdd(speckleObject, nativeObject);
            return readCache.TrySaveObject(speckleObject, nativeObject);
        }

        public bool WriteObject(Base speckleObject, Object nativeObject)
        {
            Type nativeType = nativeObject.GetType();
            string? folder = AssetHelpers.GetAssetFolder(nativeType, path);
            
            if (folder == null) return false;
            if (!CreateDirectory(folder)) return false;

            string assetName = AssetHelpers.GetAssetName(speckleObject, nativeType);
            string assetPath = $"{folder}/{assetName}";
            
            // Special case for GameObjects, we want to use PrefabUtility
            if (nativeObject is GameObject go)
            {
                PrefabUtility.SaveAsPrefabAssetAndConnect(go, assetPath, InteractionMode.AutomatedAction);
                return true;
            }
            
            // Exit early if there's already an asset
            Object? existing = AssetDatabase.LoadAssetAtPath(assetPath, nativeObject.GetType());
            if (existing != null)
            {
                Debug.LogWarning($"Failed to write asset as one already existed at path: {folder}/{assetName}", this);
                return false;
            }

            AssetDatabase.CreateAsset(nativeObject, $"{folder}/{assetName}");
            return true;
        }

        
        public void WriteAssets(IEnumerable<KeyValuePair<Base, Object>> assets)
        {
            //Write Asset Data
            try
            {
                AssetDatabase.StartAssetEditing();
                int i = 0;
                int count = writeBuffer.Count;
                foreach(var kvp in assets)
                {
                    if (kvp.Value is GameObject p)
                    {
                        continue;
                    }
                    
                    EditorUtility.DisplayProgressBar("Writing assets", $"Writing asset for {kvp.Value.name}", (float)i / count);
                    WriteObject(kvp.Key, kvp.Value);
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
                EditorUtility.DisplayProgressBar("Writing assets", $"Finishing writing assets", 1f);
                AssetDatabase.SaveAssets();
                EditorUtility.ClearProgressBar();
            }
        }
        
        public override void FinishWrite()
        {
            if (!isWriting) return;
            
            var prefabs = writeBuffer.Where(x => x.Value is GameObject);
            var notPrefabs = writeBuffer.Where(x => x.Value is not GameObject);
            
            WriteAssets(notPrefabs);
            WriteAssets(prefabs);
            
            writeBuffer.Clear();
            if (readCache != null) readCache.LoadedAssets.Clear();
            
            base.FinishWrite();
        }

        private static bool CreateDirectory(string directoryPath)
        {
            if (Directory.Exists(directoryPath))
                return true;
            
            var info = Directory.CreateDirectory(directoryPath);
            AssetDatabase.Refresh();
            return info.Exists;
        }
        

        [ContextMenu("SetPath")]
        internal void SetPath_Menu()
        {
            var selection = EditorUtility.OpenFolderPanel("Set Assets Path", "Assets/Resources", "");
            
            if (selection.StartsWith(Application.dataPath)) {
                path = "Assets" + selection.Substring(Application.dataPath.Length);
            }
            else
            {
                Debug.LogError($"Expected selection to be within {Application.dataPath}");
            }

        }
    }
}
