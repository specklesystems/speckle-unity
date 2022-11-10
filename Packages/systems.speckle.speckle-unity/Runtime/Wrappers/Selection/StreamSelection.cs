using System;
using System.Collections.Generic;
using Speckle.Core.Api;
using UnityEngine;

#nullable enable
namespace Speckle.ConnectorUnity.Wrappers.Selection
{
    [Serializable]
    public sealed class StreamSelection : OptionSelection<Stream>
    {
        private const int DEFAULT_REQUEST_LIMIT = 50;
        [field: SerializeField, Range(1,100), Tooltip("Number of streams to request")]
        public int StreamsLimit { get; set; } = DEFAULT_REQUEST_LIMIT;
        [field: SerializeReference]
        public AccountSelection AccountSelection { get; private set; }
        
        public StreamSelection(AccountSelection accountSelection)
        {
            AccountSelection = accountSelection;
            Initialise();
        }
        public void Initialise()
        {
            AccountSelection.OnSelectionChange = RefreshOptions;
        }

        public override Client? Client => AccountSelection.Client;

        protected override string? KeyFunction(Stream? value) => value?.id;
        public override void RefreshOptions()
        {
            if (Client == null) return;
            IList<Stream> streams;
            try
            {
                streams = Client.StreamsGet(StreamsLimit).GetAwaiter().GetResult();
            }
            catch(Exception e)
            {
                Debug.LogWarning($"Unable to refresh {this}\n{e}");
                streams = Array.Empty<Stream>();
            }
            GenerateOptions(streams, (_, i) => i == 0);
        }
    }
}
