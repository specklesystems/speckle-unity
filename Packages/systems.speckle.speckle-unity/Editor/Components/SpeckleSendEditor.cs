using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Threading.Tasks;
using Speckle.Core.Models;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using Component = UnityEngine.Component;

namespace Speckle.ConnectorUnity.Components.Editor
{
    public enum SelectionFilter
    {
        [Tooltip("Convert all children of this GameObject")]
        Children,

        [Tooltip(
            "Convert GameObjects currently selected in the hierarchy (consider padlocking this inspector)"
        )]
        Selection,

        [InspectorName("All (excl. disabled)")]
        [Tooltip("Convert all GameObjects (excluding disabled) in the active scene")]
        Enabled,

        [Tooltip("Convert all GameObjects (including disabled) in the active scene")]
        [InspectorName("All (incl. disabled)")]
        All,
    }

    [CustomEditor(typeof(SpeckleSender))]
    [CanEditMultipleObjects]
    public class SpeckleSendEditor : UnityEditor.Editor
    {
        private SerializedProperty _accountSelection;
        private SerializedProperty _streamSelection;
        private SerializedProperty _branchSelection;

#nullable enable
        private SelectionFilter _selectedFilter = SelectionFilter.Children;

        public void OnEnable()
        {
            _accountSelection = serializedObject.FindProperty(
                $"<{nameof(SpeckleSender.Account)}>k__BackingField"
            );
            _streamSelection = serializedObject.FindProperty(
                $"<{nameof(SpeckleSender.Stream)}>k__BackingField"
            );
            _branchSelection = serializedObject.FindProperty(
                $"<{nameof(SpeckleSender.Branch)}>k__BackingField"
            );
        }

        public override async void OnInspectorGUI()
        {
            //Selection
            EditorGUILayout.PropertyField(_accountSelection);
            EditorGUILayout.PropertyField(_streamSelection, new GUIContent("Project"));
            EditorGUILayout.PropertyField(_branchSelection, new GUIContent("Model"));

            bool shouldSend = GUILayout.Button("Send!");
            _selectedFilter = (SelectionFilter)
                EditorGUILayout.EnumPopup("Selection", _selectedFilter);

            if (shouldSend)
            {
                await ConvertAndSend();
            }
        }

        public async Task<string?> ConvertAndSend()
        {
            var speckleSender = (SpeckleSender)target;

            if (!speckleSender.GetSelection(out _, out _, out _, out string? error))
            {
                Debug.LogWarning($"Not ready to send: {error}", speckleSender);
                return null;
            }

            RecursiveConverter converter = speckleSender.Converter;
            Base data = _selectedFilter switch
            {
                SelectionFilter.All => ConvertAll(converter),
                SelectionFilter.Enabled => ConvertEnabled(converter),
                SelectionFilter.Children => ConvertChildren(converter),
                SelectionFilter.Selection => ConvertSelection(converter),
                _
                    => throw new InvalidEnumArgumentException(
                        nameof(_selectedFilter),
                        (int)_selectedFilter,
                        _selectedFilter.GetType()
                    ),
            };

            if (data["@objects"] is IList l && l.Count == 0)
            {
                Debug.LogWarning("Nothing to send", speckleSender);
                return null;
            }

            return await speckleSender.SendDataAsync(data, true);
        }

        private Base ConvertChildren(RecursiveConverter converter)
        {
            return converter.RecursivelyConvertToSpeckle(
                new[] { ((Component)target).gameObject },
                _ => true
            );
        }

        private Base ConvertSelection(RecursiveConverter converter)
        {
            ISet<GameObject> selection = Selection
                .GetFiltered<GameObject>(SelectionMode.Deep)
                .ToImmutableHashSet();
            return converter.RecursivelyConvertToSpeckle(
                SceneManager.GetActiveScene().GetRootGameObjects(),
                go => selection.Contains(go)
            );
        }

        private Base ConvertAll(RecursiveConverter converter)
        {
            return converter.RecursivelyConvertToSpeckle(
                SceneManager.GetActiveScene().GetRootGameObjects(),
                _ => true
            );
        }

        private Base ConvertEnabled(RecursiveConverter converter)
        {
            return converter.RecursivelyConvertToSpeckle(
                SceneManager.GetActiveScene().GetRootGameObjects(),
                go => go.activeInHierarchy
            );
        }
    }
}
