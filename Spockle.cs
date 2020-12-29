using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Speckle.Core;
using Speckle.Core.Models;
using Speckle.Core.Api;
using Speckle.Core.Credentials;
using System;
using System.Threading.Tasks;
using System.Linq;
using Speckle.Core.Transports;
using Objects.Converter.Unity;
using Objects.Geometry;

namespace Speckle.ConnectorUnity
{
  public class Spockle : MonoBehaviour
  {

    ConverterUnity _converter = new ConverterUnity();

    async void Start()
    {
      await GetStreamLatestCommit("cd83745025");
    }


    void Update()
    {

    }

    private async Task GetStreamLatestCommit(string streamId)
    {
      try
      {

        var account = AccountManager.GetDefaultAccount();
        var client = new Client(account);
        var res = await client.StreamGet(streamId);
        var mainBranch = res.branches.items.FirstOrDefault(b => b.name == "main");
        var commit = mainBranch.commits.items[0];
        var transport = new ServerTransport(account, streamId);
        var @base = await Operations.Receive(
          commit.referencedObject,
          remoteTransport: transport
        );



        var data = ConvertRecursivelyToNative(@base, commit.id);


      }
      catch (Exception e)
      {

      }
    }

    /// <summary>
    /// Helper method to convert a tree-like structure (nested lists) to Native
    /// </summary>
    /// <param name="base"></param>
    /// <returns></returns>
    public GameObject ConvertRecursivelyToNative(Base @base, string name)
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


    private List<GameObject> RecurseTreeToNative(object @object)
    {
      var objects = new List<GameObject>();
      if (IsList(@object))
      {
        var list = ((IEnumerable)@object).Cast<object>();
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

      var @base = (Base)value;

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

          //if (converted is UnityMesh)
          //{
          //  MeshRenderer mr = converted.gameObject.GetComponent<MeshRenderer>();
          //  mr.material = new Material(Shader.Find("Diffuse"));
          //}


          objects.Add(converted);
        }
        catch (Exception ex)
        {
          Core.Logging.Log.CaptureAndThrow(ex);
        }
      }



      return objects;
    }


    public static bool IsList(object @object)
    {
      if (@object == null)
        return false;

      var type = @object.GetType();
      return (typeof(IEnumerable).IsAssignableFrom(type) && !typeof(IDictionary).IsAssignableFrom(type) &&
              type != typeof(string));
    }

    public static bool IsDictionary(object @object)
    {
      if (@object == null)
        return false;

      Type type = @object.GetType();
      return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Dictionary<,>);
    }
  }

}