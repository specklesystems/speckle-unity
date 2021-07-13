using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Sentry;
using Speckle.Core.Api;
using Speckle.Core.Credentials;
using Speckle.Core.Logging;
using Speckle.Core.Transports;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Speckle.ConnectorUnity
{
  [CustomEditor(typeof(StreamManager))]
  [CanEditMultipleObjects]
  public class StreamManagerEditor : Editor
  {
    private bool _foldOutAccount;
    private int _totalChildrenCount = 0;
    private StreamManager _streamManager;

    private int SelectedAccountIndex
    {
      get { return _streamManager.SelectedAccountIndex; }
      set { _streamManager.SelectedAccountIndex = value; }
    }

    private int SelectedStreamIndex
    {
      get { return _streamManager.SelectedStreamIndex; }
      set { _streamManager.SelectedStreamIndex = value; }
    }

    private int SelectedBranchIndex
    {
      get { return _streamManager.SelectedBranchIndex; }
      set { _streamManager.SelectedBranchIndex = value; }
    }

    private int SelectedCommitIndex
    {
      get { return _streamManager.SelectedCommitIndex; }
      set { _streamManager.SelectedCommitIndex = value; }
    }

    private int OldSelectedAccountIndex
    {
      get { return _streamManager.OldSelectedAccountIndex; }
      set { _streamManager.OldSelectedAccountIndex = value; }
    }

    private int OldSelectedStreamIndex
    {
      get { return _streamManager.OldSelectedStreamIndex; }
      set { _streamManager.OldSelectedStreamIndex = value; }
    }

    private Client Client
    {
      get { return _streamManager.Client; }
      set { _streamManager.Client = value; }
    }

    private Account SelectedAccount
    {
      get { return _streamManager.SelectedAccount; }
      set { _streamManager.SelectedAccount = value; }
    }

    private Stream SelectedStream
    {
      get { return _streamManager.SelectedStream; }
      set { _streamManager.SelectedStream = value; }
    }

    public List<Account> Accounts
    {
      get { return _streamManager.Accounts; }
      set { _streamManager.Accounts = value; }
    }

    private List<Stream> Streams
    {
      get { return _streamManager.Streams; }
      set { _streamManager.Streams = value; }
    }

    private List<Branch> Branches
    {
      get { return _streamManager.Branches; }
      set { _streamManager.Branches = value; }
    }

    private async Task LoadAccounts()
    {
      //refresh accounts just in case
      Accounts = AccountManager.GetAccounts().ToList();
      if (!Accounts.Any())
      {
        Debug.Log("No Accounts found, please login in Manager");
      } else
      {
        await SelectAccount(0);
      }
    }

    private async Task SelectAccount(int i)
    {
      SelectedAccountIndex = i;
      OldSelectedAccountIndex = i;
      SelectedAccount = Accounts[i];

      Client = new Client(SelectedAccount);
      await LoadStreams();
    }

    private async Task LoadStreams()
    {
      EditorUtility.DisplayProgressBar("Loading streams...", "", 0);
      Streams = await Client.StreamsGet();
      EditorUtility.ClearProgressBar();
      if (Streams.Any())
        await SelectStream(0);
    }

    private async Task SelectStream(int i)
    {
      SelectedStreamIndex = i;
      OldSelectedStreamIndex = i;
      SelectedStream = Streams[i];

      EditorUtility.DisplayProgressBar("Loading stream details...", "", 0);
      Branches = await Client.StreamGetBranches(SelectedStream.id);
      if (Branches.Any())
      {
        SelectedBranchIndex = 0;
        if (Branches[SelectedBranchIndex].commits.items.Any())
        {
          SelectedCommitIndex = 0;
        }
      }

      EditorUtility.ClearProgressBar();
    }

    private async Task Receive()
    {
      EditorUtility.DisplayProgressBar("Receving data...", "", 0);


      try
      {
        Tracker.TrackPageview(Tracker.RECEIVE);

        var transport = new ServerTransport(SelectedAccount, SelectedStream.id);
        var @base = await Operations.Receive(
          Branches[SelectedBranchIndex].commits.items[SelectedCommitIndex].referencedObject,
          remoteTransport: transport,
          onProgressAction: dict =>
          {
            EditorUtility.DisplayProgressBar("Receving data...", "",
                                             Convert.ToSingle(dict.Values.Average() / _totalChildrenCount));
          },
          onTotalChildrenCountKnown: count => { _totalChildrenCount = count; },
          disposeTransports: true
        );

        var go = _streamManager.ConvertRecursivelyToNative(@base,
                                                           Branches[SelectedBranchIndex].commits.items[SelectedCommitIndex].id);
      }
      catch (Exception e)
      {
        throw new SpeckleException(e.Message, e, true, SentryLevel.Error);
      }


      EditorUtility.ClearProgressBar();
    }

    public override async void OnInspectorGUI()
    {
      _streamManager = (StreamManager) target;


      #region Account GUI
      if (Accounts == null)
      {
        await LoadAccounts();
        return;
      }


      EditorGUILayout.BeginHorizontal();

      SelectedAccountIndex = EditorGUILayout.Popup(
        "Accounts", SelectedAccountIndex,
        Accounts.Select(x => x.userInfo.email + " | " + x.serverInfo.name).ToArray(),
        GUILayout.ExpandWidth(true), GUILayout.Height(20));

      if (OldSelectedAccountIndex != SelectedAccountIndex)
      {
        await SelectAccount(SelectedAccountIndex);
        return;
      }

      if (GUILayout.Button("Refresh", GUILayout.Width(60), GUILayout.Height(20)))
      {
        await LoadAccounts();
        return;
      }

      EditorGUILayout.EndHorizontal();


      #region Speckle Account Info
      _foldOutAccount = EditorGUILayout.BeginFoldoutHeaderGroup(_foldOutAccount, "Account Info");

      if (_foldOutAccount)
      {
        EditorGUI.BeginDisabledGroup(true);

        EditorGUILayout.TextField(
          "Name", SelectedAccount.userInfo.name,
          GUILayout.Height(20),
          GUILayout.ExpandWidth(true));

        EditorGUILayout.TextField(
          "Server", SelectedAccount.serverInfo.name,
          GUILayout.Height(20),
          GUILayout.ExpandWidth(true));

        EditorGUILayout.TextField(
          "URL", SelectedAccount.serverInfo.url,
          GUILayout.Height(20),
          GUILayout.ExpandWidth(true));

        EditorGUI.EndDisabledGroup();
      }

      EditorGUILayout.EndFoldoutHeaderGroup();
      #endregion
      #endregion

      #region Stream List
      if (Streams == null)
        return;

      EditorGUILayout.BeginHorizontal();

      SelectedStreamIndex = EditorGUILayout.Popup(
        "Streams",
        SelectedStreamIndex, Streams.Select(x => x.name).ToArray(), GUILayout.Height(20),
        GUILayout.ExpandWidth(true));

      if (OldSelectedStreamIndex != SelectedStreamIndex)
      {
        await SelectStream(SelectedStreamIndex);
        return;
      }

      if (GUILayout.Button("Refresh", GUILayout.Width(60), GUILayout.Height(20)))
      {
        await LoadStreams();
        return;
      }

      EditorGUILayout.EndHorizontal();
      #endregion

      #region Branch List
      if (Branches == null)
        return;

      EditorGUILayout.BeginHorizontal();

      SelectedBranchIndex = EditorGUILayout.Popup(
        "Branches",
        SelectedBranchIndex, Branches.Select(x => x.name).ToArray(), GUILayout.Height(20),
        GUILayout.ExpandWidth(true));
      EditorGUILayout.EndHorizontal();


      if (!Branches[SelectedBranchIndex].commits.items.Any())
        return;


      EditorGUILayout.BeginHorizontal();

      SelectedCommitIndex = EditorGUILayout.Popup(
        "Commits",
        SelectedCommitIndex,
        Branches[SelectedBranchIndex].commits.items.Select(x => x.message).ToArray(),
        GUILayout.Height(20),
        GUILayout.ExpandWidth(true));

      EditorGUILayout.EndHorizontal();
      #endregion

      EditorGUILayout.BeginHorizontal();

      if (GUILayout.Button("Receive!"))
      {
        await Receive();
      }


      GUILayout.EndHorizontal();
    }


  }
}