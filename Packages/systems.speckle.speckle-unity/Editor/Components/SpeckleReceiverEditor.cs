using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Speckle.Core.Models;
using Speckle.Core.Models.GraphTraversal;
using UnityEditor;
using UnityEngine;

namespace Speckle.ConnectorUnity.Components.Editor
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(SpeckleReceiver))]
    public class SpeckleReceiverEditor : UnityEditor.Editor
    {
        private SerializedProperty _accountSelection;
        private SerializedProperty _streamSelection;
        private SerializedProperty _branchSelection;
        private SerializedProperty _commitSelection;

#nullable enable
        private static bool _generateAssets;
        private bool _foldOutStatus = true;
        private Texture2D? _previewImage;

        public override async void OnInspectorGUI()
        {
            var speckleReceiver = (SpeckleReceiver)target;

            //Selection
            EditorGUILayout.PropertyField(_accountSelection);
            EditorGUILayout.PropertyField(_streamSelection, new GUIContent("Project"));
            EditorGUILayout.PropertyField(_branchSelection, new GUIContent("Model"));
            EditorGUILayout.PropertyField(_commitSelection, new GUIContent("Version"));

            //Preview image
            {
                _foldOutStatus = EditorGUILayout.Foldout(_foldOutStatus, "Preview Image");
                if (_foldOutStatus)
                {
                    Rect rect = GUILayoutUtility.GetAspectRect(7f / 4f);
                    if (_previewImage != null)
                        GUI.DrawTexture(rect, _previewImage);
                }
            }

            //TODO: Draw events in a collapsed region

            //Receive settings
            {
                bool prev = GUI.enabled;
                GUI.enabled = !speckleReceiver.IsReceiving;
                //Receive button
                bool userRequestedReceive = GUILayout.Button("Receive!");

                bool selection = EditorGUILayout.ToggleLeft("Generate Assets", _generateAssets);
                if (_generateAssets != selection)
                {
                    _generateAssets = selection;
                    UpdateGenerateAssets();
                }
                GUI.enabled = prev;

                if (speckleReceiver.IsReceiving)
                {
                    var value = Progress.globalProgress; //NOTE: this may include non-speckle items...
                    var percent = Math.Max(0, Mathf.Ceil(value * 100));
                    var rect = EditorGUILayout.GetControlRect(
                        false,
                        EditorGUIUtility.singleLineHeight
                    );
                    EditorGUI.ProgressBar(rect, value, $"{percent}%");
                }
                else if (userRequestedReceive)
                {
                    var id = Progress.Start(
                        "Receiving Speckle Model",
                        "Fetching data from Speckle",
                        Progress.Options.Sticky
                    );
                    Progress.ShowDetails();

                    try
                    {
                        await ReceiveSelection(id).ConfigureAwait(true);
                        Progress.Finish(id);
                    }
                    catch (OperationCanceledException ex)
                    {
                        Progress.Finish(id, Progress.Status.Canceled);
                        Debug.Log($"Receive operation cancelled\n{ex}", this);
                    }
                    catch (Exception ex)
                    {
                        Progress.Finish(id, Progress.Status.Failed);
                        Debug.LogError($"Receive operation failed {ex}", this);
                    }
                    finally
                    {
                        EditorUtility.ClearProgressBar();
                    }
                }
            }
        }

        public void OnEnable()
        {
            Init();

            _accountSelection = serializedObject.FindProperty(
                $"<{nameof(SpeckleReceiver.Account)}>k__BackingField"
            );
            _streamSelection = serializedObject.FindProperty(
                $"<{nameof(SpeckleReceiver.Stream)}>k__BackingField"
            );
            _branchSelection = serializedObject.FindProperty(
                $"<{nameof(SpeckleReceiver.Branch)}>k__BackingField"
            );
            _commitSelection = serializedObject.FindProperty(
                $"<{nameof(SpeckleReceiver.Commit)}>k__BackingField"
            );
        }

        public void Reset()
        {
            Init();
        }

        private void Init()
        {
            var speckleReceiver = (SpeckleReceiver)target;
            UpdatePreviewImage();
            speckleReceiver.OnCommitSelectionChange.AddListener(_ => UpdatePreviewImage());
            UpdateGenerateAssets();
        }

        private void UpdatePreviewImage()
        {
            _previewImage = null;
            ((SpeckleReceiver)target).GetPreviewImage(t => _previewImage = t);
        }

        private async Task ReceiveSelection(int progressId)
        {
            var speckleReceiver = (SpeckleReceiver)target;

            bool shouldCancel = false;

            Progress.RegisterCancelCallback(
                progressId,
                () =>
                {
                    speckleReceiver.Cancel();
                    shouldCancel = true;
                    return true;
                }
            );

            Base commitObject;
            try
            {
                var token = speckleReceiver.BeginOperation();
                commitObject = await Task.Run(
                        async () => await ReceiveCommit(progressId).ConfigureAwait(false),
                        token
                    )
                    .ConfigureAwait(true);
            }
            finally
            {
                speckleReceiver.FinishOperation();
            }

            int childrenConverted = 0;
            int childrenFailed = 0;

            int totalChildren = (int)Math.Min(commitObject.totalChildrenCount, int.MaxValue);
            float totalChildrenFloat = commitObject.totalChildrenCount;

            var convertProgress = Progress.Start(
                "Converting To Native",
                "Preparing...",
                Progress.Options.Indefinite | Progress.Options.Sticky,
                progressId
            );

            bool BeforeConvert(TraversalContext context)
            {
                Base b = context.Current;

                //NOTE: progress wont reach 100% because not all objects are convertable
                float progress = (childrenConverted + childrenFailed) / totalChildrenFloat;

                if (shouldCancel)
                    return false;

                shouldCancel = EditorUtility.DisplayCancelableProgressBar(
                    "Converting To Native...",
                    $"{b.speckle_type} - {b.id}",
                    progress
                );

                return !shouldCancel;
            }

            foreach (
                var conversionResult in speckleReceiver.Converter.RecursivelyConvertToNative_Enumerable(
                    commitObject,
                    speckleReceiver.transform,
                    BeforeConvert
                )
            )
            {
                Base speckleObject = conversionResult.SpeckleObject;
                if (conversionResult.WasSuccessful(out _, out var ex))
                {
                    childrenConverted++;
                }
                else
                {
                    childrenFailed++;
                    Debug.LogWarning(
                        $"Failed to convert Speckle object of type {speckleObject.speckle_type}\n{ex}",
                        this
                    );
                }

                Progress.Report(
                    progressId,
                    childrenConverted + childrenFailed,
                    totalChildren,
                    "Receiving objects"
                );

                if (shouldCancel)
                    break;
            }

            var resultString = $"{childrenConverted} {nameof(GameObject)}s created";
            if (childrenFailed != 0)
                resultString += $", {childrenFailed} objects failed to convert!";

            Debug.Log(
                shouldCancel
                    ? $"Stopped converting to native: The operation has been cancelled - {resultString}\n "
                    : $"Finished converting to native.\n{resultString}",
                speckleReceiver
            );

            Progress.Finish(convertProgress);

            if (shouldCancel)
                throw new OperationCanceledException(
                    "Conversion operation canceled through editor dialogue"
                );
        }

        private void UpdateGenerateAssets()
        {
            var speckleReceiver = (SpeckleReceiver)target;
            speckleReceiver.Converter.AssetCache.nativeCaches =
                NativeCacheFactory.GetDefaultNativeCacheSetup(_generateAssets);
        }

        private async Task<Base> ReceiveCommit(int progressId)
        {
            var speckleReceiver = (SpeckleReceiver)target;

            string serverLogName = speckleReceiver.Account.Client?.ServerUrl ?? "Speckle";

            int transport = Progress.Start(
                $"Downloading data from {serverLogName}",
                "Waiting...",
                Progress.Options.Sticky,
                progressId
            );
            int deserialize = Progress.Start(
                "Deserializing data",
                "Waiting...",
                Progress.Options.Sticky,
                progressId
            );
            Progress.SetPriority(transport, Progress.Priority.High);

            var totalObjectCount = 1;
            void OnTotalChildrenKnown(int count)
            {
                totalObjectCount = count;
                Progress.Report(progressId, 0, totalObjectCount, "Receiving objects");
            }

            void OnProgress(ConcurrentDictionary<string, int> dict)
            {
                bool r = dict.TryGetValue("RemoteTransport", out int rtProgress);
                bool l = dict.TryGetValue("SQLite", out int ltProgress);
                if (r || l)
                {
                    var fetched = (rtProgress + ltProgress);
                    Progress.Report(
                        transport,
                        fetched,
                        totalObjectCount,
                        $"{fetched}/{totalObjectCount}"
                    );
                }

                if (dict.TryGetValue("DS", out int tsProgress))
                {
                    tsProgress--; //The root object isn't included, so we add an extra 1
                    Progress.Report(
                        deserialize,
                        tsProgress,
                        totalObjectCount,
                        $"{tsProgress}/{totalObjectCount}"
                    );
                    Progress.Report(progressId, tsProgress, totalObjectCount);
                }
            }

            Base commitObject;
            try
            {
                speckleReceiver.OnTotalChildrenCountKnown.AddListener(OnTotalChildrenKnown);
                speckleReceiver.OnReceiveProgressAction.AddListener(OnProgress);
                commitObject = await speckleReceiver
                    .ReceiveAsync(speckleReceiver.CancellationToken)
                    .ConfigureAwait(false);
                Progress.Finish(transport);
                Progress.Finish(deserialize);
            }
            catch (OperationCanceledException)
            {
                Progress.Finish(transport, Progress.Status.Canceled);
                Progress.Finish(deserialize, Progress.Status.Canceled);
                throw;
            }
            catch (Exception)
            {
                Progress.Finish(transport, Progress.Status.Failed);
                Progress.Finish(deserialize, Progress.Status.Failed);
                throw;
            }
            finally
            {
                speckleReceiver.OnTotalChildrenCountKnown.RemoveListener(OnTotalChildrenKnown);
                speckleReceiver.OnReceiveProgressAction.RemoveListener(OnProgress);
            }

            return commitObject;
        }

        [MenuItem("GameObject/Speckle/Speckle Connector", false, 10)]
        static void CreateCustomGameObject(MenuCommand menuCommand)
        {
            // Create a custom game object
            GameObject go = new("Speckle Connector");
            // Ensure it gets reparented if this was a context click (otherwise does nothing)
            GameObjectUtility.SetParentAndAlign(go, menuCommand.context as GameObject);
            // Register the creation in the undo system
            Undo.RegisterCreatedObjectUndo(go, "Create " + go.name);
            Selection.activeObject = go;

            go.AddComponent<RecursiveConverter>();
            go.AddComponent<SpeckleReceiver>();
            go.AddComponent<ReceiveFromURL>();
            go.AddComponent<SpeckleSender>();

#if UNITY_2021_2_OR_NEWER
            var icon = AssetDatabase.LoadAssetAtPath<Texture2D>(
                "Packages/systems.speckle.speckle-unity/Editor/Gizmos/logo128.png"
            );
            EditorGUIUtility.SetIconForObject(go, icon);
#endif
        }
    }
}
