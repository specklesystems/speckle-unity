using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Speckle.Core.Api;
using Speckle.Core.Credentials;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using UnityEngine;
using UnityEngine.Events;

namespace Speckle.ConnectorUnity
{

  [Serializable]
  public class SpeckleStreamInstance
  {
    public int accountIndex;
    public int streamIndex;
    public int branchIndex;
    public int commitIndex;
    public int kitIndex;

    public List<Account> accounts;
    public List<Stream> streams;
    public List<Branch> branches;
    public List<Commit> commits;
    public List<ISpeckleKit> kits;
    // public List<SpeckleConvertersSO> kits;

    public event Action OnRefreshTriggered;
    public Action OnProcessComplete;

    public event Action<bool> OnProgressRunning;
    public Action<float, string> OnProgressUpdate;

    public Action<string, Exception> onErrorAction;
    public Action<ConcurrentDictionary<string, int>> onProgressAction;
    public Action<int> onTotalChildrenCountKnown;

    private int totalChildCount;

    private StreamShell streamInfo =>
      new StreamShell
      {
        streamId = activeStream.id,
        branch = activeBranch.name,
        streamName = activeStream.name,
        commitId = activeCommit.id
      };
    /// <summary>
    /// boolean flag for checking if speckle account and client are properly setup
    /// </summary>
    public bool IsPrimed
    {
      get => activeAccount != null;
    }

    /// <summary>
    /// boolean flag for checking if manager is primed with a proper stream, branch and commit
    /// </summary>
    public bool IsReady
    {
      get => IsPrimed && activeStream != null && activeBranch != null && activeCommit != null;
    }
    
    public Account activeAccount
    {
      get => accounts.Valid(accountIndex) ? accounts[accountIndex] : null;
    }

    public Stream activeStream
    {
      get => streams.Valid(streamIndex) ? streams[streamIndex] : null;
    }

    public Branch activeBranch
    {
      get => branches.Valid(branchIndex) ? branches[branchIndex] : null;
    }

    public Commit activeCommit
    {
      get => commits.Valid(commitIndex) ? commits[commitIndex] : null;
    }

    // public SpeckleConvertersSO activeKit
    // {
    //   get => kits.Valid(kitIndex) ? kits[kitIndex] : null;
    // }

    public SpeckleStreamInstance()
    {
      onProgressAction = UpdateProgress;
      onErrorAction = (s, exception) => Debug.LogException(exception);
      onTotalChildrenCountKnown = value =>
      {
        // add one for ui
        value++;
        Debug.Log($"Total Child Count ={value}");
        totalChildCount = value;
      };
    }

    private void UpdateProgress(ConcurrentDictionary<string, int> progressReports)
    {
      var msg = "";
      var num = 0.0;

      foreach (var report in progressReports)
      {
        msg += $"{report.Key}: {report.Value}";
        num += report.Value;
      }

      totalChildCount = totalChildCount < 1 ? 1 : totalChildCount;

      var overall = (float)num / totalChildCount;

      Debug.Log($"Updating progress: {msg} - {num} - {overall:0.00%}");

      OnProgressUpdate?.Invoke(overall, msg);
    }

    public async Task RefreshManager()
    {
      streams = null;
      branches = null;
      commits = null;

      // if (UnityKits.Instance != null)
      // {
      //   kits = UnityKits.Instance.converters;
      // }

      accounts = AccountManager.GetAccounts().ToList();

      await LoadAccount();

      OnRefreshTriggered?.Invoke();
    }

    public void LoadKit(int i = 0)
    {
      kitIndex = kits.Check(i);
    }

    public void Receive()
    {
      if (!IsReady)
      {
        Debug.Log("Data is not setup correctly to receive stream");
        return;
      }
      OnProgressRunning?.Invoke(true);

      var r = new GameObject().AddComponent<Receiver>();
      r.Init(activeStream.id, false, true, activeAccount);
      r.Receive();
    }

    public async Task LoadAccount(int i = 0)
    {
      Client client = null;
      try
      {
        accountIndex = accounts.Check(i);

        if (activeAccount != null)
        {
          client = new Client(activeAccount);
          streams = await client.StreamsGet();
        }
      }
      catch (Exception e)
      {
        Debug.LogException(e);
        streams = new List<Stream>();
      }
      finally
      {
        await LoadStream(streamIndex, client);
      }
    }

    public async Task LoadStream(int i = 0, Client client = null)
    {
      if (client == null && activeAccount != null)
      {
        client = new Client(activeAccount);
      }

      streamIndex = streams.Check(i);

      if (activeStream != null)
      {
        branches = await client.StreamGetBranches(activeStream.id, 20, 20);
      }
      else
      {
        Debug.LogWarning("Error with Loading Stream");
        branches = new List<Branch>();
      }


      if (branches == null)
      {
        LoadBranch();
        return;
      }

      for (int bIndex = 0; bIndex < branches.Count; bIndex++)
      {
        if (branches[bIndex].name.Equals("main"))
        {
          LoadBranch(bIndex);
          break;
        }
      }
    }

    public void LoadBranch(int i = 0)
    {
      branchIndex = branches.Check(i);

      commits = activeBranch != null ? activeBranch.commits.items : new List<Commit>();
      LoadCommit();
    }

    public void LoadCommit(int i = 0)
    {
      commitIndex = commits.Check(i);

      if (activeCommit != null)
      {
        Debug.Log("Manager Primed with activeCommit!\n"
                  + $"id:{activeCommit.id}\n"
                  + $"author:{activeCommit.authorName}\n"
                  + $"msg:{activeCommit.message}");
      }
    }

  }

  public static class ObjectUtilities
  {
    /// <summary>
    /// Check if item is in index. Returns 0 if value is not valid in collection
    /// </summary>
    /// <param name="list"></param>
    /// <param name="index"></param>
    /// <returns></returns>
    public static int Check(this IList list, int index)
    {
      return list.Valid(index) ? index : 0;
    }

    /// <summary>
    /// Shorthand for checking if list not null and contains something
    /// </summary>
    /// <param name="list"></param>
    /// <returns></returns>
    public static bool Valid(this IList list) => list.Valid(0);

    /// <summary>
    /// Shorthand for checking if list is not null and contains index amount
    /// </summary>
    /// <param name="list"></param>
    /// <param name="count"></param>
    /// <returns></returns>
    public static bool Valid(this IList list, int count) => list != null && list.Count > count;
  }

}