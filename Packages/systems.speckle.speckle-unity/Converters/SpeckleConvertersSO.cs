using Speckle.Core.Models;
using UnityEngine;

namespace Objects.Converter.Unity
{
  public interface IConvertMono
  {
    public GameObject ConvertToMono(Base @base);
    public bool CanConvertToMono(Base @base);
    
  }
  
  public abstract class SpeckleConvertersSO : ScriptableObject, IConvertMono
  {
    public abstract GameObject ConvertToMono(Base @base);
    public abstract bool CanConvertToMono(Base @base);
  }
}