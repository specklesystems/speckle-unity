using System;
using System.Linq;
using Speckle.Core.Api;
using Speckle.Core.Credentials;
using Speckle.Core.Helpers;
using UnityEngine;

#nullable enable
namespace Speckle.ConnectorUnity.Wrappers.Selection
{
    [Serializable]
    public sealed class AccountSelection : OptionSelection<Account>, IDisposable
    {
        private Client? _client;
        public override Client? Client
        {
            get
            {
                Account? account = Selected;
                if (account == null)
                    return _client = null;
                if (_client == null || !_client.Account.Equals(account))
                    return _client = new Client(account);
                return _client;
            }
        }

        protected override string? KeyFunction(Account? value)
        {
            if (value is null)
                return null;

            return value.id + Crypt.Md5(value.serverInfo.url ?? "", "X2");
        }

        public override void RefreshOptions()
        {
            Account[] accounts;
            try
            {
                accounts = AccountManager.GetAccounts().ToArray();
                if (accounts.Length == 0)
                    Debug.LogWarning("No Accounts found, please login in Manager");
            }
            catch (Exception e)
            {
                accounts = Array.Empty<Account>();
                Debug.LogWarning($"Unable to refresh {this}\n{e}");
            }
            GenerateOptions(accounts, isDefault: (a, i) => a.isDefault || i == 0);
        }

        public void Dispose()
        {
            _client?.Dispose();
        }
    }
}
