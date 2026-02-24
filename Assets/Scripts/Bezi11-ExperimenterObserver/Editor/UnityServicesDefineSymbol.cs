using UnityEditor;
using UnityEngine;

namespace Bezi11.ExperimenterObserver.Editor
{
    [InitializeOnLoad]
    public static class UnityServicesDefineSymbol
    {
        private const string DefineSymbol = "UNITY_SERVICES_INSTALLED";

        static UnityServicesDefineSymbol()
        {
            CheckAndAddDefineSymbol();
        }

        private static void CheckAndAddDefineSymbol()
        {
            bool hasUnityServicesCore = System.Type.GetType("Unity.Services.Core.UnityServices, Unity.Services.Core") != null;
            bool hasRelay = System.Type.GetType("Unity.Services.Relay.RelayService, Unity.Services.Relay") != null;
            bool hasAuthentication = System.Type.GetType("Unity.Services.Authentication.AuthenticationService, Unity.Services.Authentication") != null;

            bool shouldHaveSymbol = hasUnityServicesCore && hasRelay && hasAuthentication;

            BuildTargetGroup targetGroup = EditorUserBuildSettings.selectedBuildTargetGroup;
            string defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(targetGroup);
            bool hasSymbol = defines.Contains(DefineSymbol);

            if (shouldHaveSymbol && !hasSymbol)
            {
                defines += ";" + DefineSymbol;
                PlayerSettings.SetScriptingDefineSymbolsForGroup(targetGroup, defines);
                Debug.Log("[Bezi11] Unity Services packages detected. Relay support enabled.");
            }
            else if (!shouldHaveSymbol && hasSymbol)
            {
                defines = defines.Replace(";" + DefineSymbol, "").Replace(DefineSymbol, "");
                PlayerSettings.SetScriptingDefineSymbolsForGroup(targetGroup, defines);
                Debug.Log("[Bezi11] Unity Services packages not found. Relay support disabled.");
            }
        }
    }
}
