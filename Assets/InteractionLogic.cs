using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Speckle.Core.Api;
using Speckle.Core.Logging;
using UnityEngine;
using UnityEngine.UI;

namespace Speckle.ConnectorUnity
{
  public class InteractionLogic : MonoBehaviour
  {
    private Receiver receiver;

    public void InitReceiver(Stream stream, bool autoReceive)
    {
      gameObject.name = $"receiver-{stream.id}-{Guid.NewGuid().ToString()}";
      InitRemove();

      receiver = gameObject.AddComponent<Receiver>();
      receiver.Stream = stream;

      var btn = gameObject.transform.Find("Btn").GetComponentInChildren<Button>();
      var streamText = gameObject.transform.Find("StreamText").GetComponentInChildren<Text>();
      var statusText = gameObject.transform.Find("StatusText").GetComponentInChildren<Text>();
      var branchesDropdown = gameObject.transform.Find("Dropdown").GetComponentInChildren<Dropdown>();
      var receiveProgress = btn.GetComponentInChildren<Slider>();
      receiveProgress.gameObject.SetActive(false); //hide

      //populate branches
      branchesDropdown.options.Clear();
      foreach (var branch in receiver.Stream.branches.items)
      {
        branchesDropdown.options.Add(new Dropdown.OptionData(branch.name));
      }

      //trigger ui refresh, maybe there's a better method
      branchesDropdown.value = -1;
      branchesDropdown.value = 0;
      branchesDropdown.onValueChanged.AddListener(index =>
      {
        if (index == -1)
          return;

        receiver.BranchName = receiver.Stream.branches.items[index].name;
      });

      receiver.Init(stream.id, autoReceive, true,
        onDataReceivedAction: (go) =>
        {
          statusText.text = $"Received {go.name}";
          MakeButtonsInteractable(true);
          receiveProgress.value = 0;
          receiveProgress.gameObject.SetActive(false);

          AddComponents(go);
        },
        onTotalChildrenCountKnown: (count) => { receiver.TotalChildrenCount = count; },
        onProgressAction: (dict) =>
        {
          //Run on a dispatcher as GOs can only be retrieved on the main thread
          Dispatcher.Instance().Enqueue(() =>
          {
            var val = dict.Values.Average() / receiver.TotalChildrenCount;
            receiveProgress.gameObject.SetActive(true);
            receiveProgress.value = (float) val;
          });
        });


      streamText.text = $"Stream: {stream.name}\nId: {stream.id} - Auto: {autoReceive}";
      btn.onClick.AddListener(() =>
      {
        statusText.text = "Receiving...";
        MakeButtonsInteractable(false);
        receiver.Receive();
      });
    }

    /// <summary>
    /// Recursively adds custom components to all children of a GameObject
    /// </summary>
    /// <param name="go"></param>
    private void AddComponents(GameObject go)
    {
      for (var i = 0; i < go.transform.childCount; i++)
      {
        var child = go.transform.GetChild(i);

        if (child.childCount > 0)
        {
          AddComponents(child.gameObject);
        }

        child.gameObject.AddComponent<Selectable>();

        //Add extra Components
        //var rigidbody = child.gameObject.AddComponent<Rigidbody>();
        //rigidbody.mass = 10;
      }
    }

    public void InitSender(Stream stream)
    {
      gameObject.name = $"sender-{stream.id}-{Guid.NewGuid().ToString()}";
      InitRemove();

      var sender = gameObject.AddComponent<Sender>();

      var btn = gameObject.transform.Find("Btn").GetComponentInChildren<Button>();

      var streamText = gameObject.transform.Find("StreamText").GetComponentInChildren<Text>();
      var statusText = gameObject.transform.Find("StatusText").GetComponentInChildren<Text>();

      btn.GetComponentInChildren<Text>().text = "Send";
      statusText.text = "Ready to send";

      var sendProgress = btn.GetComponentInChildren<Slider>();
      sendProgress.gameObject.SetActive(false); //hide

      streamText.text = $"Stream: {stream.name}\nId: {stream.id}";


      btn.onClick.AddListener(() =>
        {
          var objs = new List<GameObject>();
          foreach (var s in SelectionManager.selectedObjects)
          {
            objs.Add(s.gameObject);
          }

          if (!objs.Any())
          {
            statusText.text = $"No objects selected";
            return;
          }
          
          MakeButtonsInteractable(false);

          statusText.text = "Sending...";
          sender.Send(stream.id, objs,
            onProgressAction: (dict) =>
            {
              //Run on a dispatcher as GOs can only be retrieved on the main thread
              Dispatcher.Instance().Enqueue(() =>
              {
                var val = dict.Values.Average() / objs.Count;
                sendProgress.gameObject.SetActive(true);
                sendProgress.value = (float) val;
              });
            },
            onDataSentAction: (commitId) =>
            {
              Dispatcher.Instance().Enqueue(() =>
              {
                MakeButtonsInteractable(true);
                statusText.text = $"Sent {commitId}";
                sendProgress.gameObject.SetActive(false); //hide
              });
            },
            onErrorAction: (id, e) =>
            {
              MakeButtonsInteractable(true);
              statusText.text = $"Error {id}";
              sendProgress.gameObject.SetActive(false); //hide
              throw new SpeckleException(e.Message, e);
            });
        }
      );
    }

    private void MakeButtonsInteractable(bool interactable)
    {
      var selectables = gameObject.transform.GetComponentsInChildren<UnityEngine.UI.Selectable>();
      foreach (var selectable in selectables)
      {
        selectable.interactable = interactable;
      }
    }

    private void InitRemove()
    {
      var close = gameObject.transform.Find("Close").GetComponentInChildren<Button>();

      close.onClick.AddListener(() =>
      {
        //remove received geometry
        if (receiver != null)
        {
          Destroy(receiver.ReceivedData);
        }

        //update ui
        GameObject.Find("_SpeckleExamples").GetComponent<SpeckleExamples>().RemoveStreamPrefab(gameObject);

        //kill it
        Destroy(gameObject);
      });
    }
  }
}