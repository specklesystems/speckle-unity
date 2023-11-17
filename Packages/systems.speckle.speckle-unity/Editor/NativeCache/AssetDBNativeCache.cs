using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
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
        public const string DefaultPath = "Assets/Resources";
        public string path = DefaultPath;

        private MemoryNativeCache _readCache;

#nullable enable

        void Awake()
        {
            _readCache = CreateInstance<MemoryNativeCache>();
        }

        public override bool TryGetObject<T>(
            Base speckleObject,
            [NotNullWhen(true)] out T? nativeObject
        )
            where T : class
        {
            if (_readCache.TryGetObject(speckleObject, out nativeObject))
                return true;

            Type nativeType = typeof(T);
            if (!GetAssetPath(nativeType, speckleObject, out string? assetPath))
                return false;

            nativeObject = AssetDatabase.LoadAssetAtPath<T>(assetPath);
            return nativeObject != null;
        }

        public override bool TrySaveObject(Base speckleObject, Object nativeObject)
        {
            return WriteObject(speckleObject, nativeObject);
        }

        private bool WriteObject(Base speckleObject, Object nativeObject)
        {
            Type nativeType = nativeObject.GetType();
            if (!GetAssetPath(nativeType, speckleObject, out string? assetPath))
                return false;

            // Special case for GameObjects, we want to use PrefabUtility
            if (nativeObject is GameObject go)
            {
                var prefab = PrefabUtility.SaveAsPrefabAssetAndConnect(
                    go,
                    assetPath,
                    InteractionMode.AutomatedAction
                );
                return _readCache.TrySaveObject(speckleObject, prefab);
            }

            // Exit early if there's already an asset
            Object? existing = AssetDatabase.LoadAssetAtPath(assetPath, nativeObject.GetType());
            if (existing != null)
            {
                Debug.LogWarning(
                    $"Failed to write asset as one already existed at path: {assetPath}",
                    this
                );
                return false;
            }

            AssetDatabase.CreateAsset(nativeObject, $"{assetPath}");
            return _readCache.TrySaveObject(speckleObject, nativeObject);
        }

        public override void FinishWrite()
        {
            if (!isWriting)
                return;
            //AssetDatabase.StopAssetEditing();
            AssetDatabase.SaveAssets();

            if (_readCache != null)
                _readCache.LoadedAssets.Clear();

            base.FinishWrite();
        }

        private bool GetAssetPath(
            Type nativeType,
            Base speckleObject,
            [NotNullWhen(true)] out string? outPath
        )
        {
            string? folder = AssetHelpers.GetAssetFolder(nativeType, path);
            outPath = null;
            if (folder == null)
                return false;
            if (!CreateDirectory(folder))
                return false;

            string assetName = AssetHelpers.GetAssetName(speckleObject, nativeType);
            outPath = $"{folder}/{assetName}";
            return true;
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
        public void SetPath_Menu()
        {
            var selection = EditorUtility.OpenFolderPanel(
                "Set Assets Path",
                "Assets/Resources",
                ""
            );

            if (selection.StartsWith(Application.dataPath))
            {
                path = "Assets" + selection.Substring(Application.dataPath.Length);
            }
            else
            {
                Debug.LogError($"Expected selection to be within {Application.dataPath}");
            }
        }
    }
}
