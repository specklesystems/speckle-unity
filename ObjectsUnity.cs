using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Objects.Geometry
{


  /// <summary>
  /// Base definition for all rendered stream objects. Any Speckle object that needs to be
  /// displayed with a game object should inherit from this class.
  /// </summary>
  public abstract class UnityGeometry
  {
    /// <summary>
    /// The gameobject that will be displayed. The <c>Transform</c> parent will be assigned
    /// by the <c>SpeckleUnityReceiver</c>.
    /// </summary>
    public GameObject gameObject;

    /// <summary>
    /// 
    /// </summary>
    public Renderer renderer;

    /// <summary>
    /// 
    /// </summary>
    public UnityGeometry()
    {
      gameObject = new GameObject();

    }
  }

  /// <summary>
  /// A stream object represented as a gameobject with a <c>MeshRenderer</c>. Also adds a 
  /// <c>MeshCollider</c> to the object. The material is assigned by the <c>SpeckleUnityReceiver</c>.
  /// </summary>
  public class UnityMesh : UnityGeometry
  {
    /// <summary>
    /// 
    /// </summary>
    public static bool RecentreMeshTransforms = false;

    /// <summary>
    /// 
    /// </summary>
    public UnityEngine.Mesh mesh;

    /// <summary>
    /// 
    /// </summary>
    public MeshRenderer meshRenderer;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="type"></param>
    /// <param name="verts"></param>
    /// <param name="tris"></param>
    public UnityMesh(string type, Vector3[] verts, int[] tris) : base()
    {
      gameObject.name = type;

      mesh = gameObject.AddComponent<MeshFilter>().mesh;
      renderer = meshRenderer = gameObject.AddComponent<MeshRenderer>();

      if (verts.Length >= 65535)
        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;

      // center transform pivot according to the bounds of the model
      if (RecentreMeshTransforms)
      {
        Bounds meshBounds = new Bounds();
        meshBounds.center = verts[0];

        for (int i = 1; i < verts.Length; i++)
        {
          meshBounds.Encapsulate(verts[i]);
        }

        gameObject.transform.position = meshBounds.center;

        // offset mesh vertices
        for (int i = 0; i < verts.Length; i++)
        {
          verts[i] -= meshBounds.center;
        }
      }

      // assign mesh properties
      mesh.vertices = verts;
      mesh.triangles = tris;

      mesh.RecalculateNormals();
      mesh.RecalculateTangents();

      //generate uvs doesn't work as intended. Leaving out for now
      //GenerateUVs (ref mesh);

      //Add mesh collider
      MeshCollider mc = gameObject.AddComponent<MeshCollider>();
      mc.sharedMesh = mesh;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="mesh"></param>
    /// <returns></returns>
    protected void GenerateUVs(ref UnityEngine.Mesh mesh)
    {
      Vector3 p = Vector3.up;
      Vector3 u = Vector3.Cross(p, Vector3.forward);
      if (Vector3.Dot(u, u) < 0.001f)
      {
        u = Vector3.right;
      }
      else
      {
        u = Vector3.Normalize(u);
      }

      Vector3 v = Vector3.Normalize(Vector3.Cross(p, u));
      Vector3[] vertexs = mesh.vertices;
      int[] tris = mesh.triangles;
      Vector2[] uvs = new Vector2[vertexs.Length];

      for (int i = 0; i < tris.Length; i += 3)
      {

        Vector3 a = vertexs[tris[i]];
        Vector3 b = vertexs[tris[i + 1]];
        Vector3 c = vertexs[tris[i + 2]];
        Vector3 side1 = b - a;
        Vector3 side2 = c - a;
        Vector3 N = Vector3.Cross(side1, side2);

        N = new Vector3(Mathf.Abs(N.normalized.x), Mathf.Abs(N.normalized.y), Mathf.Abs(N.normalized.z));



        if (N.x > N.y && N.x > N.z)
        {
          uvs[tris[i]] = new Vector2(vertexs[tris[i]].z, vertexs[tris[i]].y);
          uvs[tris[i + 1]] = new Vector2(vertexs[tris[i + 1]].z, vertexs[tris[i + 1]].y);
          uvs[tris[i + 2]] = new Vector2(vertexs[tris[i + 2]].z, vertexs[tris[i + 2]].y);
        }
        else if (N.y > N.x && N.y > N.z)
        {
          uvs[tris[i]] = new Vector2(vertexs[tris[i]].x, vertexs[tris[i]].z);
          uvs[tris[i + 1]] = new Vector2(vertexs[tris[i + 1]].x, vertexs[tris[i + 1]].z);
          uvs[tris[i + 2]] = new Vector2(vertexs[tris[i + 2]].x, vertexs[tris[i + 2]].z);
        }
        else if (N.z > N.x && N.z > N.y)
        {
          uvs[tris[i]] = new Vector2(vertexs[tris[i]].x, vertexs[tris[i]].y);
          uvs[tris[i + 1]] = new Vector2(vertexs[tris[i + 1]].x, vertexs[tris[i + 1]].y);
          uvs[tris[i + 2]] = new Vector2(vertexs[tris[i + 2]].x, vertexs[tris[i + 2]].y);
        }

      }

      mesh.uv = uvs;
    }
  }

  /// <summary>
  /// Used to display lines, curves, or polylines as a game object with a <c>LineRenderer</c>.
  /// The material is assigned by the <c>SpeckleUnityReceiver</c>.
  /// </summary>
  public class UnityPolyline : UnityGeometry
  {
    /// <summary>
    /// 
    /// </summary>
    public static float LineWidth = 1;

    /// <summary>
    /// 
    /// </summary>
    public LineRenderer lineRenderer;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="type"></param>
    /// <param name="points"></param>
    public UnityPolyline(string type, Vector3[] points) : base()
    {
      gameObject.name = type;

      //create line renderer       
      renderer = lineRenderer = gameObject.AddComponent<LineRenderer>();

      lineRenderer.positionCount = points.Length;
      lineRenderer.SetPositions(points);
      lineRenderer.numCornerVertices = lineRenderer.numCapVertices = 8;
      lineRenderer.startWidth = lineRenderer.endWidth = LineWidth;
    }
  }


  /// <summary>
  /// Display Point. Uses a line renderer for display. The material is assigned by the
  /// <c>SpeckleUnityReceiver</c>.
  /// </summary>
  public class UnityPoint : UnityGeometry
  {
    /// <summary>
    /// 
    /// </summary>
    public static float PointDiameter = 1;

    /// <summary>
    /// 
    /// </summary>
    public Vector3 point;

    /// <summary>
    /// 
    /// </summary>
    public LineRenderer lineRenderer;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="type"></param>
    /// <param name="point"></param>
    public UnityPoint(string type, Vector3 point) : base()
    {
      gameObject.name = type;

      this.point = point;

      //create line renderer       
      renderer = lineRenderer = gameObject.AddComponent<LineRenderer>();
      lineRenderer.SetPositions(new Vector3[2] { point, point });
      lineRenderer.numCornerVertices = lineRenderer.numCapVertices = 8;
      lineRenderer.startWidth = lineRenderer.endWidth = PointDiameter;
    }
  }
}
