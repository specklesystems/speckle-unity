
using System;
using System.Collections.Generic;
using Speckle.Core.Api;
using Speckle.Core.Credentials;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEditor.UIElements;
using UnityEngine;

#nullable enable
namespace Speckle.ConnectorUnity
{
    [CustomPropertyDrawer(typeof(AccountSelection))]
    public sealed class AccountSelectionDrawer : OptionSelectionDrawer<Account>
    {
        protected override bool DisplayRefresh => true;
        protected override string FormatOption(Account o) => $"{o.userInfo.email} | {o.serverInfo.name}";
        protected override int GUIDetailsPropertyCount => 4; 
        protected override void OnGUIDetails(Rect position, SerializedProperty property, GUIContent label, Account selection)
        {
            EditorGUI.BeginDisabledGroup(true);
            position.height = DetailsTextHeight;
            
            position.y += DetailsTextHeight;
            EditorGUI.TextField(position, "Id", selection.userInfo.id);
            
            position.y += DetailsTextHeight;
            EditorGUI.TextField(position, "Name", selection.userInfo.name);
            
            position.y += DetailsTextHeight;
            EditorGUI.TextField(position, "Server", selection.serverInfo.name);

            position.y += DetailsTextHeight;
            EditorGUI.TextField(position, "URL", selection.serverInfo.url);
            
            EditorGUI.EndDisabledGroup();
        }
    }
    
    [CustomPropertyDrawer(typeof(StreamSelection))]
    public sealed class StreamSelectionDrawer : OptionSelectionDrawer<Stream>
    {
        protected override bool DisplayRefresh => true;

        protected override string FormatOption(Stream o) => $"{o.name}";
        protected override int GUIDetailsPropertyCount => 7;

        protected override void OnGUIDetails(Rect position, SerializedProperty property, GUIContent label, Stream selection)
        {
            EditorGUI.BeginDisabledGroup(true);
            position.height = DetailsTextHeight;
            
            position.y += DetailsTextHeight;
            EditorGUI.TextField(position, "Stream Id", selection.id);
            
            position.y += DetailsTextHeight;
            EditorGUI.TextField(position, "Description", selection.description);
            
            position.y += DetailsTextHeight;
            EditorGUI.TextField(position, "Is Public", selection.isPublic.ToString());
            
            position.y += DetailsTextHeight;
            EditorGUI.TextField(position, "Role", selection.role);

            position.y += DetailsTextHeight;
            EditorGUI.TextField(position, "Created At", selection.createdAt);
            
            position.y += DetailsTextHeight;
            EditorGUI.TextField(position, "Updated At", selection.updatedAt);

            EditorGUI.EndDisabledGroup();
            
            position.y += DetailsTextHeight;
            var nameField = EditorGUI.PropertyField(position, property.FindPropertyRelative($"<{nameof(StreamSelection.RequestLimit)}>k__BackingField"));

        }
    }
    
    [CustomPropertyDrawer(typeof(BranchSelection))]
    public sealed class BranchSelectionDrawer : OptionSelectionDrawer<Branch>
    {
        protected override bool DisplayRefresh => true;

        protected override string FormatOption(Branch o) => $"{o.name}";
        protected override int GUIDetailsPropertyCount => 1;

        protected override void OnGUIDetails(Rect position, SerializedProperty property, GUIContent label, Branch selection)
        {
            EditorGUI.BeginDisabledGroup(true);
            position.height = DetailsTextHeight;
            
            position.y += DetailsTextHeight;
            EditorGUI.TextField(position, "Description", selection.description);

            EditorGUI.EndDisabledGroup();
        }
    }
    
    [CustomPropertyDrawer(typeof(CommitSelection))]
    public sealed class CommitSelectionDrawer : OptionSelectionDrawer<Commit>
    {
        protected override string FormatOption(Commit o) => $"{o.message} - {o.id}";
        protected override int GUIDetailsPropertyCount => 5;

        protected override void OnGUIDetails(Rect position, SerializedProperty property, GUIContent label, Commit selection)
        {            
            EditorGUI.BeginDisabledGroup(true);
            position.height = DetailsTextHeight;
            
            position.y += DetailsTextHeight;
            EditorGUI.TextField(position, "Commit Id", selection.id);
            
            position.y += DetailsTextHeight;
            EditorGUI.TextField(position, "Author Name", selection.authorName);
            
            position.y += DetailsTextHeight;
            EditorGUI.TextField(position, "Created At", selection.createdAt);
            
            position.y += DetailsTextHeight;
            EditorGUI.TextField(position, "Source Application", selection.sourceApplication);
            
            position.y += DetailsTextHeight;
            EditorGUI.TextField(position, "Reference Object Id", selection.referencedObject);

            EditorGUI.EndDisabledGroup();
 
        }
    }
    
    
    public abstract class OptionSelectionDrawer<TOption> : PropertyDrawer where TOption : class
    {
        private const float RefreshButtonWidthScale = 0.2f;
        private const float PrefixIndentation = 100f;
        protected readonly float DetailsTextHeight = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
        private bool foldOutStatus = false;
        protected virtual bool DisplayRefresh => false;
        protected abstract string FormatOption(TOption o);
        protected abstract int GUIDetailsPropertyCount { get; }

        private string[] GetFormattedOptions(TOption[] options)
        {
            int optionsCount = options.Length;
            string[] choices = new string[optionsCount];
            for (int i = 0; i < optionsCount; i++)
            {
                choices[i] = FormatOption(options[i]);
            }

            return choices;
        }

        protected abstract void OnGUIDetails(Rect position, SerializedProperty property, GUIContent label, TOption selection);
        
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            var t = (OptionSelection<TOption>)fieldInfo.GetValue(property.serializedObject.targetObject);

            var selectionRect = position;//EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);
            selectionRect.x += PrefixIndentation + 5;
            selectionRect.width -= PrefixIndentation + 5;
            
            var popupSize = DisplayRefresh
                ? new Rect(selectionRect.x, selectionRect.y, selectionRect.width * (1-RefreshButtonWidthScale), DetailsTextHeight)
                : selectionRect;
            //TODO: fancy popup

            var selectedOption = t.Selected;
            string selectedChoice = selectedOption != null ? FormatOption(selectedOption) : "";
            
            if (GUI.Button(popupSize, selectedChoice, EditorStyles.popup))
            {
                var windowPos = GUIUtility.GUIToScreenPoint(Event.current.mousePosition);
                var provider = ScriptableObject.CreateInstance<StringListSearchProvider>();
                provider.Title = typeof(TOption).Name;
                provider.listItems = GetFormattedOptions(t.Options);;
                provider.onSetIndexCallback = o => { t.SelectedIndex = o;};
                SearchWindow.Open(new SearchWindowContext(windowPos), provider);
            }

            if (DisplayRefresh)
            {
                var buttonSize = new Rect(selectionRect.x + popupSize.width , selectionRect.y, selectionRect.width * RefreshButtonWidthScale, DetailsTextHeight);
                if (GUI.Button(buttonSize, "Refresh"))
                {
                    t.RefreshOptions();
                }
            }
            
            //TODO drop down with details
            //EditorGUI.DropdownButton(position, "TEST", FocusType.Passive);

            //position.y += DetailsTextHeight;
            { // Details drop down
                int visiblePropCount = property.isExpanded ? GUIDetailsPropertyCount : 0;
                var detailsHeight = new Vector2(PrefixIndentation, DetailsTextHeight + visiblePropCount * DetailsTextHeight);
                var foldoutRect = new Rect(position.position,  detailsHeight);
                property.isExpanded = EditorGUI.BeginFoldoutHeaderGroup(foldoutRect, property.isExpanded, label);
                if (property.isExpanded && selectedOption != null)
                {
                    EditorGUI.indentLevel++;
                    OnGUIDetails(position, property, label, selectedOption);
                    EditorGUI.indentLevel--;
                }
                EditorGUI.EndFoldoutHeaderGroup();
            }

            EditorGUI.EndProperty();
            EditorUtility.SetDirty(property.serializedObject.targetObject);

        }
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var standardHeight = EditorGUIUtility.singleLineHeight;
            
            if (!property.isExpanded) return standardHeight;

            var detailsHeight = GUIDetailsPropertyCount * (standardHeight + EditorGUIUtility.standardVerticalSpacing);
            
            return standardHeight + detailsHeight;
        }
        
    }

    #nullable disable

    public sealed class StringListSearchProvider : ScriptableObject, ISearchWindowProvider
    {
        public string Title { get; set; }
        public string[] listItems;

        public Action<int> onSetIndexCallback;
        
        public List<SearchTreeEntry> CreateSearchTree(SearchWindowContext context)
        {
            List<SearchTreeEntry> searchList = new(listItems.Length + 1) {new SearchTreeGroupEntry(new GUIContent(Title), 0)};
            
            for(int i = 0; i < listItems.Length; i++)
            {
                SearchTreeEntry entry = new SearchTreeEntry(new GUIContent(listItems[i]))
                {
                    level = 1,
                    userData = i
                };
                searchList.Add(entry);
            }
            
            return searchList;
        }

        public bool OnSelectEntry(SearchTreeEntry SearchTreeEntry, SearchWindowContext context)
        {
            onSetIndexCallback?.Invoke((int)SearchTreeEntry.userData);
            
            return true;
        }
    }
    
    
    
}
