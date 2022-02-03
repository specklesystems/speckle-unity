using Objects.Geometry;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Objects.BuiltElements;
using Unity.Plastic.Antlr3.Runtime.Debug;
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

    public IEnumerable<string> GetServicedApplications() => new string[] {Applications.Other}; //TODO: add unity

    public HashSet<Exception> ConversionErrors { get; private set; } = new HashSet<Exception>();


    public List<ApplicationPlaceholderObject> ContextObjects { get; set; } = new List<ApplicationPlaceholderObject>();
    
    public void SetContextDocument(object doc) => throw new NotImplementedException();

    public void SetContextObjects(List<ApplicationPlaceholderObject> objects) => ContextObjects = objects;

    public void SetPreviousContextObjects(List<ApplicationPlaceholderObject> objects) =>
      throw new NotImplementedException();
    
    public Base ConvertToSpeckle(object @object)
    {
      switch (@object)
      {
        case GameObject o:
          if (o.GetComponent<MeshFilter>() != null)
            return MeshToSpeckle(o);
          throw new NotSupportedException();
        default:
          throw new NotSupportedException();
      }
    }

    public object ConvertToNative(Base @object)
    {
      switch (@object)
      {
        // case Point o:
        //   return PointToNative(o);
        // case Line o:
        //   return LineToNative(o);
        // case Polyline o:
        //   return PolylineToNative(o);
        // case Curve o:
        //   return CurveToNative(o);
        // case View3D o:
        //   return View3DToNative(o);
        case Mesh o:
          return MeshToNative(o);
        default:
          //capture any other object that might have a mesh representation
          if (@object["displayValue"] is Base b)
            return ConvertToNative(b);
          if (@object["displayValue"] is IEnumerable<Base> bs)
            return MeshesToNative(@object, bs.OfType<Mesh>().ToList());
          if (@object["displayMesh"] is Base m)
            return ConvertToNative(m);
          Debug.LogWarning($"Skipping {@object.GetType()} {@object.id} - Not supported type");
          return null;
      }

    }

    public List<Base> ConvertToSpeckle(List<object> objects)
    {
      return objects.Select(ConvertToSpeckle).ToList();
    }

    public List<object> ConvertToNative(List<Base> objects)
    {
      return objects.Select(x => ConvertToNative(x)).ToList();
      ;
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
          if (@object["displayValue"] is Base)
            return true;
          if (@object["displayValue"] is IEnumerable<Base>)
            return true;
          if (@object["displayMesh"] is Base)
            return true;
          return false;
      }
    }

    #endregion implemented methods
  }
}