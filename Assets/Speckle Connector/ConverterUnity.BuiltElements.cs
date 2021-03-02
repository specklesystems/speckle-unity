using Objects.BuiltElements;
using Objects.Geometry;
using UnityEngine;

namespace Objects.Converter.Unity
{
  public partial class ConverterUnity
  {
    /// <summary>
    /// Converts a Speckle View3D to a GameObject
    /// </summary>
    /// <param name="point"></param>
    /// <returns></returns>
    public GameObject View3DToNative(View3D speckleView)
    {
      var go = new GameObject(speckleView.name);
      var camera = go.AddComponent<Camera>();
      camera.transform.position = VectorByCoordinates(speckleView.origin.x, speckleView.origin.y, speckleView.origin.z,
        speckleView.origin.units);
      camera.transform.forward = VectorByCoordinates(speckleView.forwardDirection.x, speckleView.forwardDirection.y,
        speckleView.forwardDirection.z, speckleView.forwardDirection.units);
      camera.transform.up = VectorByCoordinates(speckleView.upDirection.x, speckleView.upDirection.y,
        speckleView.upDirection.z, speckleView.upDirection.units);

      SetSpeckleData(go, speckleView);
      return go;
    }
  }
}