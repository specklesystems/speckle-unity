using System;
using System.Collections;
using Speckle.Core.Api;
using Speckle.Core.Credentials;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Speckle.Core.Models;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;

namespace Speckle.ConnectorUnity {
    [ExecuteAlways]
    public class StreamManager : MonoBehaviour {

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

        public GameObject ConvertRecursivelyToNative( Base @base, string id )
            {
            
                var rc = GetComponent<RecursiveConverter>( );
                if ( rc == null )
                    rc = gameObject.AddComponent<RecursiveConverter>( );
                
                return rc.ConvertRecursivelyToNative( @base,
                    Branches[ SelectedBranchIndex ].commits.items[ SelectedCommitIndex ].id );
            }
    }
}