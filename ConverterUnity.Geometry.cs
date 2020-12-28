using Objects.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Mesh = Objects.Geometry.Mesh;

namespace Objects.Converter.Unity
{
  public partial class ConverterUnity
  {
    /// <summary>
    /// 
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <param name="z"></param>
    /// <returns></returns>
    public Vector3 PointByCoordinates(double x, double y, double z)
    {
      // switch y and z
      return new Vector3((float)x, (float)z, (float)y);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="ptValues"></param>
    /// <returns></returns>
    public Vector3 ArrayToPoint(double[] ptValues)
    {
      double x = ptValues[0];
      double y = ptValues[1];
      double z = ptValues[2];
      // switch y and z
      return new Vector3((float)x, (float)z, (float)y);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="arr"></param>
    /// <returns></returns>
    public Vector3[] ArrayToPoints(IEnumerable<double> arr)
    {
      if (arr.Count() % 3 != 0) throw new Exception("Array malformed: length%3 != 0.");

      Vector3[] points = new Vector3[arr.Count() / 3];
      var asArray = arr.ToArray();
      for (int i = 2, k = 0; i < arr.Count(); i += 3)
        points[k++] = PointByCoordinates(asArray[i - 2], asArray[i - 1], asArray[i]);

      return points;
    }



    /// <summary>
    /// 
    /// </summary>
    /// <param name="obj"></param>
    /// <returns></returns>
    public Point PointToSpeckle(UnityPoint obj)
    {
      Vector3 p = obj.point;

      //switch y and z
      return new Point(p.x, p.z, p.y);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="point"></param>
    /// <returns></returns>
    public UnityPoint PointToNative(Point point)
    {

      Vector3 newPt = ArrayToPoint(point.value.ToArray());
      return new UnityPoint(point.speckle_type, newPt);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="line"></param>
    /// <returns></returns>
    public UnityPolyline LineToNative(Line line)
    {
      Vector3[] points = ArrayToPoints(line.value);

      if (points.Length == 0) return null;

      return new UnityPolyline(line.speckle_type, points);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="polyline"></param>
    /// <returns></returns>
    public UnityPolyline PolylineToNative(Polyline polyline)
    {
      Vector3[] points = ArrayToPoints(polyline.value);

      if (points.Length == 0) return null;


      return new UnityPolyline(polyline.speckle_type, points);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="curve"></param>
    /// <returns></returns>
    public UnityPolyline CurveToNative(Curve curve)
    {
      Vector3[] points = ArrayToPoints(curve.displayValue.value);

      if (points.Length == 0) return null;


      return new UnityPolyline(curve.speckle_type, points);
    }


    /// <summary>
    /// 
    /// </summary>
    /// <param name="speckleMesh"></param>
    /// <returns></returns>
    public UnityMesh MeshToNative(Mesh speckleMesh)
    {
      if (speckleMesh.vertices.Count == 0 || speckleMesh.faces.Count == 0)
      {
        return null;
      }

      //convert speckleMesh.faces into triangle array           
      List<int> tris = new List<int>();
      int i = 0;
      while (i < speckleMesh.faces.Count)
      {
        if (speckleMesh.faces[i] == 0)
        {
          //Triangles
          tris.Add(speckleMesh.faces[i + 1]);
          tris.Add(speckleMesh.faces[i + 3]);
          tris.Add(speckleMesh.faces[i + 2]);
          i += 4;
        }
        else
        {
          //Quads to triangles
          tris.Add(speckleMesh.faces[i + 1]);
          tris.Add(speckleMesh.faces[i + 3]);
          tris.Add(speckleMesh.faces[i + 2]);

          tris.Add(speckleMesh.faces[i + 3]);
          tris.Add(speckleMesh.faces[i + 1]);
          tris.Add(speckleMesh.faces[i + 4]);

          i += 5;
        }
      }

      return new UnityMesh(speckleMesh.speckle_type, ArrayToPoints(speckleMesh.vertices), tris.ToArray());
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="brep"></param>
    /// <returns></returns>
    //public static UnityMesh ToNative(this Brep brep)
    //{
    //  Mesh speckleMesh = brep.displayValue;
    //  return MeshToNative(speckleMesh);
    //}


  }
}
