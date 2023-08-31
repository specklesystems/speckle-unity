#nullable enable
using UnityEditor;
using UnityEngine;

namespace Speckle.ConnectorUnity.Components.Editor
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(ReceiveFromURL))]
    public class ReceiveFromURLEditor: UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            var speckleReceiver = (ReceiveFromURL)target;

            DrawDefaultInspector();

            bool isBusy = speckleReceiver.IsBusy();

            GUI.enabled = !isBusy;
            if (GUILayout.Button("Receive!"))
            {
                speckleReceiver.Receive();
            }
            GUI.enabled = isBusy;
            if (GUILayout.Button("Cancel!"))
            {
                speckleReceiver.Cancel();
            }

            
        }
        
    }
}
