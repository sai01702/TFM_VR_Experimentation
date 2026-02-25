using UnityEditor;
using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;

namespace Bezi11.ExperimenterObserver.Editor
{
    public static class FixObserverNetworkManager
    {
        [MenuItem("Tools/Bezi11/FIX OBSERVER NETWORKMANAGER NOW")]
        public static void FixObserverNetworkManagerNow()
        {
            // Load the ExperimenterClientScene
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/ExperimenterClientScene.unity", OpenSceneMode.Single);
            
            // Find NetworkManager in the scene
            NetworkManager nm = null;
            foreach (var go in scene.GetRootGameObjects())
            {
                nm = go.GetComponent<NetworkManager>();
                if (nm != null) break;
                nm = go.GetComponentInChildren<NetworkManager>();
                if (nm != null) break;
            }
            
            if (nm == null)
            {
                Debug.LogError("[FixObserverNetworkManager] NetworkManager not found in ExperimenterClientScene!");
                return;
            }
            
            Debug.Log("[FixObserverNetworkManager] Found NetworkManager, applying fixes...");
            
            // CRITICAL: Set the transport reference
            var transport = nm.GetComponent<UnityTransport>();
            if (transport != null)
            {
                nm.NetworkConfig.NetworkTransport = transport;
                Debug.Log("[FixObserverNetworkManager] ✅ Transport reference set!");
            }
            else
            {
                Debug.LogError("[FixObserverNetworkManager] ❌ UnityTransport component not found!");
                return;
            }
            
            // CRITICAL FIXES
            nm.NetworkConfig.ClientConnectionBufferTimeout = 60;
            nm.NetworkConfig.LoadSceneTimeOut = 300;
            nm.NetworkConfig.SpawnTimeout = 30;
            nm.NetworkConfig.ForceSamePrefabs = false;
            nm.NetworkConfig.ConnectionApproval = false;
            nm.NetworkConfig.EnableSceneManagement = true;
            nm.NetworkConfig.EnableNetworkLogs = true;
            
            // Use the SAME NetworkPrefabsList as the host
            var prefabsList = AssetDatabase.LoadAssetAtPath<NetworkPrefabsList>("Assets/Prefabs/Bezi11-ExperimenterObserver/Bezi11NetworkPrefabs.asset");
            if (prefabsList != null)
            {
                nm.NetworkConfig.Prefabs.NetworkPrefabsLists.Clear();
                nm.NetworkConfig.Prefabs.NetworkPrefabsLists.Add(prefabsList);
                Debug.Log("[FixObserverNetworkManager] ✅ Set NetworkPrefabsList to Bezi11NetworkPrefabs");
            }
            else
            {
                Debug.LogError("[FixObserverNetworkManager] Could not find Bezi11NetworkPrefabs.asset!");
            }
            
            // Fix UnityTransport timeouts (reuse transport variable from above)
            if (transport != null)
            {
                SerializedObject so = new SerializedObject(transport);
                so.FindProperty("m_ConnectTimeoutMS").intValue = 30000;
                so.FindProperty("m_MaxConnectAttempts").intValue = 300;
                so.FindProperty("m_DisconnectTimeoutMS").intValue = 90000;
                so.FindProperty("m_HeartbeatTimeoutMS").intValue = 3000;
                so.ApplyModifiedProperties();
                Debug.Log("[FixObserverNetworkManager] ✅ Transport timeouts updated");
            }
            
            // Mark scene dirty and save
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            
            Debug.Log("========================================");
            Debug.Log("✅✅✅ OBSERVER NETWORKMANAGER FIXED!");
            Debug.Log("Settings now match HOST:");
            Debug.Log($"  - ForceSamePrefabs: {nm.NetworkConfig.ForceSamePrefabs}");
            Debug.Log($"  - ClientConnectionBufferTimeout: {nm.NetworkConfig.ClientConnectionBufferTimeout}");
            Debug.Log($"  - SpawnTimeout: {nm.NetworkConfig.SpawnTimeout}");
            Debug.Log($"  - NetworkPrefabsList: Bezi11NetworkPrefabs");
            Debug.Log("");
            Debug.Log("IMPORTANT: ExperimenterClientScene also needs:");
            Debug.Log("  - NetworkManager ✅");
            Debug.Log("  - RelayConnectionManager ✅ (should already be there)");
            Debug.Log("========================================");
            Debug.Log("NOW TEST: Observer should connect with ClientId: 123!");
        }
    }
}
