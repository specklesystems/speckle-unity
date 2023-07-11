using Objects.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Serialization;
using Mesh = Objects.Geometry.Mesh;

namespace Objects.Converter.Unity
{
    public partial class ConverterUnity
    {
        [field: SerializeField]
        [field: Tooltip("The units used by conversion functions")]
        public string ModelUnits { get; set; } = Speckle.Core.Kits.Units.Meters; //the default Unity units are meters

        private double ScaleToNative(double value, string units)
        {
            var f = GetConversionFactor(units);
            return value * f;
        }

        private double GetConversionFactor(string units)
        {
            return Speckle.Core.Kits.Units.GetConversionFactor(units, ModelUnits);
        }
    }
}
