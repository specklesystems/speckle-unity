using System;
using System.Collections.Generic;
using Speckle.Core.Api;
using Speckle.Core.Models;
using UnityEngine;

namespace Speckle.ConnectorUnity
{

  /// <summary>
  /// This class gets attached to GOs and is used to store Speckle's metadata when sending / receiving
  /// </summary>
  public class SpeckleProperties : MonoBehaviour
  {
    [Serializable]
    private class SpeckleData : Base
    { }

    [SerializeField] private SpeckleData DataBase;

    public string SerializedData => DataBase != null ? Operations.Serialize(DataBase) : null;

    public Dictionary<string, object> Data
    {
      get => DataBase?.GetMembers();
      set
      {
        if (value != null)
        {
          DataBase = new SpeckleData();
          foreach (var v in value)
          {
            DataBase[v.Key] = v.Value;
          }
        }
      }
    }

  }
}