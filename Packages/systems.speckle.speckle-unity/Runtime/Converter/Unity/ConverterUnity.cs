using Speckle.Core.Kits;
using Speckle.Core.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Objects.BuiltElements;
using Objects.Organization;
using Objects.Other;
using Speckle.ConnectorUnity.Utils;
using Speckle.ConnectorUnity.NativeCache;
using Speckle.ConnectorUnity.Wrappers;
using UnityEngine;
using Mesh = Objects.Geometry.Mesh;
using Object = UnityEngine.Object;

namespace Objects.Converter.Unity
{
    public partial class ConverterUnity : ISpeckleConverter
    {
        #region implemented methods

        public void SetConverterSettings(object settings) => throw new NotImplementedException();

        public string Description => "Default Speckle Kit for Unity";
        public string Name => nameof(ConverterUnity);
        public string Author => "Speckle";
        public string WebsiteOrEmail => "https://speckle.systems";

        public ProgressReport Report { get; }
        public ReceiveMode ReceiveMode { get; set; }

        public IEnumerable<string> GetServicedApplications() => new string[] {HostApplications.Unity.Name};

        public AbstractNativeCache LoadedAssets { get; private set; }

        public void SetContextDocument(object doc)
        {
            if (doc is not AbstractNativeCache context)
                throw new ArgumentException(
                    $"Expected {nameof(doc)} to be of type {typeof(Dictionary<string, Object>)}", nameof(doc));
            LoadedAssets = context;
        }

        public void SetContextObjects(List<ApplicationObject> objects) => throw new NotImplementedException();

        public void SetPreviousContextObjects(List<ApplicationObject> objects) => throw new NotImplementedException();

#nullable enable
        public object? ConvertToNative(Base @object) => ConvertToNativeGameObject(@object);

        public Base ConvertToSpeckle(object @object)
        {
            if (!(@object is GameObject go))
                throw new NotSupportedException($"Cannot convert object of type {@object.GetType()} to Speckle");
            return ConvertGameObjectToSpeckle(go);
        }

        #endregion implemented methods


        public Base ConvertGameObjectToSpeckle(GameObject go)
        {
            Base speckleObject = CreateSpeckleObjectFromProperties(go);

            speckleObject["name"] = go.name;
            //speckleObject["transform"] = TransformToSpeckle(go.Transform); //TODO
            speckleObject["tag"] = go.tag;
            speckleObject["physicsLayer"] = LayerMask.LayerToName(go.layer);
            //speckleObject["isStatic"] = go.isStatic; //todo figure out realtime-rendered static mobility interoperability (unreal)

            foreach (Component component in go.GetComponents<Component>())
            {
                try
                {
                    switch (component)
                    {
                        case MeshFilter meshFilter:
                            var displayValues = MeshToSpeckle(meshFilter);

                            speckleObject.SetDetachedPropertyChecked("displayValue", displayValues);

                            break;
                        // case Camera camera:
                        //     speckleObject["cameraComponent"] = CameraToSpeckle(camera);
                        //     break;
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"Failed to convert {component.GetType()} component\n{e}", component);
                }
            }

            return speckleObject;
        }


        public GameObject? ConvertToNativeGameObject(Base speckleObject)
        {
            switch (speckleObject)
            {
                // case Point o:
                //   return PointToNative(o);
                // case Line o:
                //   return LineToNative(o);
                // case Polyline o:
                //   return PolylineToNative(o);
                // case Curve o:
                //   return CurveToNative(o);
                case View3D v:
                    return View3DToNative(v);
                case Mesh o:
                    return MeshToNative(o);
                case Instance o:
                    return InstanceToNative(o);
                case Collection c:
                    return CollectionToNative(c);
                default:

                    //Object is not a raw geometry, convert it as display value element
                    GameObject? element = DisplayValueToNative(speckleObject);
                    if (element != null)
                    {
                        if (!speckleObject.speckle_type.Contains("Objects.Geometry"))
                            AttachSpeckleProperties(element, speckleObject.GetType(), GetProperties(speckleObject));

                        return element;
                    }

                    return new GameObject();
            }
        }


        public IList<string> DisplayValuePropertyAliases { get; set; } =
            new[] {"displayValue", "@displayValue", "displayMesh", "@displayMesh"};

        public GameObject? DisplayValueToNative(Base @object)
        {
            foreach (string alias in DisplayValuePropertyAliases)
            {
                switch (@object[alias])
                {
                    //capture any other object that might have a mesh representation
                    case IList dvCollection:
                        return MeshesToNative(@object, dvCollection.OfType<Mesh>().ToList());
                    case Mesh dvMesh:
                        return MeshesToNative(@object, new[] {dvMesh});
                    case Base dvBase:
                        return ConvertToNativeGameObject(dvBase);
                }
            }

            return null;
        }
        
        private SpeckleProperties AttachSpeckleProperties(GameObject go, Type speckleType,
            IDictionary<string, object?> properties)
        {
            var sd = go.AddComponent<SpeckleProperties>();
            sd.Data = properties;
            sd.SpeckleType = speckleType;
            return sd;
        }
        

        public List<Base> ConvertToSpeckle(List<object> objects)
        {
            return objects.Select(ConvertToSpeckle).ToList();
        }

        public List<object?> ConvertToNative(List<Base> objects)
        {
            return objects.Select(x => ConvertToNative(x)).ToList();
        }

        public bool CanConvertToSpeckle(object @object)
        {
            switch (@object)
            {
                case GameObject o:
                    return o.GetComponent<MeshFilter>() != null;
                default:
                    return false;
            }
        }

        public GameObject CollectionToNative(Collection collection)
        {
            var name = collection.name ?? $"{collection.collectionType} -- {collection.applicationId ?? collection.id}";
            var go = new GameObject(name);
            AttachSpeckleProperties(go, collection.GetType(), GetProperties(collection));
            if (name == "Rooms")
            {
                go.SetActive(false);
            }
                
            return go;
        }
        
        public bool CanConvertToNative(Base @object)
        {
            switch (@object)
            {
                // case Point _:
                //   return true;
                // case Line _:
                //   return true;
                // case Polyline _:
                //   return true;
                // case Curve _:
                //   return true;
                // case View2D:
                //     return false;
                // case View _:
                //   return true;
                case Model:
                    return false; //This allows us to traverse older commits pre-collections
                case Collection:
                    return true;
                case Mesh:
                    return true;
                case Instance:
                    return true;
                default:

                    foreach (string alias in DisplayValuePropertyAliases)
                    {
                        if (@object[alias] is Base)
                            return true;
                        if (@object[alias] is IList)
                            return true;
                    }
 
                    return false;
            }
        }
    }
}
