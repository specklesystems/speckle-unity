using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;

#endif


#if UNITY_EDITOR
#endif

[CustomPropertyDrawer( typeof( ReadOnlyAttribute ) )]
public class ReadOnlyDrawer : PropertyDrawer {

    public override void OnGUI( Rect position,
        SerializedProperty property,
        GUIContent label )
        {
            GUI.enabled = false;
            EditorGUI.PropertyField( position, property, label, true );
            GUI.enabled = true;
        }

}

public class ReadOnlyAttribute : PropertyAttribute { }