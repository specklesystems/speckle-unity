#nullable enable
using System;
using System.Collections.Generic;
using Speckle.Core.Api;
using Speckle.Core.Credentials;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

namespace Speckle.ConnectorUnity.Wrappers.Selection.Editor
{
    [CustomPropertyDrawer(typeof(AccountSelection))]
    public sealed class AccountSelectionDrawer : OptionSelectionDrawer<Account>
    {
        protected override bool DisplayRefresh => true;
        protected override string FormatOption(Account o) => $"{o.userInfo.email} | {o.serverInfo.name}";
        
        public AccountSelectionDrawer()
        {
            details = new (string, Func<Account, string>)[]
            {
                ("Id", s => s.userInfo.id),
                ("Name", s => s.userInfo.name),
                ("Email", s => s.userInfo.email),
                ("Company", s => s.userInfo.company),
                ("Server", s => s.serverInfo.name),
                ("URL", s => s.serverInfo.url),
                ("Description", s => s.serverInfo.description),
            };
        }
    }
    
    [CustomPropertyDrawer(typeof(StreamSelection))]
    public sealed class StreamSelectionDrawer : OptionSelectionDrawer<Stream>
    {
        protected override bool DisplayRefresh => true;
        protected override string FormatOption(Stream o) => $"{o.name}";

        public StreamSelectionDrawer()
        {
            properties = new []{$"<{nameof(StreamSelection.StreamsLimit)}>k__BackingField"};
            
            details = new (string, Func<Stream, string>)[]
            {
                ("Stream id", s => s.id),
                ("Description", s => s.description),
                ("Is Public", s => s.isPublic.ToString()),
                ("Role", s => s.role),
                ("Created at", s => s.createdAt.ToString()),
                ("Updated at", s => s.updatedAt.ToString()),
            };
        }
    }
    
    [CustomPropertyDrawer(typeof(BranchSelection))]
    public sealed class BranchSelectionDrawer : OptionSelectionDrawer<Branch>
    {
        protected override bool DisplayRefresh => true;
        protected override string FormatOption(Branch o) => $"{o.name}";

        public BranchSelectionDrawer()
        {
            properties = new []
            {
                $"<{nameof(BranchSelection.BranchesLimit)}>k__BackingField",
                $"<{nameof(BranchSelection.CommitsLimit)}>k__BackingField",
            };
            
            details = new (string, Func<Branch, string>)[]
            {
                ("Description", s => s.description),
            };
        }
    }
    
    [CustomPropertyDrawer(typeof(CommitSelection))]
    public sealed class CommitSelectionDrawer : OptionSelectionDrawer<Commit>
    {
        protected override string FormatOption(Commit o) => $"{o.message} - {o.id}";
 
        public CommitSelectionDrawer()
        {
            details = new (string, Func<Commit, string>)[]
            {
                ("Commit Id", s => s.id),
                ("Author Name", s => s.authorName),
                ("Created At", s => s.createdAt.ToString()),
                ("Source Application", s => s.sourceApplication),
                ("Reference Object Id", s => s.referencedObject),
            };
        }
    }
    
    
    public abstract class OptionSelectionDrawer<TOption> : PropertyDrawer where TOption : class
    {
        private const float RefreshButtonWidthScale = 0.2f;
        private const float PrefixIndentation = 100f;
        protected readonly float DetailsTextHeight = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

        protected virtual bool DisplayRefresh => false;
        protected abstract string FormatOption(TOption o);
        protected virtual int GUIDetailsPropertyCount => properties.Length + details.Length;

        protected string[] properties = { };

        protected (string, Func<TOption, string>)[] details = { };
        
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

        protected virtual void OnGUIDetails(Rect position, SerializedProperty property, GUIContent label, TOption? selection)
        {
            position.height = DetailsTextHeight;

            foreach (string subPropertyName in properties)
            {
                position.y += DetailsTextHeight;
                var subProperty = property.FindPropertyRelative(subPropertyName);
                EditorGUI.PropertyField(position, subProperty);
            }

            EditorGUI.indentLevel++;
            EditorGUI.BeginDisabledGroup(true);

            foreach (var (name, func) in details)
            {
                position.y += DetailsTextHeight;
                string text = selection != null ? func(selection) : "";
                EditorGUI.TextField(position, name, text);
            }

            EditorGUI.EndDisabledGroup();
            EditorGUI.indentLevel--;
        }
        
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            var t = (OptionSelection<TOption>)fieldInfo.GetValue(property.serializedObject.targetObject);

            var selectionRect = position; 
            selectionRect.x += PrefixIndentation + 5;
            selectionRect.width -= PrefixIndentation + 5;
            
            TOption? selectedOption = t.Selected;
            
            // Options selection
            {
                
                var popupSize = DisplayRefresh
                    ? new Rect(selectionRect.x, selectionRect.y, selectionRect.width * (1-RefreshButtonWidthScale), DetailsTextHeight)
                    : selectionRect;

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

                // Optional refresh
                if (DisplayRefresh)
                {
                    var buttonSize = new Rect(selectionRect.x + popupSize.width , selectionRect.y, selectionRect.width * RefreshButtonWidthScale, DetailsTextHeight);
                    if (GUI.Button(buttonSize, "Refresh"))
                    {
                        EditorApplication.delayCall += t.RefreshOptions;
                    }
                }
            }
            
            // Collapsable details
            { 
                int visiblePropCount = property.isExpanded ? GUIDetailsPropertyCount : 0;
                var detailsHeight = new Vector2(PrefixIndentation, DetailsTextHeight + visiblePropCount * DetailsTextHeight);
                var foldoutRect = new Rect(position.position,  detailsHeight);
                property.isExpanded = EditorGUI.BeginFoldoutHeaderGroup(foldoutRect, property.isExpanded, label);
                if (property.isExpanded)
                {
                    OnGUIDetails(position, property, label, selectedOption);
                }
                EditorGUI.EndFoldoutHeaderGroup();
            }
            

            EditorGUI.EndProperty();
            //EditorUtility.SetDirty(property.serializedObject.targetObject);

        }
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var standardHeight = EditorGUIUtility.singleLineHeight;
            
            if (!property.isExpanded) return standardHeight + EditorGUIUtility.standardVerticalSpacing;

            var detailsHeight = GUIDetailsPropertyCount * (standardHeight + EditorGUIUtility.standardVerticalSpacing);
            
            return standardHeight + detailsHeight + EditorGUIUtility.standardVerticalSpacing + EditorGUIUtility.standardVerticalSpacing;
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
