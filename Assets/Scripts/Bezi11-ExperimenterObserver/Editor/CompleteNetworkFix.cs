using UnityEditor;
using UnityEngine;
using Unity.Netcode;

namespace Bezi11.ExperimenterObserver.Editor
{
    /// <summary>
    /// Complete fix for all network timing and configuration issues.
    /// Run this after removing NetworkObject from rigs.
    /// </summary>
    public static class CompleteNetworkFix
    {
        [MenuItem("Tools/Bezi11/Complete Network Fix (All Issues)")]
        public static void ApplyAllFixes()
        {
            Debug.Log("========================================");
            Debug.Log("[CompleteNetworkFix] Starting complete network fix...");
            Debug.Log("========================================");

            bool success = true;

            // Fix 1: Update NetworkManager to NOT force same prefabs
            success &= FixNetworkManagerConfig();

            // Fix 2: Verify NetworkPrefabsList only has NetworkSessionManager
            success &= VerifyNetworkPrefabsList();

            // Fix 3: Verify NetworkSessionManager prefab is properly configured
            success &= VerifyNetworkSessionManagerPrefab();

            if (success)
            {
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                
                Debug.Log("========================================");
                Debug.Log("[CompleteNetworkFix] ✅ ALL FIXES APPLIED SUCCESSFULLY!");
                Debug.Log("[CompleteNetworkFix] Changes:");
                Debug.Log("  1. NetworkManager.ForceSamePrefabs = FALSE");
                Debug.Log("  2. NetworkPrefabsList verified");
                Debug.Log("  3. NetworkSessionManager prefab verified");
                Debug.Log("");
                Debug.Log("[CompleteNetworkFix] NOW TEST:");
                Debug.Log("  1. HOST: Start game → RoomScene → Click Desktop/VR");
                Debug.Log("  2. OBSERVER: Connect via Relay");
                Debug.Log("  3. Check console for PlayerCameraStreamer and NetworkSessionManager logs");
                Debug.Log("========================================");
            }
            else
            {
                Debug.LogError("[CompleteNetworkFix] ❌ Some fixes failed. Check errors above.");
            }
        }

        private static bool FixNetworkManagerConfig()
        {
            string prefabPath = "Assets/Prefabs/Bezi11-ExperimenterObserver/NetworkManager.prefab";
            GameObject prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);

            if (prefabAsset == null)
            {
                Debug.LogError($"[CompleteNetworkFix] Could not find NetworkManager prefab at {prefabPath}");
                return false;
            }

            string assetPath = AssetDatabase.GetAssetPath(prefabAsset);
            GameObject prefabContents = PrefabUtility.LoadPrefabContents(assetPath);

            NetworkManager networkManager = prefabContents.GetComponent<NetworkManager>();
            if (networkManager == null)
            {
                Debug.LogError("[CompleteNetworkFix] NetworkManager component not found on prefab!");
                PrefabUtility.UnloadPrefabContents(prefabContents);
                return false;
            }

            // Set ForceSamePrefabs to false
            networkManager.NetworkConfig.ForceSamePrefabs = false;
            
            // Increase timeouts for more reliable connection
            networkManager.NetworkConfig.ClientConnectionBufferTimeout = 30;
            networkManager.NetworkConfig.LoadSceneTimeOut = 180;

            Debug.Log("[CompleteNetworkFix] ✅ Updated NetworkManager config:");
            Debug.Log("  - ForceSamePrefabs: FALSE (allows client/server prefab differences)");
            Debug.Log("  - ClientConnectionBufferTimeout: 30s");
            Debug.Log("  - LoadSceneTimeOut: 180s");

            PrefabUtility.SaveAsPrefabAsset(prefabContents, assetPath);
            PrefabUtility.UnloadPrefabContents(prefabContents);

            EditorUtility.SetDirty(prefabAsset);
            return true;
        }

        private static bool VerifyNetworkPrefabsList()
        {
            string prefabsListPath = "Assets/Prefabs/Bezi11-ExperimenterObserver/Bezi11NetworkPrefabs.asset";
            NetworkPrefabsList prefabsList = AssetDatabase.LoadAssetAtPath<NetworkPrefabsList>(prefabsListPath);

            if (prefabsList == null)
            {
                Debug.LogError($"[CompleteNetworkFix] Could not find NetworkPrefabsList at {prefabsListPath}");
                return false;
            }

            int count = prefabsList.PrefabList.Count;
            Debug.Log($"[CompleteNetworkFix] NetworkPrefabsList has {count} entry(ies)");

            if (count == 1)
            {
                var entry = prefabsList.PrefabList[0];
                if (entry.Prefab != null && entry.Prefab.name.Contains("NetworkSessionManager"))
                {
                    Debug.Log("[CompleteNetworkFix] ✅ NetworkPrefabsList correct (only NetworkSessionManager)");
                    return true;
                }
            }

            Debug.LogWarning("[CompleteNetworkFix] ⚠️ NetworkPrefabsList might have incorrect entries");
            return true; // Non-critical
        }

        private static bool VerifyNetworkSessionManagerPrefab()
        {
            string prefabPath = "Assets/Prefabs/Bezi11-ExperimenterObserver/NetworkSessionManager.prefab";
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);

            if (prefab == null)
            {
                Debug.LogError($"[CompleteNetworkFix] Could not find NetworkSessionManager prefab at {prefabPath}");
                return false;
            }

            NetworkObject netObj = prefab.GetComponent<NetworkObject>();
            if (netObj == null)
            {
                Debug.LogError("[CompleteNetworkFix] NetworkSessionManager prefab missing NetworkObject component!");
                return false;
            }

            var sessionManager = prefab.GetComponent<NetworkSessionManager>();
            if (sessionManager == null)
            {
                Debug.LogError("[CompleteNetworkFix] NetworkSessionManager prefab missing NetworkSessionManager component!");
                return false;
            }

            Debug.Log("[CompleteNetworkFix] ✅ NetworkSessionManager prefab properly configured");
            return true;
        }
    }
}
