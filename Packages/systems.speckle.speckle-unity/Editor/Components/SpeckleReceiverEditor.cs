using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Speckle.Core.Api;
using Speckle.Core.Models;
using UnityEditor;
using UnityEngine;

#nullable enable
namespace Speckle.ConnectorUnity.Components.Editor
{
    [CustomEditor(typeof(SpeckleReceiver))]
    [CanEditMultipleObjects]
    public class SpeckleReceiverEditor : UnityEditor.Editor
    {
        private bool foldOutStatus = true;
        private Texture2D? previewImage;

        public void OnEnable()
        {
            var speckleReceiver = (SpeckleReceiver) target;
            UpdatePreviewImage();
            speckleReceiver.OnCommitSelectionChange.AddListener(_ => UpdatePreviewImage());
        }

        private void UpdatePreviewImage()
        {
            previewImage = null;
            ((SpeckleReceiver)target).GetPreviewImage(t => previewImage = t);
        }
        
        public override async void OnInspectorGUI()
        {
            var speckleReceiver = (SpeckleReceiver) target;
            
            
            //Draw events in a collapsed region
            DrawDefaultInspector();

            foldOutStatus = EditorGUILayout.Foldout(foldOutStatus, "Preview Image");
            if (foldOutStatus)
            {
                Rect rect = GUILayoutUtility.GetAspectRect(7f/4f);
                if(previewImage != null) GUI.DrawTexture(rect, previewImage);
            }
            
            bool receive = GUILayout.Button("Receive!");
                
            if (receive)
            {
                await Receive(speckleReceiver);
            }
        }

        public async Task<GameObject?> Receive(SpeckleReceiver speckleReceiver)
        {
            speckleReceiver.CancellationTokenSource?.Cancel();
            if (!speckleReceiver.GetSelection(out Client? client, out _, out Commit? commit, out string? error))
            {
                Debug.LogWarning($"Not ready to receive: {error}", speckleReceiver);
                return null;
            }
            
            Base? commitObject = await ReceiveCommit(speckleReceiver, client.ServerUrl);

            if (commitObject == null) return null;
            
            var gameObject = Convert(speckleReceiver, commitObject, commit.id);
            Debug.Log($"Successfully received and converted commit: {commit.id}", target);
            return gameObject;
        }

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
        

        private async Task<Base?> ReceiveCommit(SpeckleReceiver speckleReceiver, string serverLogName)
        {
            string message = $"Receiving data from {serverLogName}...";
            EditorUtility.DisplayProgressBar(message, "", 0);

            var totalObjectCount = 1;
            void OnTotalChildrenKnown(int count)
            {
                totalObjectCount = count;
            };
            
            void OnProgress(ConcurrentDictionary<string, int> dict)
            {
                var currentProgress = dict.Values.Average();
                var progress = (float) currentProgress / totalObjectCount;
                EditorApplication.delayCall += () =>
                {
                    bool shouldCancel = EditorUtility.DisplayCancelableProgressBar(message, 
                        $"{currentProgress}/{totalObjectCount}",
                        progress);
                    
                    if (shouldCancel)
                    {
                        CancelReceive();
                    }
                };
            };
            
            void OnError(string message, Exception e)
            {
                if (e is not OperationCanceledException)
                {
                    Debug.LogError($"Receive failed: {message}\n{e}", speckleReceiver);
                }
                CancelReceive();
            };

            Base? commitObject = null;
            try
            {
                speckleReceiver.OnTotalChildrenCountKnown.AddListener(OnTotalChildrenKnown);
                speckleReceiver.OnReceiveProgressAction.AddListener(OnProgress);
                speckleReceiver.OnErrorAction.AddListener(OnError);
                commitObject = await speckleReceiver.ReceiveAsync();
                if (commitObject == null)
                {
                    Debug.LogWarning($"Receive warning: Receive operation returned null", speckleReceiver);
                }
            }
            finally
            {
                speckleReceiver.OnTotalChildrenCountKnown.RemoveListener(OnTotalChildrenKnown);
                speckleReceiver.OnReceiveProgressAction.RemoveListener(OnProgress);
                speckleReceiver.OnErrorAction.RemoveListener(OnError);
                EditorApplication.delayCall += EditorUtility.ClearProgressBar;
            }

            return commitObject;
        }

        private void CancelReceive()
        {
            ((SpeckleReceiver)target).CancellationTokenSource?.Cancel();
            EditorApplication.delayCall += EditorUtility.ClearProgressBar;
        }
    }
}
