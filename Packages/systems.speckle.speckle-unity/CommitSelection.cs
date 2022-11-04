using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Speckle.Core.Api;
using Speckle.Core.Credentials;
using UnityEngine;

#nullable enable
namespace Speckle.ConnectorUnity
{
    [Serializable]
    public sealed class CommitSelection : OptionSelection<Commit>
    {
        [field: SerializeReference]
        public BranchSelection BranchSelection { get; private set; }

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
            Branch? branch = BranchSelection.Selected;
            if (branch == null) return;
            List<Commit> commits = branch.commits.items;
            GenerateOptions(commits, (_, i) => i == 0);
        }
    }
    
    [Serializable]
    public sealed class BranchSelection : OptionSelection<Branch>
    {
        [field: SerializeField, Range(1,100)]
        public int BranchLimit { get; set; } = 30;
        [field: SerializeField, Range(1,100)]
        public int CommitLimit { get; set; } = 15;

        [field: SerializeReference]
        public StreamSelection StreamSelection { get; private set; }

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
            List<Branch> branches = StreamSelection.Client!.StreamGetBranches(stream.id, BranchLimit, CommitLimit).GetAwaiter().GetResult();
            GenerateOptions(branches, (b, _) => b.name == "main");
        }
    }
    
    [Serializable]
    public sealed class StreamSelection : OptionSelection<Stream>
    {
        private const int DEFAULT_REQUEST_LIMIT = 50;
        [field: SerializeField, Range(1,100)]
        public int RequestLimit { get; set; } = DEFAULT_REQUEST_LIMIT;
        
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

        internal Client? Client => AccountSelection.Client;
        
        protected override string? KeyFunction(Stream? value) => value?.id;
        public override void RefreshOptions()
        {
            if (Client == null) return;
            List<Stream> streams = Client.StreamsGet(RequestLimit).GetAwaiter().GetResult();
            GenerateOptions(streams, (_, i) => i == 0);
        }
    }
    
    
    [Serializable]
    public sealed class AccountSelection : OptionSelection<Account>, IDisposable
    {
        private Client? client;
        public Client? Client
        {
            get
            {
                Account? account = Selected;
                if (account == null) return client = null;
                if (client == null || !client.Account.Equals(account)) return client = new Client(Selected);
                return client;
            }
        }
        
        protected override string? KeyFunction(Account? value) => value?.id;

        public override void RefreshOptions()
        {
            GenerateOptions(AccountManager.GetAccounts().ToArray(),
                isDefault: (a, _) => a.isDefault);
        }

        public void Dispose()
        {
            client?.Dispose();
        }
    }
    
    
    [Serializable]
    public abstract class OptionSelection<TOption> where TOption : class
    {
        [SerializeField]
        private int selectedIndex = -1;

        public int SelectedIndex
        {
            get => selectedIndex;
            set
            {
                selectedIndex = value;
                OnSelectionChange?.Invoke();
            }
        }

        public TOption? Selected
        {
            get
            {
                if (Options == null) return null; 
                if (SelectedIndex < 0 || SelectedIndex >= Options.Length) return null;
                return Options[SelectedIndex];
            }
        }

        public TOption[] Options { get; protected set; } = Array.Empty<TOption>();
        public Action? OnSelectionChange { get; set; }

        [return: NotNullIfNotNull("value")]
        protected abstract string? KeyFunction(TOption? value);
        
        public abstract void RefreshOptions();
        
        protected void GenerateOptions(IList<TOption> source, Func<TOption, int, bool> isDefault)
        {
            List<TOption> optionsToAdd = new List<TOption>(source.Count);
            int defaultOption = -1;
            int index = 0;
            foreach (TOption? a in source)
            {
                if (a == null) continue;
                optionsToAdd.Add(a);
                if (isDefault(a, index)) defaultOption = index;
                index++;
            }

            TOption? currentSelected = Selected;
            bool selectionOutOfRange = SelectedIndex < 0 || SelectedIndex >= optionsToAdd.Count;
            if (selectionOutOfRange
                || (currentSelected != null
                && KeyFunction(currentSelected) != KeyFunction(optionsToAdd[SelectedIndex])))
            {
                selectedIndex = defaultOption;
            }
            
            Options = optionsToAdd.ToArray();
            Debug.Log($"{this.GetType()} updated");
            OnSelectionChange?.Invoke();
        }
        
    }
    
    
    

    // [Serializable]
    // public abstract class OptionSelection<TOption> : ISerializationCallbackReceiver where TOption : class
    // {
    //     [SerializeReference]
    //     private int selectedIndex = -1;
    //     [SerializeReference]
    //     protected string? selectedId;
    //     public TOption? Selected
    //     {
    //         get
    //         {
    //             if (selectedIndex == -1) return null;
    //             if(Options.Any(a => a.Equals())
    //             if (!Options.ContainsKey(selectedId)) return null;
    //             return Options[selectedId];
    //         }
    //         set
    //         {
    //             selectedId = KeyFunction(value);
    //             OnSelectionChange?.Invoke();
    //         }
    //     }
    //
    //     //public IDictionary<string, TOption> Options { get; protected set; } = new Dictionary<string, TOption>();
    //     public TOption[] Options { get; protected set; } = Array.Empty<TOption>();
    //     internal Action? OnSelectionChange { get; set; }
    //
    //     [return: NotNullIfNotNull("value")]
    //     protected abstract string? KeyFunction(TOption? value);
    //     
    //     public abstract void RefreshOptions();
    //     
    //     protected void GenerateOptions(IEnumerable<TOption?> source, Func<TOption, int, bool> isDefault)
    //     {
    //         Options.Clear();
    //         TOption? defaultOption = null;
    //         int index = 0;
    //         foreach (TOption? a in source)
    //         {
    //             if (a == null) continue;
    //             Options.TryAdd(KeyFunction(a), a);
    //             if (isDefault(a, index)) defaultOption = a;
    //             index++;
    //         }
    //
    //         if (selectedId == null || !Options.ContainsKey(selectedId))
    //         {
    //             Selected = defaultOption;
    //         }
    //     }
    //
    //     public virtual void OnAfterDeserialize()
    //     {
    //         RefreshOptions();
    //     }
    //     
    //     public virtual void OnBeforeSerialize()  { /*pass*/ }
    // }
}
