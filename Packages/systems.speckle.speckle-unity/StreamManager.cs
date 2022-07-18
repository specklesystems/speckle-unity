using System;
using Speckle.Core.Api;
using Speckle.Core.Credentials;
using System.Collections.Generic;
using System.Linq;
using Objects.Other;
using Objects.Utils;
using Speckle.Core.Models;
using UnityEngine;

namespace Speckle.ConnectorUnity
{
  [ExecuteAlways]
  [AddComponentMenu("Speckle/Stream Manager")]
  public class StreamManager : MonoBehaviour
  {
    public int SelectedAccountIndex = -1;
    public int SelectedStreamIndex = -1;
    public int SelectedBranchIndex = -1;
    public int SelectedCommitIndex = -1;
    public int OldSelectedAccountIndex = -1;
    public int OldSelectedStreamIndex = -1;

    public Client Client;
    public Account SelectedAccount;
    public Stream SelectedStream;

    public List<Account> Accounts;
    public List<Stream> Streams;
    public List<Branch> Branches;
    
#if UNITY_EDITOR
    public static bool GenerateMaterials = false;
#endif
    public List<GameObject> ConvertRecursivelyToNative(Base @base, string name)
    {

      var rc = GetComponent<RecursiveConverter>();
      if (rc == null)
        rc = gameObject.AddComponent<RecursiveConverter>();

      var rootObject = new GameObject(name);
      
      Func<Base, bool> predicate = o =>
          rc.ConverterInstance.CanConvertToNative(o) //Accept geometry
          || o.speckle_type == "Base" && o.totalChildrenCount > 0; // Or Base objects that have children


      return rc.RecursivelyConvertToNative(@base, rootObject.transform, predicate);
    }
    
#if UNITY_EDITOR
    [ContextMenu("Open Speckle Stream in Browser")]
    protected void OpenUrlInBrowser()
    {
        string url = $"{SelectedAccount.serverInfo.url}/streams/{SelectedStream.id}/commits/{Branches[SelectedBranchIndex].commits.items[SelectedCommitIndex].id}";
        Application.OpenURL(url);
    }
#endif
  }
}