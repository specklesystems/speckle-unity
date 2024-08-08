using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Speckle.Core.Api;
using Speckle.Core.Credentials;
using Speckle.Core.Kits;
using Speckle.Core.Logging;
using Speckle.Core.Models;
using Speckle.Core.Transports;
using UnityEditor;
using UnityEngine;

namespace Speckle.ConnectorUnity.Components.Editor
{
    [Obsolete]
    [CustomEditor(typeof(StreamManager))]
    [CanEditMultipleObjects]
    public class StreamManagerEditor : UnityEditor.Editor
    {
        private bool _foldOutAccount;
        private int _totalChildrenCount;
        private StreamManager _streamManager;

        private static bool _generateAssets;

        public int StreamsLimit { get; set; } = 30;
        public int BranchesLimit { get; set; } = 75;
        public int CommitsLimit { get; set; } = 25;

        private int SelectedAccountIndex
        {
            get => _streamManager.SelectedAccountIndex;
            set => _streamManager.SelectedAccountIndex = value;
        }

        private int SelectedStreamIndex
        {
            get => _streamManager.SelectedStreamIndex;
            set => _streamManager.SelectedStreamIndex = value;
        }

        private int SelectedBranchIndex
        {
            get => _streamManager.SelectedBranchIndex;
            set => _streamManager.SelectedBranchIndex = value;
        }

        private int SelectedCommitIndex
        {
            get => _streamManager.SelectedCommitIndex;
            set => _streamManager.SelectedCommitIndex = value;
        }

        private int OldSelectedAccountIndex
        {
            get => _streamManager.OldSelectedAccountIndex;
            set => _streamManager.OldSelectedAccountIndex = value;
        }

        private int OldSelectedStreamIndex
        {
            get => _streamManager.OldSelectedStreamIndex;
            set => _streamManager.OldSelectedStreamIndex = value;
        }

        private Client Client
        {
            get => _streamManager.Client;
            set => _streamManager.Client = value;
        }

        private Account SelectedAccount
        {
            get => _streamManager.SelectedAccount;
            set => _streamManager.SelectedAccount = value;
        }

        private Stream SelectedStream
        {
            get => _streamManager.SelectedStream;
            set => _streamManager.SelectedStream = value;
        }

        public List<Account> Accounts
        {
            get => _streamManager.Accounts;
            set => _streamManager.Accounts = value;
        }

        private List<Stream> Streams
        {
            get => _streamManager.Streams;
            set => _streamManager.Streams = value;
        }

        private List<Branch> Branches
        {
            get => _streamManager.Branches;
            set => _streamManager.Branches = value;
        }

        private async Task LoadAccounts()
        {
            //refresh accounts just in case
            Accounts = AccountManager.GetAccounts().ToList();
            if (!Accounts.Any())
            {
                Debug.Log("No Accounts found, please login in Manager");
            }
            else
            {
                await SelectAccount(0);
            }
        }

        private async Task SelectAccount(int i)
        {
            SelectedAccountIndex = i;
            OldSelectedAccountIndex = i;
            SelectedAccount = Accounts[i];

            Client = new Client(SelectedAccount);
            await LoadStreams();
        }

        private async Task LoadStreams()
        {
            EditorUtility.DisplayProgressBar("Loading streams...", "", 0);
            Streams = await Client.StreamsGet(StreamsLimit);
            EditorUtility.ClearProgressBar();
            if (Streams.Any())
                await SelectStream(0);
        }

        private async Task SelectStream(int i)
        {
            SelectedStreamIndex = i;
            OldSelectedStreamIndex = i;
            SelectedStream = Streams[i];

            EditorUtility.DisplayProgressBar("Loading stream details...", "", 0);
            Branches = await Client.StreamGetBranches(
                SelectedStream.id,
                BranchesLimit,
                CommitsLimit
            );
            if (Branches.Any())
            {
                SelectedBranchIndex = 0;
                if (Branches[SelectedBranchIndex].commits.items.Any())
                {
                    SelectedCommitIndex = 0;
                }
            }

            EditorUtility.ClearProgressBar();
        }

        private async Task Receive()
        {
            var transport = new ServerTransport(SelectedAccount, SelectedStream.id);
            EditorUtility.DisplayProgressBar($"Receiving data from {transport.BaseUri}...", "", 0);

            try
            {
                Commit selectedCommit = Branches[SelectedBranchIndex].commits.items[
                    SelectedCommitIndex
                ];
                // Receive Speckle Objects
                var @base = await Operations.Receive(
                    selectedCommit.referencedObject,
                    remoteTransport: transport,
                    onProgressAction: dict =>
                    {
                        EditorApplication.delayCall += () =>
                        {
                            EditorUtility.DisplayProgressBar(
                                $"Receiving data from {transport.BaseUri}...",
                                "",
                                Convert.ToSingle(dict.Values.Average() / _totalChildrenCount)
                            );
                        };
                    },
                    onTotalChildrenCountKnown: count =>
                    {
                        _totalChildrenCount = count;
                    }
                );
                if (@base is null)
                    throw new InvalidOperationException("Received object was null");

                EditorUtility.ClearProgressBar();

                Analytics.TrackEvent(
                    SelectedAccount,
                    Analytics.Events.Receive,
                    new Dictionary<string, object>()
                    {
                        { "mode", nameof(StreamManagerEditor) },
                        {
                            "sourceHostApp",
                            HostApplications
                                .GetHostAppFromString(selectedCommit.sourceApplication)
                                .Slug
                        },
                        { "sourceHostAppVersion", selectedCommit.sourceApplication ?? "" },
                        { "hostPlatform", Application.platform.ToString() },
                        { "isMultiplayer", selectedCommit.authorId != SelectedAccount.userInfo.id },
                    }
                );

                //Convert Speckle Objects
                int childrenConverted = 0;

                void BeforeConvertCallback(Base b)
                {
                    EditorUtility.DisplayProgressBar(
                        "Converting To Native...",
                        $"{b.speckle_type} - {b.id}",
                        Convert.ToSingle(childrenConverted++ / _totalChildrenCount)
                    );
                }

                _streamManager.ConvertRecursivelyToNative(
                    @base,
                    Branches[SelectedBranchIndex].commits.items[SelectedCommitIndex].id,
                    BeforeConvertCallback
                );

                // Read Receipt
                await Client.CommitReceived(
                    new CommitReceivedInput
                    {
                        streamId = SelectedStream.id,
                        commitId = Branches[SelectedBranchIndex]
                            .commits
                            .items[SelectedCommitIndex]
                            .id,
                        message = $"received commit from {HostApplications.Unity.Name} Editor",
                        sourceApplication = HostApplications.Unity.Name
                    }
                );
            }
            finally
            {
                EditorApplication.delayCall += EditorUtility.ClearProgressBar;
            }
        }

        public override async void OnInspectorGUI()
        {
            _streamManager = (StreamManager)target;

            #region Account GUI

            if (Accounts == null)
            {
                await LoadAccounts();
                return;
            }

            EditorGUILayout.BeginHorizontal();

            SelectedAccountIndex = EditorGUILayout.Popup(
                "Accounts",
                SelectedAccountIndex,
                Accounts.Select(x => x.userInfo.email + " | " + x.serverInfo.name).ToArray(),
                GUILayout.ExpandWidth(true),
                GUILayout.Height(20)
            );

            if (OldSelectedAccountIndex != SelectedAccountIndex)
            {
                await SelectAccount(SelectedAccountIndex);
                return;
            }

            if (GUILayout.Button("Refresh", GUILayout.Width(60), GUILayout.Height(20)))
            {
                await LoadAccounts();
                return;
            }

            EditorGUILayout.EndHorizontal();

            #region Speckle Account Info

            _foldOutAccount = EditorGUILayout.BeginFoldoutHeaderGroup(
                _foldOutAccount,
                "Account Info"
            );

            if (_foldOutAccount)
            {
                EditorGUI.BeginDisabledGroup(true);

                EditorGUILayout.TextField(
                    "Name",
                    SelectedAccount.userInfo.name,
                    GUILayout.Height(20),
                    GUILayout.ExpandWidth(true)
                );

                EditorGUILayout.TextField(
                    "Server",
                    SelectedAccount.serverInfo.name,
                    GUILayout.Height(20),
                    GUILayout.ExpandWidth(true)
                );

                EditorGUILayout.TextField(
                    "URL",
                    SelectedAccount.serverInfo.url,
                    GUILayout.Height(20),
                    GUILayout.ExpandWidth(true)
                );

                EditorGUI.EndDisabledGroup();
            }

            EditorGUILayout.EndFoldoutHeaderGroup();

            #endregion

            #endregion

            #region Stream List

            if (Streams == null)
                return;

            EditorGUILayout.BeginHorizontal();

            SelectedStreamIndex = EditorGUILayout.Popup(
                "Streams",
                SelectedStreamIndex,
                Streams.Select(x => x.name).ToArray(),
                GUILayout.Height(20),
                GUILayout.ExpandWidth(true)
            );

            if (OldSelectedStreamIndex != SelectedStreamIndex)
            {
                await SelectStream(SelectedStreamIndex);
                return;
            }

            if (GUILayout.Button("Refresh", GUILayout.Width(60), GUILayout.Height(20)))
            {
                await LoadStreams();
                return;
            }

            EditorGUILayout.EndHorizontal();

            #endregion

            #region Branch List

            if (Branches == null)
                return;

            EditorGUILayout.BeginHorizontal();

            SelectedBranchIndex = EditorGUILayout.Popup(
                "Branches",
                SelectedBranchIndex,
                Branches.Select(x => x.name).ToArray(),
                GUILayout.Height(20),
                GUILayout.ExpandWidth(true)
            );
            EditorGUILayout.EndHorizontal();

            if (!Branches[SelectedBranchIndex].commits.items.Any())
                return;

            EditorGUILayout.BeginHorizontal();

            SelectedCommitIndex = EditorGUILayout.Popup(
                "Commits",
                SelectedCommitIndex,
                Branches[SelectedBranchIndex]
                    .commits.items.Select(x => $"{x.message} - {x.id}")
                    .ToArray(),
                GUILayout.Height(20),
                GUILayout.ExpandWidth(true)
            );

            EditorGUILayout.EndHorizontal();

            #endregion

            #region Generate Materials

            EditorGUILayout.BeginHorizontal();

            GUILayout.Label("Generate assets");
            GUILayout.FlexibleSpace();
            bool selection = GUILayout.Toggle(_generateAssets, "");
            if (_generateAssets != selection)
            {
                _generateAssets = selection;
                _streamManager.RC.AssetCache.nativeCaches =
                    NativeCacheFactory.GetDefaultNativeCacheSetup(_generateAssets);
            }

            EditorGUILayout.EndHorizontal();

            #endregion


            EditorGUILayout.BeginHorizontal();

            bool receive = GUILayout.Button("Receive!");

            EditorGUILayout.EndHorizontal();

            if (receive)
            {
                await Receive();
            }
        }
    }
}
