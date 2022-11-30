using System;
using Speckle.Core.Api;
using Speckle.Core.Credentials;
using System.Collections.Generic;
using Speckle.Core.Models;
using UnityEngine;

namespace Speckle.ConnectorUnity.Components
{
    [Obsolete("Replaced by new " + nameof(SpeckleReceiver))]
    [ExecuteAlways]
    [AddComponentMenu("Speckle/Obsolete/Stream Manager")]
    [RequireComponent(typeof(RecursiveConverter))]
    public class StreamManager : MonoBehaviour
    {
        public int SelectedAccountIndex = -1;
        public int SelectedStreamIndex = -1;
        public int SelectedBranchIndex = -1;
        public int SelectedCommitIndex = -1;
        public int OldSelectedAccountIndex = -1;
        public int OldSelectedStreamIndex = -1;

        public Client Client;
        public Account SelectedAccount;
        public Stream SelectedStream;

        public List<Account> Accounts;
        public List<Stream> Streams;
        public List<Branch> Branches;

        public RecursiveConverter RC { get; private set; }
        
#nullable enable
        private void Awake()
        {
            RC = GetComponent<RecursiveConverter>();
        }
        
        
        public GameObject ConvertRecursivelyToNative(Base @base, string rootObjectName,
            Action<Base>? beforeConvertCallback)
        {
            var rootObject = new GameObject(rootObjectName);

            bool Predicate(Base o)
            {
                beforeConvertCallback?.Invoke(o);
                return RC.ConverterInstance.CanConvertToNative(o) //Accept geometry
                       || o.speckle_type == nameof(Base) && o.totalChildrenCount > 0; // Or Base objects that have children  
            }


            // For the rootObject only, we will create property GameObjects
            // i.e. revit categories
            foreach (var prop in @base.GetMembers())
            {
                var converted = RC.RecursivelyConvertToNative(prop.Value, null, Predicate);

                //Skip empties
                if (converted.Count <= 0) continue;

                var propertyObject = new GameObject(prop.Key);
                propertyObject.transform.SetParent(rootObject.transform);
                foreach (var o in converted)
                {
                    if (o.transform.parent == null) o.transform.SetParent(propertyObject.transform);
                }
            }

            return rootObject;
        }

#if UNITY_EDITOR
        [ContextMenu("Open Speckle Stream in Browser")]
        protected void OpenUrlInBrowser()
        {
            string url =
                $"{SelectedAccount.serverInfo.url}/streams/{SelectedStream.id}/commits/{Branches[SelectedBranchIndex].commits.items[SelectedCommitIndex].id}";
            Application.OpenURL(url);
        }
#endif
    }
}