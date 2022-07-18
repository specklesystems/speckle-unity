#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Speckle.Core.Models;
using UnityEngine;

namespace Speckle.ConnectorUnity
{
    public partial class RecursiveConverter
    {
        
        /// <summary>
        /// Given <paramref name="baseObject"/>,
        /// will recursively convert any objects in the tree
        /// </summary>
        /// <param name="baseObject">The Speckle object to convert + its children</param>
        /// <param name="parent">Optional parent transform for the created root <see cref="GameObject"/>s</param>
        /// <returns> A list of all created <see cref="GameObject"/>s</returns>
        public virtual List<GameObject> RecursivelyConvertToNative(Base baseObject, Transform? parent)
            => RecursivelyConvertToNative(baseObject, parent, o => ConverterInstance.CanConvertToNative(o));
        
        /// <inheritdoc cref="RecursivelyConvertToNative(Base, Transform)"/>
        /// <param name="predicate">A function to determine if an object should be converted</param>
        public virtual List<GameObject> RecursivelyConvertToNative(Base baseObject, Transform? parent, Func<Base, bool> predicate)
        {
            LoadMaterialOverrides();

            var createdGameObjects = new List<GameObject>();
            RecurseTreeToNative(baseObject, parent, predicate, createdGameObjects);
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
                if (!string.IsNullOrWhiteSpace(s)) return s!; //TODO any sanitization needed?
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
                
                go.name = GenerateObjectName(baseObject);
                go.transform.SetParent(parent);
                //TODO add support for unity specific props
                //if (baseObject["tag"] is string t) go.tag = t;
                //if (baseObject["layer"] is int layer) go.layer = layer;
                //if (baseObject["isStatic"] is bool isStatic) go.isStatic = isStatic;
            }
            
            // For geometry, only traverse `elements` prop, otherwise, try and convert everything
            IEnumerable<string> potentialChildren;
            if (ConverterInstance.CanConvertToNative(baseObject)) potentialChildren = new []{"elements"};
            else potentialChildren = baseObject.GetMemberNames();

            // Convert Children
            foreach (string propertyName in potentialChildren)
            {
                ConvertChild(baseObject[propertyName], nextParent, predicate, outCreatedObjects);
            }
            
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
            else
            {
                Debug.Log($"Unknown type {value.GetType()} found when traversing tree, will be safely ignored");
                
            }
        }

        protected virtual void LoadMaterialOverrides()
        {
            //using the ApplicationPlaceholderObject to pass materials
            //available in Assets/Materials to the converters
            var materials = Resources.LoadAll("", typeof(Material)).Cast<Material>().ToArray();
            if (materials.Length == 0) Debug.Log("To automatically assign materials to received meshes, materials have to be in the \'Assets/Resources\' folder!");
            var placeholderObjects = materials.Select(x => new ApplicationPlaceholderObject { NativeObject = x }).ToList();
            ConverterInstance.SetContextObjects(placeholderObjects);
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