using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Speckle.Core.Api;
using Speckle.Core.Models;
using UnityEngine;

namespace Speckle.ConnectorUnity
{

  [Serializable]
  public sealed class SpeckleObservablePropertiesCollection : ObservableCollection<SpeckleProperty>
  {
    public string serializedData;

    public SpeckleObservablePropertiesCollection()
    {
      CollectionChanged += (sender, args) => Serialize();
    }

    [Serializable]
    private class PropertiesWrapper : Base
    {
      public PropertiesWrapper(IDictionary<string, object> data)
      {
        foreach (var v in data) this[v.Key] = v.Value;
      }
    }

    private SpeckleProperty GetByKey(string key)
    {
      return Items.FirstOrDefault(i => i.Key.Equals(key));
    }

    private SpeckleProperty CreateProp(string key, object value)
    {
      var p = new SpeckleProperty(key, value);

      p.PropertyChanged += (sender, args) =>
      {
        Debug.Log(args.PropertyName + " " + "has changed!");
        Serialize();
      };

      return p;
    }

    private bool Serialize()
    {
      var speckleData = new PropertiesWrapper(ToDictionary());
      serializedData = Operations.Serialize(speckleData);

      return serializedData.Valid();
    }

    public void Set(string props)
    {
      var propsWrapper = Operations.Deserialize(props);

      Set(propsWrapper.GetMembers());
    }

    public void Set(Dictionary<string, object> value)
    {
      Clear();

      foreach (var item in value)
        Add(item.Key, item.Value);

      Serialize();
    }

    public Dictionary<string, object> ToDictionary()
    {
      var res = new Dictionary<string, object>();

      foreach (var i in Items)
        res[i.Key] = i.Value;

      return res;
    }

    public void Add(string key, object value)
    {
      if (ContainsKey(key))
        throw new ArgumentException("The dictionary already contains the key");

      Add(CreateProp(key, value));
    }

    public bool ContainsKey(string key)
    {
      var r = this.FirstOrDefault(i => Equals(key, i.Key));

      return!Equals(default(SpeckleProperty), r);
    }

    public bool Remove(string key)
    {
      var remove = Items.Where(pair => Equals(key, pair.Key)).ToList();

      foreach (var pair in remove)
        Items.Remove(pair);

      Serialize();

      return remove.Count > 0;
    }

    public bool TryGetValue(string key, out object value)
    {
      value = default;
      var res = GetByKey(key);

      if (Equals(res, default(SpeckleProperty)))
        return false;

      value = res.Value;
      return true;
    }

    public object this[string key]
    {
      get
      {
        var res = GetByKey(key);
        if (res != null && res.Value != null)
          return res.Value;

        return null;
      }
      set
      {
        if (ContainsKey(key))
          Remove(key);

        Add(key, value);
      }
    }

    public ICollection<string> Keys
    {
      get => (from i in Items select i.Key).ToList();
    }
    public ICollection<object> Values
    {
      get => (from i in Items select i.Value).ToList();
    }

  }
}