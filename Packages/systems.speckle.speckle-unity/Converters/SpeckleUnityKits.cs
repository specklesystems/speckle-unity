using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Objects.Converter.Unity
{
  [CreateAssetMenu(menuName = "Speckle/Create Speckle Kit Manager", fileName = "KitsAndConverters", order = 0)]
  public class SpeckleUnityKits : ScriptableObject
  {
    public List<SpeckleConvertersSO> converters;

    public static SpeckleUnityKits Instance { get; private set; }

    public void OnEnable()
    {
      Instance = this;
      
      Debug.Log("enablecalled");
    }

  }
}