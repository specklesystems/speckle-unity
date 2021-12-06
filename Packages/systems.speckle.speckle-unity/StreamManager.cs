using System;
using System.Collections;
using System.Collections.Concurrent;
using Speckle.Core.Api;
using Speckle.Core.Credentials;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Speckle.Core.Models;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;

namespace Speckle.ConnectorUnity
{
  [ExecuteAlways]
  [AddComponentMenu("Speckle/Stream Manager")]
  public class StreamManager : MonoBehaviour
  {

    public SpeckleStreamInstance streamInstance;
    [SerializeField, HideInInspector] private float overallProgress;
    private ConcurrentDictionary<string, double> progressReports;
    
    #if UNITY_EDITOR
    public static bool GenerateMaterials = false;
    #endif
    
    private async void OnEnable()
    {
      streamInstance ??= new SpeckleStreamInstance();

      streamInstance.OnProgressUpdate += (num, msg) => { overallProgress = Mathf.Clamp(num * 100f, 0f, 100f); };
      streamInstance.OnProcessComplete += () => overallProgress = 0;

      await streamInstance.RefreshManager();
    }

  }
}