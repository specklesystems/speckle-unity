using Objects.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Objects.Other;
using Speckle.ConnectorUnity;
using Speckle.Core.Models;
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
    public Vector3 VectorByCoordinates(double x, double y, double z, string units)
    {
      // switch y and z
      return new Vector3((float) ScaleToNative(x, units), (float) ScaleToNative(z, units),
        (float) ScaleToNative(y, units));
    }
    
        public Vector3 VectorFromPoint(Point p)
    {
      // switch y and z
      return new Vector3((float) ScaleToNative(p.x, p.units), (float) ScaleToNative(p.z, p.units),
        (float) ScaleToNative(p.y, p.units));
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="ptValues"></param>
    /// <returns></returns>
    // public Vector3 ArrayToPoint(double[] ptValues, string units)
    // {
    //   double x = ptValues[0];
    //   double y = ptValues[1];
    //   double z = ptValues[2];
    //
    //   return PointByCoordinates(x, y, z, units);
    // }

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
        points[k++] = VectorByCoordinates(asArray[i - 2], asArray[i - 1], asArray[i], units);
    
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
    public Mesh MeshToSpeckle(GameObject go)
    {
      //TODO: support multiple filters?
      var filter = go.GetComponent<MeshFilter>();
      if (filter == null)
      {
        return null;
      }

      //convert triangle array into speckleMesh faces     
      List<int> faces = new List<int>();
      int i = 0;
      //store them here, makes it like 1000000x faster?
      var triangles = filter.mesh.triangles;
      while (i < triangles.Length)
      {
        faces.Add(0);

        faces.Add(triangles[i + 0]);
        faces.Add(triangles[i + 2]);
        faces.Add(triangles[i + 1]);
        i += 3;
      }

      var mesh = new Mesh();
      // get the speckle data from the go here
      // so that if the go comes from speckle, typed props will get overridden below
      GetSpeckleData(mesh, go);

      mesh.units = ModelUnits;
      
      var vertices = filter.mesh.vertices;
      foreach (var vertex in vertices)
      {
        var p = go.transform.TransformPoint(vertex);
        var sp = PointToSpeckle(p);
        mesh.vertices.Add(sp.x);
        mesh.vertices.Add(sp.y);
        mesh.vertices.Add(sp.z);
      }
      
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
    /// Converts a Speckle point to a GameObject with a line renderer
    /// </summary>
    /// <param name="point"></param>
    /// <returns></returns>
    public GameObject PointToNative(Point point)
    {
      Vector3 newPt = VectorByCoordinates(point.x, point.y, point.z, point.units);

      var go = NewPointBasedGameObject(new Vector3[2] {newPt, newPt}, point.speckle_type);
      SetSpeckleData(go, point);
      return go;
    }
    

    

    /// <summary>
    /// Converts a Speckle line to a GameObject with a line renderer
    /// </summary>
    /// <param name="line"></param>
    /// <returns></returns>
    public GameObject LineToNative(Line line)
    {
      var points = new List<Vector3> {VectorFromPoint(line.start), VectorFromPoint(line.end)};

      var go = NewPointBasedGameObject(points.ToArray(), line.speckle_type);
      SetSpeckleData(go, line);
      return go;
    }

    /// <summary>
    /// Converts a Speckle polyline to a GameObject with a line renderer
    /// </summary>
    /// <param name="polyline"></param>
    /// <returns></returns>
    public GameObject PolylineToNative(Polyline polyline)
    {
      var points = polyline.points.Select(x => VectorFromPoint(x));

      var go = NewPointBasedGameObject(points.ToArray(), polyline.speckle_type);
      SetSpeckleData(go, polyline);
      return go;
    }

    /// <summary>
    /// Converts a Speckle curve to a GameObject with a line renderer
    /// </summary>
    /// <param name="curve"></param>
    /// <returns></returns>
    public GameObject CurveToNative(Curve curve)
    {
      var points = ArrayToPoints(curve.points, curve.units);
      var go = NewPointBasedGameObject(points.ToArray(), curve.speckle_type);
      SetSpeckleData(go, curve);
      return go;
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

      //todo support more complex materials
      var shader = Shader.Find("Standard");
      var mat = new Material(shader);

      var speckleMaterial = speckleMesh["renderMaterial"];
      if (speckleMaterial != null && speckleMaterial is RenderMaterial rm)
      {
        // 1. match shader by name, if any
        shader = Shader.Find(rm.name);
        if (shader != null)
        {
          mat = new Material(shader);
        }
        else
        {
          // 2. re-create material by setting diffuse color and transparency on standard shaders
          shader = Shader.Find("Transparent/Diffuse");
          mat = new Material(shader);
          var c = rm.diffuse.ToUnityColor();
          mat.color = new Color(c.r, c.g, c.b, Convert.ToSingle(rm.opacity));
        }
      }

      // 3. if not renderMaterial was passed, the default shader will be used 
      meshRenderer.material = mat;


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
      mc.convex = true;


      SetSpeckleData(go, speckleMesh);
      return go;
    }

    #endregion

    private void SetSpeckleData(GameObject go, Base @base)
    {
      var sd = go.AddComponent<SpeckleData>();
      var meshprops = typeof(Mesh).GetProperties(BindingFlags.Instance | BindingFlags.Public).Select(x=>x.Name).ToList();
      
      //get members, but exclude mesh props to avoid issues down the line 
      sd.Data = @base.GetMembers()
        .Where(x=> !meshprops.Contains(x.Key))
        .ToDictionary(x=>x.Key, x=>x.Value);
    }

    private void GetSpeckleData(Base @base, GameObject go)
    {
      var sd = go.GetComponent<SpeckleData>();
      if (sd == null || sd.Data == null)
        return;
      foreach (var key in sd.Data.Keys)
      {
        @base[key] = sd.Data[key];
      }
    }
  }
}