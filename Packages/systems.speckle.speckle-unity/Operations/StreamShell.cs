using System;

namespace Speckle.ConnectorUnity
{
  [Serializable]
  public class StreamShell
  {

    public string streamName, streamId;
    public string branch;
    public string commitId;
    public int totalChildCount;

    /// <summary>
    /// boolean flag for notifying displaying if active stream has new updates
    /// </summary>
    public bool expired;
    /// <summary>
    /// If true, it will automatically receive updates sent to this stream
    /// </summary>
    public bool autoReceive;
    /// <summary>
    /// If true, it will delete previously received objects when new one are received.\n
    /// Set to true by default 
    /// </summary>
    public bool clearOnUpdate = true;

  }
}