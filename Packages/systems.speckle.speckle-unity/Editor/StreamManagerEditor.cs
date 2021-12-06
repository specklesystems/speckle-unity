using UnityEditor;
using UnityEngine.UIElements;

namespace Speckle.ConnectorUnity
{
  [CustomEditor(typeof(StreamManager))]
  public class StreamManagerEditor : Editor
  {
    private SpeckleStreamElement connector;

    public override VisualElement CreateInspectorGUI()
    {
      var mono = (StreamManager)target;

      var root = new VisualElement();
      
      connector = new SpeckleStreamElement(mono.streamInstance);

      root.Add(connector);
      return root;
    }
  }
}