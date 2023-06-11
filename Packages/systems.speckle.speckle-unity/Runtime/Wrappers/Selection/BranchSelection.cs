using System;
using System.Collections.Generic;
using Speckle.Core.Api;
using UnityEngine;

#nullable enable
namespace Speckle.ConnectorUnity.Wrappers.Selection
{
    [Serializable]
    public sealed class BranchSelection : OptionSelection<Branch>
    {
        [field: SerializeField, Range(1,100), Tooltip("Number of branches to request")]
        public int BranchesLimit { get; set; } = 100;
        [field: SerializeField, Range(1,100), Tooltip("Number of commits to request")]
        public int CommitsLimit { get; set; } = 25;

        [field: SerializeReference]
        public StreamSelection StreamSelection { get; private set; }
        public override Client? Client => StreamSelection.Client;
        
        public BranchSelection(StreamSelection streamSelection)
        {
            StreamSelection = streamSelection;
            Initialise();
        }
        
        public void Initialise()
        {
            StreamSelection.OnSelectionChange = RefreshOptions;
        }

        protected override string? KeyFunction(Branch? value) => value?.name;
        
        public override void RefreshOptions()
        {
            Stream? stream = StreamSelection.Selected;
            if (stream == null) return;
            IList<Branch> branches;
            try
            {
                branches = Client!.StreamGetBranches(stream.id, BranchesLimit, CommitsLimit).GetAwaiter().GetResult();
            }
            catch(Exception e)
            {
                Debug.LogWarning($"Unable to refresh {this}\n{e}");
                branches = Array.Empty<Branch>();
            }
            GenerateOptions(branches, (b, _) => b.name == "main");
        }
    }
}
