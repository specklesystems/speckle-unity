using Speckle.Core.Kits;
using Speckle.Core.Logging;

namespace Speckle.ConnectorUnity
{
    public static class CoreUtils
    {
        public static void SetupInit()
        {
            SpeckleLog.Initialize(HostApplications.Unity.Slug, HostApplications.Unity.GetVersion(CoreUtils.GetHostAppVersion()));
            Setup.Init(HostApplications.Unity.GetVersion(CoreUtils.GetHostAppVersion()), HostApplications.Unity.Slug);
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
        
        
        public static bool Tautology<T>(T _) => true;

    }
}
