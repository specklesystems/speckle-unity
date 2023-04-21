using System;
using System.Collections;
using System.Linq;
using Speckle.Core.Api;
using Speckle.Core.Credentials;
using UnityEngine;

#nullable enable
namespace Speckle.ConnectorUnity.Wrappers.Selection
{
    [Serializable]
    public sealed class AccountSelection : OptionSelection<Account>, IDisposable
    {
        private Client? client;
        public override Client? Client
        {
            get
            {
                Account? account = Selected;
                if (account == null) return client = null;
                if (client == null || !client.Account.Equals(account)) return client = new Client(account);
                return client;
            }
        }
        
        protected override string? KeyFunction(Account? value) => value?.id;
        
        public override void RefreshOptions()
        {
            Account[] accounts;
            try
            {
                accounts = AccountManager.GetAccounts().ToArray();
                if(accounts.Length == 0)
                    Debug.LogWarning("No Accounts found, please login in Manager");
            }
            catch(Exception e)
            {
                accounts = Array.Empty<Account>();
                Debug.LogWarning($"Unable to refresh {this}\n{e}");
            }
            GenerateOptions(accounts, isDefault: (a, i) => a.isDefault || i == 0);
        }
        
        public void Dispose()
        {
            client?.Dispose();
        }
    }
}
