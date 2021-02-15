using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Speckle.Core;
using Speckle.Core.Models;
using Speckle.Core.Api;
using Speckle.Core.Credentials;
using System;
using System.Collections.Concurrent;
using System.Data;
using System.Threading.Tasks;
using System.Linq;
using Speckle.Core.Transports;
using Objects.Converter.Unity;
using Objects.Geometry;
using UnityEditor;
using System.Text.RegularExpressions;
using System.IO;
using UnityEngine.UI;
using Stream = Speckle.Core.Api.Stream;

namespace Speckle.ConnectorUnity
{
  public class SpeckleExamples : MonoBehaviour
  {
    public Text SelectStreamText;
    public Text DetailsStreamText;
    public Dropdown StreamSelectionDropdown;
    public Button ReceiveBtn;
    public Toggle AutoReceiveToggle;
    public Button SendBtn;

    private Slider ReceiveProgress;
    private Slider SendProgress;
    private Text SendText;
    private GameObject receivedGo;
    private List<Stream> StreamList = null;
    private Stream SelectedStream = null;
    private List<Receiver> Receivers = new List<Receiver>();

    async void Start()
    {
      if (SelectStreamText == null || StreamSelectionDropdown == null)
      {
        Debug.Log("Please set all input fields on _SpeckleExamples");
        return;
      }

      var defaultAccount = AccountManager.GetDefaultAccount();
      if (defaultAccount == null)
      {
        Debug.Log("Please set a default account in SpeckleManager");
        return;
      }

      SelectStreamText.text = $"Select a stream on {defaultAccount.serverInfo.name}:";

      StreamList = await Streams.List();
      if (!StreamList.Any())
      {
        Debug.Log("There are no streams in your account, please create one online.");
        return;
      }

      StreamSelectionDropdown.options.Clear();
      foreach (var stream in StreamList)
      {
        StreamSelectionDropdown.options.Add(new Dropdown.OptionData(stream.name + " - " + stream.id));
      }

      StreamSelectionDropdown.onValueChanged.AddListener(StreamSelectionChanged);
      //trigger ui refresh, maybe there's a better method
      StreamSelectionDropdown.value = -1;
      StreamSelectionDropdown.value = 0;

      ReceiveProgress = ReceiveBtn.GetComponentInChildren<Slider>();
      ReceiveProgress.gameObject.SetActive(false); //hide
      ReceiveBtn.onClick.AddListener(CreateReceiver);

      SendText = SendBtn.GetComponentInChildren<Text>();
      SendProgress = SendBtn.GetComponentInChildren<Slider>();
      SendProgress.gameObject.SetActive(false); //hide
      SendBtn.onClick.AddListener(SendData);
    }

    private void Update()
    {
      if (!SelectionManager.selectedObjects.Any())
      {
        SendBtn.interactable = false;
        SendText.text = "Send";
      }
      else
      {
        SendBtn.interactable = true;
        var s = SelectionManager.selectedObjects.Count == 1 ? "" : "s";
        SendText.text = $"Send {SelectionManager.selectedObjects.Count} object{s}";
      }
    }

    public void StreamSelectionChanged(int index)
    {
      if (index == -1)
        return;

      SelectedStream = StreamList[index];
      DetailsStreamText.text =
        $"Description: {SelectedStream.description}\n" +
        $"Link sharing on: {SelectedStream.isPublic}\n" +
        $"Role: {SelectedStream.role}\n" +
        $"Collaborators: {SelectedStream.collaborators.Count}\n" +
        $"Id: {SelectedStream.id}";
    }

    // Shows how to create a new Receiver from code and then pull data manually
    // Created receivers are added to a List of Receivers for future use
    private void CreateReceiver()
    {
      ReceiveBtn.interactable = false;
      var streamId = SelectedStream.id;
      var autoReceive = AutoReceiveToggle.isOn;

      var receiver = ScriptableObject.CreateInstance<Receiver>();
      receiver.Init(streamId, autoReceive,
        onDataReceivedAction: ReceiverOnDataReceivedAction,
        onTotalChildrenCountKnown: (count) => { receiver.TotalChildrenCount = count; },
        onProgressAction: (dict) =>
        {
          //Run on a dispatcher as GOs can only be retrieved on the main thread
          Dispatcher.Instance().Enqueue(() =>
          {
            var val = dict.Values.Average() / receiver.TotalChildrenCount;
            ReceiveProgress.gameObject.SetActive(true);
            ReceiveProgress.value = (float) val;
          });
        });

      //receive manually once
      receiver.Receive();

      Receivers.Add(receiver);
    }

    private void SendData()
    {
      if (!SelectionManager.selectedObjects.Any())
        return;

      var objs = new List<GameObject>();
      foreach (var index in SelectionManager.selectedObjects)
      {
        objs.Add(SelectionManager.selectables[index].gameObject);
      }

      Sender.Send(SelectedStream.id, objs,
        onProgressAction: (dict) =>
        {
          //Run on a dispatcher as GOs can only be retrieved on the main thread
          Dispatcher.Instance().Enqueue(() =>
          {
            var val = dict.Values.Average() / objs.Count;
            SendProgress.gameObject.SetActive(true);
            SendProgress.value = (float) val;
          });
        },
        onDataSentAction: SenderOnDataSentAction);
    }

    private void SenderOnDataSentAction(string commitId)
    {
      Dispatcher.Instance().Enqueue(() =>
      {
        Debug.Log($"Sent {commitId}");
        SendProgress.gameObject.SetActive(false); //hide
      });
    }

    private void ReceiverOnDataReceivedAction(GameObject go)
    {
      Debug.Log($"Received {go.name}");
      ReceiveBtn.interactable = true;
      ReceiveProgress.value = 0;
      ReceiveProgress.gameObject.SetActive(false);

      if (receivedGo != null)
        Destroy(receivedGo);

      AddSelectable(go);
      receivedGo = go;
    }


    /// <summary>
    /// Adds material and selectable script to all children of a GameObject
    /// </summary>
    /// <param name="go"></param>
    private void AddSelectable(GameObject go)
    {
      for (var i = 0; i < go.transform.childCount; i++)
      {
        var child = go.transform.GetChild(i);
        child.gameObject.AddComponent<Selectable>();
      }
    }
  }
}