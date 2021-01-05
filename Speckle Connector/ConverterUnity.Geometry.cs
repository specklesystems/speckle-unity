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
    #region helper methods

    /// <summary>
    /// 
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <param name="z"></param>
    /// <returns></returns>
    public Vector3 PointByCoordinates(double x, double y, double z, string units)
    {
      // switch y and z
      return new Vector3((float) ScaleToNative(x, units), (float) ScaleToNative(z, units),
        (float) ScaleToNative(y, units));
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="ptValues"></param>
    /// <returns></returns>
    public Vector3 ArrayToPoint(double[] ptValues, string units)
    {
      double x = ptValues[0];
      double y = ptValues[1];
      double z = ptValues[2];

      return PointByCoordinates(x, y, z, units);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="arr"></param>
    /// <returns></returns>
    public Vector3[] ArrayToPoints(IEnumerable<double> arr, string units)
    {
      if (arr.Count() % 3 != 0) throw new Exception("Array malformed: length%3 != 0.");

      Vector3[] points = new Vector3[arr.Count() / 3];
      var asArray = arr.ToArray();
      for (int i = 2, k = 0; i < arr.Count(); i += 3)
        points[k++] = PointByCoordinates(asArray[i - 2], asArray[i - 1], asArray[i], units);

      return points;
    }

    #endregion

    #region ToSpeckle

    //TODO: more of these

    /// <summary>
    /// 
    /// </summary>
    /// <param name="obj"></param>
    /// <returns></returns>
    public Point PointToSpeckle(Vector3 p)
    {
      //switch y and z
      return new Point(p.x, p.z, p.y);
    }


    /// <summary>
    /// Converts a Speckle mesh to a GameObject with a mesh renderer
    /// </summary>
    /// <param name="speckleMesh"></param>
    /// <returns></returns>
    public Mesh MeshToSpeckle(GameObject gameObject)
    {
      //TODO: support multiple filters?
      var filter = gameObject.GetComponent<MeshFilter>();
      if (filter == null)
      {
        return null;
      }

      //convert triangle array into speckleMesh faces     
      List<int> faces = new List<int>();
      int i = 0;
      while (i < filter.mesh.triangles.Length)
      {
        faces.Add(0);

        faces.Add(filter.mesh.triangles[i + 0]);
        faces.Add(filter.mesh.triangles[i + 2]);
        faces.Add(filter.mesh.triangles[i + 1]);
        i += 3;
      }

      var localToWorld = gameObject.transform.localToWorldMatrix;

      var mesh = new Mesh();
      mesh.units = ModelUnits;
      mesh.vertices = filter.mesh.vertices
        .SelectMany(v => PointToSpeckle(localToWorld.MultiplyPoint3x4(v)).value).ToList();
      mesh.faces = faces;

      return mesh;
    }

    #endregion

    #region ToNative

    private GameObject NewPointBasedGameObject(Vector3[] points, string name)
    {
      if (points.Length == 0) return null;

      float pointDiameter = 1; //TODO: figure out how best to change this?

      var go = new GameObject();
      go.name = name;

      var lineRenderer = go.AddComponent<LineRenderer>();

      lineRenderer.positionCount = points.Length;
      lineRenderer.SetPositions(points);
      lineRenderer.numCornerVertices = lineRenderer.numCapVertices = 8;
      lineRenderer.startWidth = lineRenderer.endWidth = pointDiameter;

      return go;
    }

    /// <summary>
    /// Converts a Speckle ioint to a GameObject with a line renderer
    /// </summary>
    /// <param name="point"></param>
    /// <returns></returns>
    public GameObject PointToNative(Point point)
    {
      Vector3 newPt = ArrayToPoint(point.value.ToArray(), point.units);
      return NewPointBasedGameObject(new Vector3[2] {newPt, newPt}, point.speckle_type);
    }


    /// <summary>
    /// Converts a Speckle line to a GameObject with a line renderer
    /// </summary>
    /// <param name="line"></param>
    /// <returns></returns>
    public GameObject LineToNative(Line line)
    {
      Vector3[] points = ArrayToPoints(line.value, line.units);

      return NewPointBasedGameObject(points, line.speckle_type);
    }

    /// <summary>
    /// Converts a Speckle polyline to a GameObject with a line renderer
    /// </summary>
    /// <param name="polyline"></param>
    /// <returns></returns>
    public GameObject PolylineToNative(Polyline polyline)
    {
      Vector3[] points = ArrayToPoints(polyline.value, polyline.units);

      return NewPointBasedGameObject(points, polyline.speckle_type);
    }

    /// <summary>
    /// Converts a Speckle curve to a GameObject with a line renderer
    /// </summary>
    /// <param name="curve"></param>
    /// <returns></returns>
    public GameObject CurveToNative(Curve curve)
    {
      Vector3[] points = ArrayToPoints(curve.displayValue.value, curve.units);

      return NewPointBasedGameObject(points, curve.speckle_type);
    }


    /// <summary>
    /// Converts a Speckle mesh to a GameObject with a mesh renderer
    /// </summary>
    /// <param name="speckleMesh"></param>
    /// <returns></returns>
    public GameObject MeshToNative(Mesh speckleMesh)
    {
      if (speckleMesh.vertices.Count == 0 || speckleMesh.faces.Count == 0)
      {
        return null;
      }

      var recentreMeshTransforms = true; //TODO: figure out how best to change this?

      var verts = ArrayToPoints(speckleMesh.vertices, speckleMesh.units).ToList();
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


      var go = new GameObject();
      go.name = speckleMesh.speckle_type;

      var mesh = go.AddComponent<MeshFilter>().mesh;
      var meshRenderer = go.AddComponent<MeshRenderer>();

      if (verts.Count >= 65535)
        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;

      // center transform pivot according to the bounds of the model
      if (recentreMeshTransforms)
      {
        Bounds meshBounds = new Bounds();
        meshBounds.center = verts[0];

        verts.ForEach(x => meshBounds.Encapsulate(x));

        go.transform.position = meshBounds.center;

        // offset mesh vertices
        for (int l = 0; l < verts.Count; l++)
        {
          verts[l] -= meshBounds.center;
        }
      }

      // assign mesh properties
      mesh.vertices = verts.ToArray();
      mesh.triangles = tris.ToArray();

      mesh.RecalculateNormals();
      mesh.RecalculateTangents();

      //generate uvs doesn't work as intended. Leaving out for now
      //GenerateUVs (ref mesh);

      //Add mesh collider
      MeshCollider mc = go.AddComponent<MeshCollider>();
      mc.sharedMesh = mesh;

      return go;
    }

    #endregion
  }
}