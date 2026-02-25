using UnityEditor;
using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;

namespace Bezi11.ExperimenterObserver.Editor
{
    public static class FixTimeoutsNow
    {
        [MenuItem("Tools/Bezi11/FIX DISCONNECT ISSUE NOW")]
        public static void FixDisconnectIssue()
        {
            string prefabPath = "Assets/Prefabs/Bezi11-ExperimenterObserver/NetworkManager.prefab";
            GameObject prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);

            if (prefabAsset == null)
            {
                Debug.LogError("NetworkManager prefab not found!");
                return;
            }

            string assetPath = AssetDatabase.GetAssetPath(prefabAsset);
            GameObject prefabContents = PrefabUtility.LoadPrefabContents(assetPath);

            NetworkManager nm = prefabContents.GetComponent<NetworkManager>();
            UnityTransport transport = prefabContents.GetComponent<UnityTransport>();

            if (nm != null)
            {
                // CRITICAL SETTINGS
                nm.NetworkConfig.ClientConnectionBufferTimeout = 60;
                nm.NetworkConfig.LoadSceneTimeOut = 300;
                nm.NetworkConfig.SpawnTimeout = 30;
                nm.NetworkConfig.ForceSamePrefabs = false;
                nm.NetworkConfig.ConnectionApproval = false; // Will be enabled when hosting starts
                nm.NetworkConfig.EnableSceneManagement = true;
                nm.NetworkConfig.EnableNetworkLogs = true;
                
                Debug.Log("✅ NetworkManager config fixed!");
                Debug.Log($"  - ClientConnectionBufferTimeout: {nm.NetworkConfig.ClientConnectionBufferTimeout}");
                Debug.Log($"  - SpawnTimeout: {nm.NetworkConfig.SpawnTimeout}");
                Debug.Log($"  - ForceSamePrefabs: {nm.NetworkConfig.ForceSamePrefabs}");
                Debug.Log($"  - ConnectionApproval: {nm.NetworkConfig.ConnectionApproval} (will be enabled at runtime)");
            }

            if (transport != null)
            {
                SerializedObject so = new SerializedObject(transport);
                so.FindProperty("m_ConnectTimeoutMS").intValue = 30000;
                so.FindProperty("m_MaxConnectAttempts").intValue = 300;
                so.FindProperty("m_DisconnectTimeoutMS").intValue = 90000;
                so.FindProperty("m_HeartbeatTimeoutMS").intValue = 3000;
                so.ApplyModifiedProperties();
                Debug.Log("✅ Transport timeouts increased!");
            }

            PrefabUtility.SaveAsPrefabAsset(prefabContents, assetPath);
            PrefabUtility.UnloadPrefabContents(prefabContents);

            EditorUtility.SetDirty(prefabAsset);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("========================================");
            Debug.Log("✅✅✅ DISCONNECT ISSUE FIX APPLIED!");
            Debug.Log("Now ensure BOTH Host and Observer use this NetworkManager prefab!");
            Debug.Log("========================================");
        }
    }
}
