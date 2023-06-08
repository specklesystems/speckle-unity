using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Speckle.ConnectorUnity.Utils;
using Speckle.Core.Api;
using Speckle.Core.Logging;
using Speckle.Core.Models;
using UnityEditor;
using UnityEngine;

#nullable enable
namespace Speckle.ConnectorUnity.Components.Editor
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(SpeckleReceiver))]
    public class SpeckleReceiverEditor : UnityEditor.Editor
    {
        private static bool generateAssets = false;
        private bool foldOutStatus = true;
        private Texture2D? previewImage;

        public void OnEnable()
        {
            Init();
        }
        
        public void Reset()
        {
            Init();
        }

        private void Init()
        {
            var speckleReceiver = (SpeckleReceiver) target;
            UpdatePreviewImage();
            speckleReceiver.OnCommitSelectionChange.AddListener(_ => UpdatePreviewImage());
            UpdateGenerateAssets();
        }

        private void UpdatePreviewImage()
        {
            previewImage = null;
            ((SpeckleReceiver)target).GetPreviewImage(t => previewImage = t);
        }

        public override async void OnInspectorGUI()
        {
            var speckleReceiver = (SpeckleReceiver)target;

            DrawDefaultInspector();

            //Preview image
            foldOutStatus = EditorGUILayout.Foldout(foldOutStatus, "Preview Image");
            if (foldOutStatus)
            {
                Rect rect = GUILayoutUtility.GetAspectRect(7f / 4f);
                if (previewImage != null) GUI.DrawTexture(rect, previewImage);
            }


            //Receive button
            bool receive = GUILayout.Button("Receive!");

            bool selection = EditorGUILayout.ToggleLeft("Generate Assets", generateAssets);
            if (generateAssets != selection)
            {
                generateAssets = selection;
                UpdateGenerateAssets();
            }


            //TODO: Draw events in a collapsed region


            if (receive)
            {
                try
                {
                    Base commitObject = await ReceiveCommit();

                    int childrenConverted = 0;
                    float totalChildren = commitObject.totalChildrenCount;

                    foreach (var e in speckleReceiver.Converter.RecursivelyConvertToNative_Enumerable(
                                 commitObject,
                                 speckleReceiver.transform))
                    {
                        Base speckleObject = e.traversalContext.current;
                        
                        float progress =
                            childrenConverted++ /
                            totalChildren; //wont reach 100% because not all objects are convertable

                        string resultMessage = e.WasSuccessful(out _, out var ex)
                            ? $"Successfully converted {CoreUtils.GenerateObjectName(speckleObject)}"
                            : $"Failed to convert {CoreUtils.GenerateObjectName(speckleObject)}: {ex}";

                        EditorUtility.DisplayProgressBar(
                            "Converting To Native...",
                            resultMessage,
                            progress);

                    }
                }
                finally
                {
                    EditorUtility.ClearProgressBar();
                }
            }

        }

        private void UpdateGenerateAssets()
        {
            var speckleReceiver = (SpeckleReceiver) target;
            speckleReceiver.Converter.AssetCache.nativeCaches = NativeCacheFactory.GetDefaultNativeCacheSetup(generateAssets);
        }

        [Obsolete]
        public async Task<GameObject?> ReceiveAndConvert(SpeckleReceiver speckleReceiver)
        {
            speckleReceiver.CancellationTokenSource?.Cancel();
            if (!speckleReceiver.GetSelection(out _, out _, out Commit? commit, out string? error))
            {
                Debug.LogWarning($"Not ready to receive: {error}", speckleReceiver);
                return null;
            }
            
            Base? commitObject = await ReceiveCommit().ConfigureAwait(true);;

            if (commitObject == null) return null;
            
            var gameObject = Convert(speckleReceiver, commitObject, commit.id);
            Debug.Log($"Successfully received and converted commit: {commit.id}", target);
            return gameObject;
        }

        [Obsolete]
        private GameObject Convert(SpeckleReceiver receiver, Base commitObject, string name)
        {
            //Convert Speckle Objects
            int childrenConverted = 0;
            float totalChildren = commitObject.totalChildrenCount; 
            
            void BeforeConvertCallback(Base b)
            {
                //TODO: this is an incorrect way of measuring progress, as totalChildren != total convertable children
                float progress = childrenConverted++ / totalChildren;
                
                EditorUtility.DisplayProgressBar("Converting To Native...", 
                    $"{b.speckle_type} - {b.id}",
                    progress);
            }

            var go = receiver.ConvertToNativeWithCategories(commitObject,
                name, BeforeConvertCallback);
            go.transform.SetParent(receiver.transform);
            return go;
        }
        
        private async Task<Base> ReceiveCommit()
        {
            var speckleReceiver = (SpeckleReceiver)target;
            
            string serverLogName = speckleReceiver.Account.Client?.ServerUrl ?? "Speckle";
            string message = $"Receiving data from {serverLogName}...";
            
            using CancellationTokenSource cancellationSource = new();
            EditorUtility.DisplayProgressBar(message, "Making request", 0f);

            var totalObjectCount = 1;
            void OnTotalChildrenKnown(int count)
            {
                totalObjectCount = count;
                EditorApplication.delayCall += () => EditorUtility.DisplayProgressBar(message, "Established connection", 0f);
            }
            
            void OnProgress(ConcurrentDictionary<string, int> dict)
            {
                var currentProgress = dict.Values.Average();
                var progress = (float) currentProgress / totalObjectCount;
                EditorApplication.delayCall += () =>
                {
                    bool shouldCancel = EditorUtility.DisplayCancelableProgressBar(message, 
                        $"Downloading data {currentProgress}/{totalObjectCount}",
                        progress);
                    
                    if (shouldCancel)
                    {
                        cancellationSource.Cancel();
                    }
                };
            }
            
            //TODO cancellation but think about disposal!
            if (speckleReceiver.IsReceiving) throw new InvalidOperationException("A pending receive operation has already started");

            Base commitObject;
            try
            {
                speckleReceiver.OnTotalChildrenCountKnown.AddListener(OnTotalChildrenKnown);
                speckleReceiver.OnReceiveProgressAction.AddListener(OnProgress);
                commitObject = await speckleReceiver.ReceiveAsync(cancellationSource.Token).ConfigureAwait(false);
            }
            catch(Exception ex)
            {
                throw new SpeckleException("Failed to receive commit", ex);
            }
            finally
            {
                speckleReceiver.OnTotalChildrenCountKnown.RemoveListener(OnTotalChildrenKnown);
                speckleReceiver.OnReceiveProgressAction.RemoveListener(OnProgress);
                EditorApplication.delayCall += EditorUtility.ClearProgressBar;
            }

            return commitObject;
        }

        [MenuItem("GameObject/Speckle/Speckle Connector", false, 10)]
        static void CreateCustomGameObject(MenuCommand menuCommand) {
            // Create a custom game object
            GameObject go = new GameObject("Speckle Connector");
            // Ensure it gets reparented if this was a context click (otherwise does nothing)
            GameObjectUtility.SetParentAndAlign(go, menuCommand.context as GameObject);
            // Register the creation in the undo system
            Undo.RegisterCreatedObjectUndo(go, "Create " + go.name);
            Selection.activeObject = go;

            go.AddComponent<RecursiveConverter>();
            go.AddComponent<SpeckleReceiver>();
            go.AddComponent<SpeckleSender>();
            
#if UNITY_2021_2_OR_NEWER
            var icon = AssetDatabase.LoadAssetAtPath<Texture2D>("Packages/systems.speckle.speckle-unity/Editor/Gizmos/logo128.png");
            EditorGUIUtility.SetIconForObject(go, icon);
#endif
        }
    }
}
