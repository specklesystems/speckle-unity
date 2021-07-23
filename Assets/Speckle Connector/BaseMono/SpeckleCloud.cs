using System.Collections.Generic;
using System.Linq;
using Pcx;
using UnityEngine;

namespace Speckle_Connector.MonoBase
{
  public class SpeckleCloud : MonoBehaviour
  {

    //compute shader based renderer from keijiro
    //>>>https://github.com/keijiro/Pcx/tree/master/Packages
    private PointCloudRenderer _pcxRenderer;
    private PointCloudData _data;

    public void FromBase(List<Vector3> values, List<Color32> colors)
    {
      if (values == null)
      {
        Debug.Log("Points for Speckle Cloud are not valid");
        return;
      }
      colors ??= values.Select(v => new Color32(255, 255, 255, 255)).ToList();

      _data = ScriptableObject.CreateInstance<PointCloudData>();
      _data.Initialize(values, colors);

      if (_pcxRenderer == null)
      {
        _pcxRenderer = gameObject.GetComponent<PointCloudRenderer>();
        if (_pcxRenderer == null)
          _pcxRenderer = gameObject.AddComponent<PointCloudRenderer>();
      }
      _pcxRenderer.sourceData = _data;
    }

    // TODO maybe add support for using particle system?
  }
}