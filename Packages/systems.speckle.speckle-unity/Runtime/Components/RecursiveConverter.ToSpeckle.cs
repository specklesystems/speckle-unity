#nullable enable

using System;
using System.Collections.Generic;
using Speckle.ConnectorUnity.Utils;
using Speckle.Core.Models;
using UnityEngine;

namespace Speckle.ConnectorUnity.Components
{

    public partial class RecursiveConverter
    {
        /// <summary>
        /// Given a collection of <paramref name="rootObjects"/>,
        /// will recursively convert any <see cref="GameObject"/>s in the tree
        /// where a given <paramref name="predicate"/> function holds true.
        /// </summary>
        /// <example>
        /// Convert all objects in a scene that have a given tag
        /// <code>
        ///     Base b = RecursivelyConvertToSpeckle(SceneManager.GetActiveScene().GetRootGameObjects(), o => o.CompareTag("myTag"));
        /// </code>
        /// Convert a selection of objects that share a common rootObject(s)
        /// <code>
        ///     GameObject parent = ...
        ///     ISet selection = ...
        ///     Base b = RecursivelyConvertToSpeckle(parent, o => selection.Contains(o));
        /// </code>
        /// </example>
        /// <param name="rootObjects">Root objects of a tree</param>
        /// <param name="predicate">A function to determine if an object should be converted</param>
        /// <returns>A simple <see cref="Base"/> wrapping converted objects</returns>
        public virtual Base RecursivelyConvertToSpeckle(IEnumerable<GameObject> rootObjects, Func<GameObject, bool> predicate)
        {
            List<Base> convertedRootObjects = new List<Base>();
            foreach (GameObject rootObject in rootObjects)
            {
                RecurseTreeToSpeckle(rootObject, predicate, convertedRootObjects);
            }
            
            return new Base()
            {
                ["@objects"] = convertedRootObjects,
            };
        }
        
        public virtual Base RecursivelyConvertToSpeckle(GameObject rootObject, Func<GameObject, bool> predicate)
        {
            return RecursivelyConvertToSpeckle(new[] {rootObject}, predicate);
        }
        
        public virtual void RecurseTreeToSpeckle(GameObject currentObject, Func<GameObject, bool> predicate, List<Base> outConverted)
        {
            // Convert children first
            var convertedChildren = new List<Base>(currentObject.transform.childCount);
            foreach(Transform child in currentObject.transform)
            {
                RecurseTreeToSpeckle(child.gameObject, predicate, convertedChildren);
            }
            
            if (ConverterInstance.CanConvertToSpeckle(currentObject) && predicate(currentObject))
            {
                // Convert and output 
                Base converted = ConverterInstance.ConvertToSpeckle(currentObject);
                converted.SetDetachedPropertyChecked("elements", convertedChildren);
                outConverted.Add(converted);
            }
            else
            {
                // Skip this object, and output any children
                outConverted.AddRange(convertedChildren);
            }

        }
    }
}