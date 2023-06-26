#nullable enable
using Speckle.Core.Kits;
using Speckle.Core.Logging;
using Speckle.Core.Models;

namespace Speckle.ConnectorUnity.Utils
{
    public static class CoreUtils
    {
        public static void SetupInit()
        {
            Setup.Init(HostApplications.Unity.GetVersion(GetHostAppVersion()), HostApplications.Unity.Slug);
        }
        
        public static HostAppVersion GetHostAppVersion()
        {
            #if UNITY_2019
            return  HostAppVersion.v2019;
            #elif UNITY_2020
            return  HostAppVersion.v2020;
            #elif UNITY_2021
            return  HostAppVersion.v2021;
            #elif UNITY_2022
            return  HostAppVersion.v2022;
            #elif UNITY_2023
            return  HostAppVersion.v2023;
            #elif UNITY_2024
            return  HostAppVersion.v2024;
            #elif UNITY_2025
            return  HostAppVersion.v2025;
            #else
            return HostAppVersion.v;
            #endif
        }
        
        public const string ObjectNameSeparator = " -- ";
    
        /// <param name="speckleObject">The object to be named</param>
        /// <returns>A human-readable Object name unique to the given <paramref name="speckleObject"/></returns>
        public static string GenerateObjectName(Base speckleObject)
        {
            var prefix = GetFriendlyObjectName(speckleObject) ?? SimplifiedSpeckleType(speckleObject);
            return $"{prefix}{ObjectNameSeparator}{speckleObject.id}";
        }

        public static string? GetFriendlyObjectName(Base speckleObject)
        {
            return speckleObject["name"] as string
                ?? speckleObject["Name"] as string
                ?? speckleObject["family"] as string;
        }
        
        /// <param name="speckleObject"></param>
        /// <returns>The most significant type in a given <see cref="Base.speckle_type"/></returns>
        public static string SimplifiedSpeckleType(Base speckleObject)
        {
            return speckleObject.speckle_type.Split(':')[^1];
        }
        
    }
}
