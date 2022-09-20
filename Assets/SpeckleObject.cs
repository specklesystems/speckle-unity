using System;
using System.Collections.Generic;
using System.Linq;
using Speckle.ConnectorUnity;
using Speckle.Core.Models;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

//Serialized wrapper around a Base object
[RequireComponent(typeof(SpeckleProperties)), ExecuteAlways]
public class SpeckleObject : MonoBehaviour, ISerializationCallbackReceiver
{

    private SpeckleProperties properties;
    public void Awake()
    {
        properties = GetComponent<SpeckleProperties>();
    }

    public void OnBeforeSerialize()
    {

    }

    public void OnAfterDeserialize()
    {

    }
    
    void OnGUI()
    {
        GUILayout.Label("Value: ");
        foreach (var kvp in properties.Data)
        {
            //var newKey = GUILayout.TextField(kvp.Key, GUILayout.rea);
            GUILayout.Label(kvp.Key);
            var existingValue = kvp.Value;
            var newValue = CreateField(kvp.Value);

            if(newValue != existingValue)
                properties.Data[kvp.Key] = newValue;

            GUILayout.Space(10); 
        }

    }

    private static object CreateField(object v)
    {
        const string label = "Value";
        GUILayoutOption[] options = { GUILayout.ExpandWidth(true) };

        return v switch
        {
            int i => EditorGUILayout.IntField(i, options),
            long l => EditorGUILayout.LongField(l, options),
            float f => EditorGUILayout.FloatField(f, options),
            double d => EditorGUILayout.DoubleField(d, options),
            string s => EditorGUILayout.TextField(s, options),
            bool b => GUILayout.Toggle(b, label, options),
            Enum e => EditorGUILayout.EnumPopup(e, options),
            Object o => EditorGUILayout.ObjectField(label, o, o.GetType(), true, options),
            _ => v
        };
    }
}


// public class SpeckleObject<T> : SpeckleObject where T : Base
// {
//     
//     public SpeckleObject(T value)
//         : base(value)
//     {
//         
//     }
//     //What I want this to look like
//     /*
//      * SubclassOf<Base> SpeckleType; //enforced type
//      * int area
//      * int prop... //explicit props defined here
//      * Map<string, object> DynamicProps...
//      * 
//      */
//     
//     
// }