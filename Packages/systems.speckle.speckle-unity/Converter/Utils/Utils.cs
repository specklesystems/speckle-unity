using System;
using UnityEngine;

#nullable enable
namespace Speckle.ConnectorUnity.Converter.Utils
{
  public static class Utils
  {

    public static void SafeDestroy(UnityEngine.Object obj)
    {
      if (Application.isPlaying)
        UnityEngine.Object.Destroy(obj);

      else
        UnityEngine.Object.DestroyImmediate(obj);

    }

    public static bool Valid(this string name) => !string.IsNullOrEmpty(name);

    public static Mesh SafeMeshGet(this MeshFilter mf) => Application.isPlaying ? mf.mesh : mf.sharedMesh;



    public static void SafeMeshSet(this GameObject go, Mesh m, Material[] materials, bool addMeshComponentsIfNotFound = true)
    {
      MeshFilter? mf = go.GetComponent<MeshFilter>();
      MeshRenderer? mr = go.GetComponent<MeshRenderer>();

      if (addMeshComponentsIfNotFound)
      {
        if (mf == null)
          mf = go.AddComponent<MeshFilter>();
        if (mr == null)
          mr = go.AddComponent<MeshRenderer>();
      }

      if (Application.isPlaying)
      {
        mf.mesh = m;
        mr.materials = materials;
      }
      else
      {
        mf.sharedMesh = m;
        mr.sharedMaterials = materials;
      }
    }


    /// <summary>
    /// Converts a Unity color to an ARBG int
    /// </summary>
    public static int ToIntColor(this Color c)
    {
      return
          System.Drawing.Color
              .FromArgb(Convert.ToInt32(c.r * 255), Convert.ToInt32(c.g * 255), Convert.ToInt32(c.b * 255))
              .ToArgb();
    }
    
    /// <summary>
    /// Converts a ARGB formatted int to a Unity Color
    /// </summary>
    public static Color ToUnityColor(this int c)
    {
      var argb = System.Drawing.Color.FromArgb(c);
      return new Color(argb.R / 255f, argb.G / 255f, argb.B / 255f);
    }

  }
}