using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Speckle.ConnectorUnity
{
  /// <summary>
  /// Visual Element Object that handles dropdown menus for accounts, streams, branches, and commits
  /// </summary>
  public class SpeckleStreamElement : VisualElement
  {
    public DropdownField drop_accounts;
    public DropdownField drop_streams;
    public DropdownField drop_branches;
    public DropdownField drop_commits;
    public DropdownField drop_kits;

    public Button btn_refresh;

    public Button btn_receive;
    public Button btn_clear;

    public ProgressBar bar_receive;
    public ProgressBar bar_send;

    public SpeckleStreamInstance Instance;

    public SpeckleStreamElement(SpeckleStreamInstance value)
    {
      var vt = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Packages/systems.speckle.speckle-unity/GUI/SpeckleStreamGUI.uxml");

      if (vt != null)
        vt.CloneTree(this);

      Instance = value;
      Instance ??= new SpeckleStreamInstance();

      Instance.OnRefreshTriggered += Refresh;
      Instance.OnProcessComplete += () => SetReceiveButton(false);

      drop_accounts = SetDropDown("accounts-dropdown", Instance.accounts.Format().ToList(), Instance.accountIndex, AccountChange);
      drop_streams = SetDropDown("streams-dropdown", Instance.streams.Format().ToList(), Instance.streamIndex, StreamChange);
      drop_branches = SetDropDown("branches-dropdown", Instance.branches.Format().ToList(), Instance.branchIndex, BranchChange);
      drop_commits = SetDropDown("commits-dropdown", Instance.commits.Format().ToList(), Instance.commitIndex, CommitChange);
      drop_kits = SetDropDown("kits-dropdown", Instance.kits.Format().ToList(), Instance.kitIndex, KitChange);

      bar_receive = this.Q<ProgressBar>("receive-progressbar");
      btn_receive = this.Q<Button>("receive-stream-button");

      btn_receive.clickable.clickedWithEventInfo += ReceiveButtonClick;
    }

    private void SetReceiveButton(bool isRunning)
    {
      btn_receive.text = isRunning ? "Receiving..." : "Receive";
      btn_receive.SetEnabled(!isRunning);
    }

    private void ReceiveButtonClick(EventBase obj)
    {
      SetReceiveButton(true);
      Instance.Receive();
    }

    private DropdownField SetDropDown(string fieldName, List<string> items, int activeItem, Action<ChangeEvent<string>> callback)
    {
      var dropDown = this.Q<DropdownField>(fieldName);
      dropDown.choices = items;
      dropDown.index = activeItem;
      dropDown.RegisterValueChangedCallback(callback.Invoke);
      return dropDown;
    }

    private void Refresh()
    {
      drop_accounts.choices = Instance.accounts.Format().ToList();
      drop_accounts.index = Instance.accountIndex;

      drop_streams.choices = Instance.streams.Format().ToList();
      drop_streams.index = Instance.streamIndex;

      drop_branches.choices = Instance.branches.Format().ToList();
      drop_branches.index = Instance.branchIndex;

      drop_commits.choices = Instance.commits.Format().ToList();
      drop_commits.index = Instance.commitIndex;

      drop_kits.choices = Instance.kits.Format().ToList();
      drop_kits.index = Instance.kitIndex;
    }

    private void KitChange(ChangeEvent<string> evt)
    {
      // if (Instance == null)
      //   return;
      //
      // // var inputA = evt.newValue.ParsKitName();
      // // var inputB = evt.newValue.ParseKitAuthor();
      // var inputA = evt.newValue;
      //
      // var index = -1;
      // for (var i = 0; i < Instance.kits.Count; i++)
      // {
      //   var item = Instance.kits[i];
      //   if (item != null && item.name.Equals(inputA))
      //   {
      //     Debug.Log($"Setting active {item.name} to {inputA}");
      //     index = i;
      //     break;
      //   }
      // }
      //
      // if (index < 0)
      //   return;
      //
      // Instance.LoadKit(index);
    }

    private void AccountChange(ChangeEvent<string> evt)
    {
      evt.newValue.ParseForAccount(out var itemA, out var itemB);
      if (Instance == null)
        return;


      var index = -1;
      for (var i = 0; i < Instance.accounts.Count; i++)
      {
        var item = Instance.accounts[i];
        if (item != null && item.userInfo.email.Equals(itemA) && item.serverInfo.name.Equals(itemB))
        {
          Debug.Log($"Setting active {item.GetType()} to {itemA}-{itemB}");
          index = i;
          break;
        }
      }

      if (index < 0)
        return;

      Task.Run(async () =>
      {
        await Instance.LoadAccount(index);
        Refresh();
      });
    }

    private async void StreamChange(ChangeEvent<string> evt)
    {
      evt.newValue.ParseForStream(out var itemA, out var itemB);

      var index = -1;

      for (var i = 0; i < Instance.streams.Count; i++)
      {
        var item = Instance.streams[i];
        if (item != null && item.name.Equals(itemA) && item.id.Equals(itemB))
        {
          Debug.Log($"Setting active {item.GetType()} to {itemA} | {itemB}");
          index = i;
          break;
        }
      }

      if (index < 0)
        return;


      await Instance.LoadStream(index);
      
      drop_streams.index = Instance.streamIndex;

      // drop_streams.choices = shell.streams.Format().ToList();

      drop_branches.choices = Instance.branches.Format().ToList();
      drop_branches.index = Instance.branchIndex;

      drop_commits.choices = Instance.commits.Format().ToList();
      drop_commits.index = Instance.commitIndex;

    }

    private void BranchChange(ChangeEvent<string> evt)
    {
      evt.newValue.ParseForBranch(out var itemA);
      
      for (var i = 0; i < Instance.branches.Count; i++)
      {
        var item = Instance.branches[i];
        if (item != null && item.name.Equals(itemA))
        {
          Debug.Log($"Setting active branch to {itemA}");
          Instance.LoadBranch(i);

          drop_branches.choices = Instance.branches.Format().ToList();
          drop_branches.index = Instance.branchIndex;

          drop_commits.choices = Instance.commits.Format().ToList();
          drop_commits.index = Instance.commitIndex;
          break;
        }
      }
    }

    private void CommitChange(ChangeEvent<string> evt)
    {
      evt.newValue.ParseForCommit(out var itemA, out var itemB);

      for (var i = 0; i < Instance.commits.Count; i++)
      {
        var item = Instance.commits[i];
        if (item != null && item.id.Equals(itemA) && item.message.Equals(itemB))
        {
          Debug.Log($"Setting active commit to {itemA} | {itemB}");
          Instance.LoadCommit(i);
          break;
        }
      }
    }
  }

}