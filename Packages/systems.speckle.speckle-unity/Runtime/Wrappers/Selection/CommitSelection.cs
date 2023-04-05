using System;
using System.Collections.Generic;
using Speckle.Core.Api;
using UnityEngine;

#nullable enable
namespace Speckle.ConnectorUnity.Wrappers.Selection
{

    [Serializable]
    public sealed class CommitSelection : OptionSelection<Commit>
    {

        [field: SerializeReference]
        public BranchSelection BranchSelection { get; private set; }
        
        public override Client? Client => BranchSelection.Client;
        
        public CommitSelection(BranchSelection branchSelection)
        {
            BranchSelection = branchSelection;
            Initialise();
            
        }
        
        public void Initialise()
        {
            BranchSelection.OnSelectionChange = RefreshOptions;
        }


        protected override string? KeyFunction(Commit? value) => value?.id;
        
        public override void RefreshOptions()
        {
            Branch? branch = BranchSelection!.Selected;
            if (branch == null) return;
            List<Commit> commits = branch.commits.items;
            GenerateOptions(commits, (_, i) => i == 0);
        }
    }
}
