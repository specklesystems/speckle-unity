#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Speckle.Core.Models;
using UnityEngine;

namespace Speckle.ConnectorUnity.Components
{
    public partial class RecursiveConverter
    {
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
            //Ensure we have A native cache
            if (AssetCache.nativeCaches.Any(x => x == null))
            {
                AssetCache.nativeCaches = NativeCacheFactory.GetStandaloneCacheSetup();
            }
            
            var createdGameObjects = new List<GameObject>();
            ConverterInstance.SetContextDocument(AssetCache);
            try
            {
                AssetCache.BeginWrite();
                ConvertChild(o, parent, predicate, createdGameObjects);
            }
            finally
            {
                AssetCache.FinishWrite();
            }

            //TODO track event
            
            
            return createdGameObjects;

        }

        protected string[] namePropertyAliases = {"name", "Name"};
        
        protected virtual string GenerateObjectName(Base baseObject)
        {
            // 1. Use explicit name
            foreach (var nameAlias in namePropertyAliases)
            {
                string? s = baseObject[nameAlias] as string;
                if (!string.IsNullOrWhiteSpace(s)) return s; //TODO any sanitization needed?
            }
            
            // 2. Use type + id as fallback name
            // Only take the most derived type from the speckle type
            string speckleType = baseObject.speckle_type.Split(':').Last();
            return $"{speckleType} - {baseObject.id}";
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
                    go.name = GenerateObjectName(baseObject);
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
            if(value == null) return;
            if(value.GetType().IsPrimitive) return;
            if (value is string) return;
            
            if(value is Base o)
            {
                RecurseTreeToNative(o, parent, predicate, outCreatedObjects);
            }
            else if (value is IDictionary dictionary)
            {
                foreach (object v in dictionary.Keys)
                {
                    ConvertChild(v, parent, predicate, outCreatedObjects);
                }
            }
            else if (value is IList collection)
            {
                foreach (object v in collection)
                {
                    ConvertChild(v, parent, predicate, outCreatedObjects);
                }
            }
            else if(!value.GetType().IsValueType) //don't want to output errors for structs
            {
                Debug.Log($"Unknown type {value.GetType()} found when traversing tree, will be safely ignored");
            }
        }
        
        [Obsolete("Use RecursivelyConvertToNative instead")]
        public GameObject ConvertRecursivelyToNative(Base @base, string name)
        {
            var parentObject = new GameObject(name);
            RecursivelyConvertToNative(@base, parentObject.transform);
            return parentObject;
        }
        
    }
}