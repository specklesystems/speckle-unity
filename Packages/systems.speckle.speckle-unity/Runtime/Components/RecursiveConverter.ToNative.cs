#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Speckle.ConnectorUnity.NativeCache;
using Speckle.Core.Models;
using Speckle.Core.Models.GraphTraversal;
using UnityEngine;

namespace Speckle.ConnectorUnity.Components
{
    public partial class RecursiveConverter
    {

        public IEnumerator ConvertToNative(Base rootObject, Transform? parent, IDictionary<Base, GameObject> outCreatedObjects)
        {
            
            InitializeAssetCache();
            var traversalFunc = DefaultTraversal.CreateBIMTraverseFunc(ConverterInstance);

            var convertableObjects = traversalFunc.Traverse(rootObject)
                .Where(tc => ConverterInstance.CanConvertToNative(tc.current));

            foreach (TraversalContext tc in convertableObjects)
            {
                Transform? nativeParent = parent;
                if (tc.parent != null && outCreatedObjects.TryGetValue(tc.parent.current, out GameObject p))
                    nativeParent = p.transform;
                
                GameObject? go = ConverterInstance.ConvertToNative(tc.current) as GameObject;
                if (go == null) continue;
                
                go.transform.SetParent(parent, true);
                outCreatedObjects.Add(tc.current, go);
            
                //Set some common for all created GameObjects
                //TODO add support for more unity specific props
                if(go.name == "New Game Object" || string.IsNullOrWhiteSpace(go.name))
                    go.name = AssetHelpers.GenerateObjectName(tc.current);
                //if (baseObject["tag"] is string t) go.tag = t;
                if (tc.current["physicsLayer"] is string layerName)
                {
                    int layer = LayerMask.NameToLayer(layerName); //TODO: check how this can be interoperable with Unreal and Blender
                    if (layer > -1) go.layer = layer;
                }
                //if (baseObject["isStatic"] is bool isStatic) go.isStatic = isStatic;

                yield return go;
            }
        }
        
        
        
        
        
        
        public IEnumerator ConvertCoroutine(Base rootObject, Transform? parent, List<GameObject> outCreatedObjects)
            => ConvertCoroutine(rootObject, parent, outCreatedObjects,b => ConverterInstance.CanConvertToNative(b));
        
        public IEnumerator ConvertCoroutine(Base rootObject, Transform? parent, List<GameObject> outCreatedObjects, Func<Base, bool> predicate)
        {
            foreach (string propertyName in GetPotentialChildren(rootObject))
            {
                ConvertChild(rootObject[propertyName], parent, predicate, outCreatedObjects);
                yield return null;
            }
        }
        
        /// <summary>
        /// Given <paramref name="o"/>,
        /// will recursively convert any objects in the tree
        /// </summary>
        /// <param name="o">The object to convert (<see cref="Base"/> or <see cref="List{T}"/> of)</param>
        /// <param name="parent">Optional parent transform for the created root <see cref="GameObject"/>s</param>
        /// <returns> A list of all created <see cref="GameObject"/>s</returns>
        public virtual List<GameObject> RecursivelyConvertToNative(object? o, Transform? parent)
            => RecursivelyConvertToNative(o, parent, b => ConverterInstance.CanConvertToNative(b));
        
        /// <inheritdoc cref="RecursivelyConvertToNative(object, Transform)"/>
        /// <param name="predicate">A function to determine if an object should be converted</param>
        public virtual List<GameObject> RecursivelyConvertToNative(object? o, Transform? parent, Func<Base, bool> predicate)
        {
            InitializeAssetCache();
            
            var createdGameObjects = new List<GameObject>();
            try
            {
                AssetCache.BeginWrite();
                ConvertChild(o, parent, predicate, createdGameObjects);
            }
            finally
            {
                AssetCache.FinishWrite();
            }

            //TODO track event?
            
            
            return createdGameObjects;

        }

        private void InitializeAssetCache()
        {
            //Ensure we have A native cache
            if (AssetCache.nativeCaches.Any(x => x == null))
            {
                AssetCache.nativeCaches = NativeCacheFactory.GetStandaloneCacheSetup();
            }
            ConverterInstance.SetContextDocument(AssetCache);
        }

        public virtual void RecurseTreeToNative(Base baseObject, Transform? parent, Func<Base, bool> predicate, IList<GameObject> outCreatedObjects)
        {
            object? converted = null;
            if(predicate(baseObject))
                converted = ConverterInstance.ConvertToNative(baseObject);

            // Handle new GameObjects
            Transform? nextParent = parent;
            if (converted is GameObject go)
            {
                outCreatedObjects.Add(go);
                nextParent = go.transform;
                
                go.transform.SetParent(parent, true);
                
                //Set some common for all created GameObjects
                //TODO add support for more unity specific props
                if(go.name == "New Game Object" || string.IsNullOrWhiteSpace(go.name))
                    go.name = AssetHelpers.GenerateObjectName(baseObject);
                //if (baseObject["tag"] is string t) go.tag = t;
                if (baseObject["physicsLayer"] is string layerName)
                {
                    int layer = LayerMask.NameToLayer(layerName); //TODO: check how this can be interoperable with Unreal and Blender
                    if (layer > -1) go.layer = layer;
                }
                //if (baseObject["isStatic"] is bool isStatic) go.isStatic = isStatic;
            }
            
            // For geometry, only traverse `elements` prop, otherwise, try and convert everything
            IEnumerable<string> potentialChildren = GetPotentialChildren(baseObject);

            // Convert Children
            foreach (string propertyName in potentialChildren)
            {
                ConvertChild(baseObject[propertyName], nextParent, predicate, outCreatedObjects);
            }
            
        }

        private IEnumerable<string> GetPotentialChildren(Base baseObject)
        {
            return ConverterInstance.CanConvertToNative(baseObject)
                ? new []{"elements"}
                : baseObject.GetMembers().Keys;
        }


        protected virtual void ConvertChild(object? value, Transform? parent, Func<Base, bool> predicate, IList<GameObject> outCreatedObjects)
        {
            foreach (Base b in GraphTraversal.TraverseMember(value))
            {
                RecurseTreeToNative(b, parent, predicate, outCreatedObjects);
            }
        }
        
        [Obsolete("Use " + nameof(RecursivelyConvertToNative), true)]
        public GameObject ConvertRecursivelyToNative(Base @base, string name)
        {
            var parentObject = new GameObject(name);
            RecursivelyConvertToNative(@base, parentObject.transform);
            return parentObject;
        }
        
    }
}