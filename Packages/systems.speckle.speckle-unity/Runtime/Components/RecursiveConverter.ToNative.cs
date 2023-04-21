#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Speckle.ConnectorUnity.NativeCache;
using Speckle.Core.Logging;
using Speckle.Core.Models;
using Speckle.Core.Models.GraphTraversal;
using UnityEngine;
using UnityEngine.Networking;

namespace Speckle.ConnectorUnity.Components
{

    /// <summary>
    /// Struct that encapsulates the result of a ToNative conversion of a single SpeckleObject <see cref="Base"/>
    /// </summary>
    public readonly struct ConversionResult
    {
        /// <summary>
        /// The context that was converted ToNative
        /// </summary>
        public readonly TraversalContext traversalContext;
        
        /// <summary>
        /// The result of conversion a successful conversion
        /// </summary>
        public readonly GameObject? converted;
        
        /// <summary>
        /// The result of conversion a failed conversion
        /// </summary>
        public readonly Exception? exception;

        /// <summary>
        /// Constructor used for Successful conversions
        /// </summary>
        /// <param name="traversalContext">The current traversal context</param>
        /// <param name="converted">The resultant ToNative conversion of <see cref="TraversalContext.current"/> context object</param>
        /// <exception cref="ArgumentNullException"/>
        public ConversionResult(TraversalContext traversalContext, [NotNull] GameObject? converted)
            : this(traversalContext, converted, null)
        {
            if (converted == null) throw new ArgumentNullException(nameof(converted));
        }

        /// <summary>
        /// Constructor used for Failed conversions
        /// </summary>
        /// <param name="traversalContext">The current conversion</param>
        /// <param name="exception">The operation halting exception that occured</param>
        /// <param name="converted">Optional converted GameObject</param>
        /// <exception cref="ArgumentNullException"/>
        public ConversionResult(TraversalContext traversalContext, [NotNull] Exception? exception,
            GameObject? converted = null)
            : this(traversalContext, converted, exception)
        {
            if (exception == null) throw new ArgumentNullException(nameof(exception));
        }

        private ConversionResult(TraversalContext traversalContext, GameObject? converted, Exception? exception)
        {
            this.traversalContext = traversalContext;
            this.converted = converted;
            this.exception = exception;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="converted">The converted <see cref="GameObject"/></param>
        /// <param name="exception">The <see cref="exception"/> that occured during conversion</param>
        /// <returns>True if the conversion was successful</returns>
        public bool WasSuccessful(
            [NotNullWhen(true)] out GameObject? converted, 
            [NotNullWhen(false)] out Exception? exception)
        {
            converted = this.converted;
            exception = this.exception;
            return this.exception == null;
        }
        
        public Base SpeckleObject => traversalContext.current;
    }
    
    public partial class RecursiveConverter
    {
        /// <summary>
        /// Coroutine for 
        /// </summary>
        /// <param name="rootObject"></param>
        /// <param name="parent"></param>
        /// <param name="predicate">Optional filter function </param>
        /// <returns></returns>
        public IEnumerator<ConversionResult> RecursivelyConvertToNative(Base rootObject, Transform? parent,
            Func<TraversalContext, bool>? predicate = null)
        {
            var traversalFunc = DefaultTraversal.CreateBIMTraverseFunc(ConverterInstance);
            
            var objectsToConvert = traversalFunc.Traverse(rootObject);
                
            if(predicate != null) objectsToConvert = objectsToConvert.Where(predicate);

            Dictionary<Base, GameObject> created = new();
            foreach (var conversionResult in ConvertTree(objectsToConvert, parent, created))
            {
                Base speckleObject = conversionResult.SpeckleObject;
                if (conversionResult.WasSuccessful(out var converted, out var ex))
                {
                    created.Add(speckleObject, converted);
                }
                else
                {
                    Debug.LogWarning($"Failed to convert Speckle object of type {speckleObject.speckle_type}\n{ex}",this);
                }

                yield return conversionResult;
            }
            
            Debug.Log($"Finished converting {rootObject.id} to native. Created {created.Count} {nameof(GameObject)}s ",this);

        }
        
        /// <summary>
        /// Converts a objectTree (see <see cref="GraphTraversal"/>) to unevaluated enumerable.
        /// As this enumerable is iterated through, each context <see cref="Base"/> object will be converted to <see cref="GameObject"/> (if successful)
        /// or <see langword="null"/> if not.
        /// </summary>
        /// <remarks>
        /// You may enumerate over multiple frames (e.g. coroutine) but you must ensure the output eventually gets fully enumerated (exactly once) 
        /// </remarks>
        /// <param name="objectTree"></param>
        /// <param name="parent"></param>
        /// <param name="outCreatedObjects"></param>
        /// <returns></returns>
        public IEnumerable<ConversionResult> ConvertTree(IEnumerable<TraversalContext> objectTree, Transform? parent, IDictionary<Base, GameObject?> outCreatedObjects)
        {
            InitializeAssetCache();
            AssetCache.BeginWrite();
            
            foreach (TraversalContext tc in objectTree)
            {
                Transform? currentParent = GetParent(tc, outCreatedObjects) ?? parent;

                ConversionResult result;
                try
                {
                    var converted = ConvertToNative(tc.current, currentParent);
                    result = new ConversionResult(tc, converted);
                    outCreatedObjects.TryAdd(tc.current, result.converted);
                }
                catch (Exception ex)
                {
                    result = new ConversionResult(tc, ex);
                }

                yield return result;
            }
            
            AssetCache.FinishWrite();
        }

        private Transform? GetParent(TraversalContext? tc, IDictionary<Base, GameObject?> createdObjects)
        {
            if (tc == null) return null; //We've reached the root object, and still not found a converted parent
            
            if(createdObjects.TryGetValue(tc.current, out GameObject? p) && p != null)
                return p.transform;
            
            //Go one level up, and repeat!
            return GetParent(tc.parent, createdObjects);
        }

        private GameObject ConvertToNative(Base speckleObject, Transform? parentTransform)
        {
            GameObject? go = ConverterInstance.ConvertToNative(speckleObject) as GameObject;
            if (go == null) throw new SpeckleException("Conversion Returned Null");
            
            go.transform.SetParent(parentTransform, true);
            
            //Set some common for all created GameObjects
            //TODO add support for more unity specific props
            if(go.name == "New Game Object" || string.IsNullOrWhiteSpace(go.name))
                go.name = AssetHelpers.GenerateObjectName(speckleObject);
            //if (baseObject["tag"] is string t) go.tag = t;
            if (speckleObject["physicsLayer"] is string layerName)
            {
                int layer = LayerMask.NameToLayer(layerName); //TODO: check how this can be interoperable with Unreal and Blender
                if (layer > -1) go.layer = layer;
            }
            //if (baseObject["isStatic"] is bool isStatic) go.isStatic = isStatic;
            return go;
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