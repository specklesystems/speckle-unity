using Objects.Converter.Unity;
using Speckle.Core.Kits;

namespace Speckle.ConnectorUnity.Factories
{
    public static class ConverterFactory
    {
        public static ISpeckleConverter GetDefaultConverter()
        {
            return new ConverterUnity();
        }
    }
}
