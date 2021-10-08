using System;
using System.ComponentModel;

namespace Speckle.ConnectorUnity
{
  [Serializable]
  public class SpeckleProperty : INotifyPropertyChanged
  {
    private string key;
    private object value;

    public SpeckleProperty(string k, object v)
    {
      key = k;
      value = v;
    }

    public string Key
    {
      get => key;
      set
      {
        key = value;
        OnPropertyChanged("Key");
      }
    }

    public object Value
    {
      get => value;
      set
      {
        this.value = value;
        OnPropertyChanged("Value");
      }
    }

    [field: NonSerialized]
    public event PropertyChangedEventHandler PropertyChanged;

    public void OnPropertyChanged(string name)
    {
      var handler = PropertyChanged;
      handler?.Invoke(this, new PropertyChangedEventArgs(name));
    }

  }
}