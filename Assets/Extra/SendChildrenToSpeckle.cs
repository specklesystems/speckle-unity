using System;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.CompilerServices;
using Speckle.ConnectorUnity;
using UnityEditor.Experimental;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Sender)), ExecuteAlways]
[Obsolete]
public class SendChildrenToSpeckle : MonoBehaviour
{
    public LayerMask layerMask;
    public string streamId;
    public string branchName = "main";
    public bool createCommit = true;

    private Sender sender;

    void Awake()
    {
        sender = GetComponent<Sender>();
    }
    
    [ContextMenu(nameof(Send))]
    public void Send()
    {
        var selected = GetComponentsInChildren<Transform>()
            .Where(t => t != this.transform)
            .Select(o => o.gameObject)
            .ToImmutableHashSet();
        
        Debug.Log("starting send...");
        sender.Send(streamId, selected, null, branchName, createCommit,
            onErrorAction: OnError,
            onProgressAction: OnProgress,
            onDataSentAction: OnSent);
    }

    private void OnSent(string objectId)
    {
        Debug.Log($"Data sent {objectId}", this);
    }
    
    private void OnError(string message, Exception e)
    {
        Debug.LogError($"Error while sending {message} \n {e}", this);
        
    }
    private void OnProgress(ConcurrentDictionary<string, int> dict)
    {
        Debug.Log($"progress was made", this);
    }




}
