#nullable enable
using System;
using System.Collections;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace Speckle.ConnectorUnity.Utils
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

    
    public static IEnumerator GetImageRoutine(string url, string authToken, Action<Texture2D?> callback)
    {
      using UnityWebRequest www = UnityWebRequestTexture.GetTexture(url);
      www.SetRequestHeader("Authorization", $"Bearer {authToken}");
      UnityWebRequestAsyncOperation request = www.SendWebRequest();
      
      yield return request;
      
      if(www.result != UnityWebRequest.Result.Success )
      {
        bool isDataError = www.result == UnityWebRequest.Result.DataProcessingError;
        string error = isDataError
          ? $"{www.result}: {www.downloadHandler.error}"
          : www.error;
        
        Debug.LogWarning( $"Error fetching image from {www.url}: {error}" );
        yield break;
      }
      Texture2D? texture = DownloadHandlerTexture.GetContent(www);
      callback.Invoke(texture);
    }
    
    /// <summary>
    /// Coroutine <see cref="CustomYieldInstruction"/> that starts and waits for an async <see cref="System.Threading.Tasks.Task"/>
    /// to complete.
    /// </summary>
    /// <remarks>Useful for running async tasks from coroutines</remarks>
    public class WaitForTask : CustomYieldInstruction
    {
        public readonly Task Task;
        public override bool keepWaiting => !Task.IsCompleted;

        public WaitForTask(Func<Task> function)
        {
            Task = Task.Run(function);
        }
    }
    
    /// <inheritdoc cref="WaitForTask"/>
    public sealed class WaitForTask<TResult> : CustomYieldInstruction
    {
        public readonly Task<TResult> Task;
        public TResult Result => Task.Result; 
        public override bool keepWaiting => !Task.IsCompleted;
        public WaitForTask(Func<Task<TResult>> function)
        {
            this.Task = System.Threading.Tasks.Task.Run(function);
        }
    }
  }
}
