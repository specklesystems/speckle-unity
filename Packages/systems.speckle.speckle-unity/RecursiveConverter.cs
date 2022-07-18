#nullable enable
using Objects.Converter.Unity;
using Speckle.Core.Kits;
using UnityEngine;

namespace Speckle.ConnectorUnity
{
    /// <summary>
    /// <see cref="Component"/> for recursive conversion of Speckle Objects to Native, and Native Objects to Speckle
    /// </summary>
    [AddComponentMenu("Speckle/Conversion/" + nameof(RecursiveConverter))]
    [ExecuteAlways, DisallowMultipleComponent]
    public partial class RecursiveConverter : MonoBehaviour
    {
        public virtual ISpeckleConverter ConverterInstance { get; set; } = new ConverterUnity();
        
    }
}