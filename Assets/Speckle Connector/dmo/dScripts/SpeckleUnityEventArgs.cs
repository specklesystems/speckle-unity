using System;
using System.Collections.Generic;

namespace Speckle.ConnectorUnity {
    public abstract class SpeckleUnityEventArgs : EventArgs { }
    
    public class StreamSelectedArgs : EventArgs {

        public StreamSelectedArgs( List<string> branchNames, List<string> commitIds )
            {
                BranchNames = branchNames;
                CommitIds = commitIds;
            }

        public List<string> BranchNames { get; }
        public List<string> CommitIds { get; }

    }

}