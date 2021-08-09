using System.Collections.Generic;
using System.Linq;
using Pcx;
using UnityEngine;

namespace Speckle_Connector
{
  public class SpeckleCloud : MonoBehaviour
  {

    //compute shader based renderer from keijiro
    //>>>https://github.com/keijiro/Pcx/tree/master/Packages
    private PointCloudRenderer _pcxRenderer;
    private PointCloudData _data;

    public string uints { get; set; }
    // TODO fetch points from point cloud data object instead 
    public List<Vector3> points { get; set; }
    public List<Color32> colors { get; set; }

    // TODO maybe add support for using particle system?
    public void ToMono(List<Vector3> v, List<Color32> c)
    {
      if (v == null)
      {
        Debug.Log("Points for Speckle Cloud are not valid");
        return;
      }
      c ??= v.Select(pts => new Color32(255, 255, 255, 255)).ToList();

      points = v;
      colors = c;
      
      UpdateContent();
    }

    private void UpdateContent()
    {
      _data = ScriptableObject.CreateInstance<PointCloudData>();
      _data.Initialize(points, colors);

      if (_pcxRenderer == null)
      {
        _pcxRenderer = gameObject.GetComponent<PointCloudRenderer>();
        if (_pcxRenderer == null)
          _pcxRenderer = gameObject.AddComponent<PointCloudRenderer>();
      }
      _pcxRenderer.sourceData = _data;
    }
  }
}