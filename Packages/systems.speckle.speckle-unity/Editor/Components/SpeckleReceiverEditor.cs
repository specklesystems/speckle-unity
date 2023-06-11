using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Speckle.ConnectorUnity.Utils;
using Speckle.Core.Api;
using Speckle.Core.Logging;
using Speckle.Core.Models;
using Speckle.Core.Models.GraphTraversal;
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
        
        public override async void OnInspectorGUI()
        {
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
                    await ReceiveSelection();
                }
                catch (OperationCanceledException ex)
                {
                    Debug.Log($"Receive operation cancelled\n{ex}", this);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Failed to receive selection {ex}", this);
                }
                finally
                {
                    EditorUtility.ClearProgressBar();
                }
            }
        }
        
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

        private async Task ReceiveSelection()
        {
            var speckleReceiver = (SpeckleReceiver)target;
            
            Base commitObject = await ReceiveCommit();
            
            int childrenConverted = 0;
            float totalChildren = commitObject.totalChildrenCount;

            bool shouldCancel = false;

            bool BeforeConvert(TraversalContext context)
            {
                Base b = context.current;

                //NOTE: progress wont reach 100% because not all objects are convertable
                float progress = childrenConverted / totalChildren;

                shouldCancel = EditorUtility.DisplayCancelableProgressBar(
                    "Converting To Native...",
                    $"{b.speckle_type} - {b.id}",
                    progress);
                
                
                return true;
            }

            foreach (var conversionResult in speckleReceiver.Converter.RecursivelyConvertToNative_Enumerable(
                         commitObject,
                         speckleReceiver.transform,
                         BeforeConvert))
            {
                Base speckleObject = conversionResult.SpeckleObject;
                if (conversionResult.WasSuccessful(out _, out var ex))
                {
                    childrenConverted++;
                }
                else
                {
                    Debug.LogWarning(
                        $"Failed to convert Speckle object of type {speckleObject.speckle_type}\n{ex}",
                        this);
                }

                if (shouldCancel) break;
            }

            Debug.Log(
                shouldCancel
                    ? $"Stopped converting to native. Created {childrenConverted} {nameof(GameObject)}s: Responding to cancel through editor"
                    : $"Finished converting to native. Created {childrenConverted} {nameof(GameObject)}s ",
                this);
        }

        private void UpdateGenerateAssets()
        {
            var speckleReceiver = (SpeckleReceiver) target;
            speckleReceiver.Converter.AssetCache.nativeCaches = NativeCacheFactory.GetDefaultNativeCacheSetup(generateAssets);
        }

                private async Task<Base> ReceiveCommit()
        {
            var speckleReceiver = (SpeckleReceiver)target;
            
            speckleReceiver.BeginOperation();
            
            string serverLogName = speckleReceiver.Account.Client?.ServerUrl ?? "Speckle";
            string message = $"Receiving data from {serverLogName}...";

            EditorUtility.DisplayProgressBar(message, "Fetching data", 0f);

            var totalObjectCount = 1;
            void OnTotalChildrenKnown(int count)
            {
                totalObjectCount = count;
                EditorApplication.delayCall += () => EditorUtility.DisplayProgressBar(message, $"Fetching data {0}/{totalObjectCount}", 0f);
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
                        speckleReceiver.CancellationTokenSource!.Cancel();
                    }
                };
            }
            
            Base commitObject;
            try
            {
                speckleReceiver.OnTotalChildrenCountKnown.AddListener(OnTotalChildrenKnown);
                speckleReceiver.OnReceiveProgressAction.AddListener(OnProgress);
                commitObject = await speckleReceiver.ReceiveAsync(speckleReceiver.CancellationToken).ConfigureAwait(false);
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
                speckleReceiver.FinishOperation();
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
