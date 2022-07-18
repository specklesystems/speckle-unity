using Speckle.Core.Kits;
using Speckle.Core.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Objects.BuiltElements;
using UnityEngine;
using Mesh = Objects.Geometry.Mesh;

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

    public IEnumerable<string> GetServicedApplications() => new string[] {VersionedHostApplications.Unity};

    public HashSet<Exception> ConversionErrors { get; private set; } = new HashSet<Exception>();


    public List<ApplicationPlaceholderObject> ContextObjects { get; set; } = new List<ApplicationPlaceholderObject>();
    
    public void SetContextDocument(object doc) => throw new NotImplementedException();

    public void SetContextObjects(List<ApplicationPlaceholderObject> objects) => ContextObjects = objects;

    public void SetPreviousContextObjects(List<ApplicationPlaceholderObject> objects) =>
      throw new NotImplementedException();
    
#nullable enable
     public object? ConvertToNative(Base @object) => ConvertToNativeGameObject(@object);

    public Base ConvertToSpeckle(object @object)
    {
      if (!(@object is GameObject go)) throw new NotSupportedException($"Cannot convert object of type {@object.GetType()} to Speckle");
      return ConvertGameObjectToSpeckle(go);
    }
    
#endregion implemented methods


    
    public Base ConvertGameObjectToSpeckle(GameObject go)
    {

      Base speckleObject = CreateSpeckleObjectFromProperties(go);

      speckleObject["name"] = go.name;
      //speckleObject["transform"] = TransformToSpeckle(go.Transform); //TODO
      speckleObject["tag"] = go.tag;
      speckleObject["layer"] = go.layer;
      speckleObject["isStatic"] = go.isStatic;

      foreach (Component component in go.GetComponents<Component>())
      {
        try
        {
          switch (component)
          {
            case MeshFilter meshFilter:
              speckleObject["@displayValue"] = MeshToSpeckle(meshFilter);
              break;
            // case Camera camera:
            //     speckleObject["cameraComponent"] = CameraToSpeckle(camera);
            //     break;
          }
        }
        catch(Exception e)
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
        default:
          
          //Object is not a raw geometry, convert it as display value element
          GameObject? element = DisplayValueToNative(speckleObject);
          if (element != null)
          {
            AttachSpeckleProperties(element, speckleObject.GetType(), GetProperties(speckleObject));
            return element;
          }

          return new GameObject();
      }

    }


    public IList<string> DisplayValuePropertyAliases { get; set; } = new[] {"displayValue","@displayValue","displayMesh", "@displayMesh" };
    public GameObject? DisplayValueToNative(Base @object)
    {
      foreach (string alias in DisplayValuePropertyAliases)
      {
        switch (@object[alias])
        {
          //capture any other object that might have a mesh representation
          case IList dvCollection:
            return MeshesToNative(dvCollection.OfType<Mesh>().ToList());
          case Mesh dvMesh:
            return MeshesToNative(new[] {dvMesh});
          case Base dvBase:
            return ConvertToNativeGameObject(dvBase);
        }
      }
      return null;
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
        // case View3D _:
        //   return true;
        // case View2D _:
        //   return false;
        case Mesh _:
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