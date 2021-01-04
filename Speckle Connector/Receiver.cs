using Objects.Converter.Unity;
using Speckle.Core.Api;
using Speckle.Core.Api.SubscriptionModels;
using Speckle.Core.Credentials;
using Speckle.Core.Logging;
using Speckle.Core.Models;
using Speckle.Core.Transports;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

namespace Speckle.ConnectorUnity
{
    public class Receiver : ScriptableObject
    {
        public string StreamId { get; private set; }
        public GameObject GameObject { get; private set; }
        private ConverterUnity _converter = new ConverterUnity();
        private Client Client { get; set; }
        private Action _onCommitCreated { get; set; }


        public Receiver()
        {
        }

        public void Init(string streamId, Action onCommitCreated = null, Account account = null)
        {
            StreamId = streamId;

            Client = new Client(account ?? AccountManager.GetDefaultAccount());

            if (onCommitCreated != null)
            {
                _onCommitCreated = onCommitCreated;
                Client.SubscribeCommitCreated(StreamId);
                Client.OnCommitCreated += Client_OnCommitCreated;
                Debug.Log($"Subscribed to commit created on stream: {streamId}");
            }
        }
        
        public async Task<GameObject> GetStreamLatestCommit()
        {
            try
            {
                var res = await Client.StreamGet(StreamId);
                var mainBranch = res.branches.items.FirstOrDefault(b => b.name == "main");
                var commit = mainBranch.commits.items[0];
                return await GetObject(commit.referencedObject, commit.id);
            }
            catch (Exception e)
            {
                Log.CaptureAndThrow(e);
            }

            return null;
        }

        #region private methods

        /// <summary>
        /// Fired when a new commit is created on this stream
        /// It receives and converts the objects and then executes the user defined _onCommitCreated action.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected virtual void Client_OnCommitCreated(object sender, CommitInfo e)
        {
            Debug.Log("commit created");

            Dispatcher.Instance().EnqueueAsync(async () =>
            {
                var newObject = await GetObject(e.objectId, e.id);
                if (GameObject != null)
                    Destroy(GameObject);
                GameObject = newObject;

                _onCommitCreated.Invoke();
            });
        }


        private async Task<GameObject> GetObject(string objectId, string commitId)
        {
            try
            {
                Debug.Log("test");
                var transport = new ServerTransport(Client.Account, StreamId);
                var @base = await Operations.Receive(
                    objectId,
                    remoteTransport: transport
                );

                return ConvertRecursivelyToNative(@base, commitId);
            }
            catch (Exception e)
            {
                Log.CaptureAndThrow(e);
            }

            return null;
        }


        /// <summary>
        /// Helper method to convert a tree-like structure (nested lists) to Native
        /// </summary>
        /// <param name="base"></param>
        /// <returns></returns>
        private GameObject ConvertRecursivelyToNative(Base @base, string name)
        {
            var go = new GameObject();
            go.name = name;

            var convertedObjects = new List<GameObject>();
            // case 1: it's an item that has a direct conversion method, eg a point
            if (_converter.CanConvertToNative(@base))
            {
                convertedObjects = TryConvertItemToNative(@base);
            }
            else
            {
                // case 2: it's a wrapper Base
                //       2a: if there's only one member unpack it
                //       2b: otherwise return dictionary of unpacked members
                var members = @base.GetDynamicMembers().ToList();


                if (members.Count() == 1)
                {
                    convertedObjects = RecurseTreeToNative(@base[members.First()]);
                }
                else
                {
                    convertedObjects = members.SelectMany(x => RecurseTreeToNative(@base[x])).ToList();
                }
            }


            convertedObjects.Where(x => x != null).ToList().ForEach(x => x.transform.parent = go.transform);

            return go;
        }


        /// <summary>
        /// Main loop to Recusrsiveli convert Speckle objects to Unity 
        /// </summary>
        /// <param name="object"></param>
        /// <returns></returns>
        private List<GameObject> RecurseTreeToNative(object @object)
        {
            var objects = new List<GameObject>();
            if (IsList(@object))
            {
                var list = ((IEnumerable) @object).Cast<object>();
                objects = list.SelectMany(x => RecurseTreeToNative(x)).ToList();
            }
            else
            {
                objects = TryConvertItemToNative(@object);
            }

            return objects;
        }

        private List<GameObject> TryConvertItemToNative(object value)
        {
            var objects = new List<GameObject>();

            if (value == null)
                return objects;

            //it's a simple type or not a Base
            if (value.GetType().IsSimpleType() || !(value is Base))
            {
                return objects;
            }

            var @base = (Base) value;

            //it's an unsupported Base, return a dictionary
            if (!_converter.CanConvertToNative(@base))
            {
                var members = @base.GetMembers().Values.ToList();


                objects = members.SelectMany(x => RecurseTreeToNative(x)).ToList();
            }
            else
            {
                try
                {
                    var converted = _converter.ConvertToNative(@base) as GameObject;
                    objects.Add(converted);
                }
                catch (Exception ex)
                {
                    Log.CaptureAndThrow(ex);
                }
            }

            return objects;
        }


        private static bool IsList(object @object)
        {
            if (@object == null)
                return false;

            var type = @object.GetType();
            return (typeof(IEnumerable).IsAssignableFrom(type) && !typeof(IDictionary).IsAssignableFrom(type) &&
                    type != typeof(string));
        }

        private static bool IsDictionary(object @object)
        {
            if (@object == null)
                return false;

            Type type = @object.GetType();
            return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Dictionary<,>);
        }

        #endregion
    }
}