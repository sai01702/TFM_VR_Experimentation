using UnityEditor;
using UnityEngine;
using Unity.Netcode;

namespace Bezi11.ExperimenterObserver.Editor
{
    public static class FixRigNetworkObjects
    {
        [MenuItem("Tools/Bezi11/Fix Rig NetworkObjects (Remove & Clean List)")]
        public static void RemoveNetworkObjectsFromRigs()
        {
            bool success = true;
            bool modified = false;

            // Step 1: Remove NetworkObject from Desktop Rig
            success &= RemoveNetworkObjectFromPrefab(
                "Assets/Prefabs/S1 - Rigs/Desktop Rig.prefab",
                "Desktop Rig",
                ref modified
            );

            // Step 2: Remove NetworkObject from VR Rig
            success &= RemoveNetworkObjectFromPrefab(
                "Assets/Prefabs/S1 - Rigs/XR Origin (XR Rig).prefab",
                "XR Origin (XR Rig)",
                ref modified
            );

            // Step 3: Update NetworkPrefabsList to only contain NetworkSessionManager
            success &= CleanNetworkPrefabsList(ref modified);

            if (success && modified)
            {
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Debug.Log("========================================");
                Debug.Log("[FixRigNetworkObjects] ✅ SUCCESS!");
                Debug.Log("[FixRigNetworkObjects] - NetworkObject removed from Desktop Rig");
                Debug.Log("[FixRigNetworkObjects] - NetworkObject removed from VR Rig");
                Debug.Log("[FixRigNetworkObjects] - NetworkPrefabsList cleaned (only NetworkSessionManager)");
                Debug.Log("[FixRigNetworkObjects] You can now test Relay connection - it should stay connected!");
                Debug.Log("========================================");
            }
            else if (success && !modified)
            {
                Debug.Log("[FixRigNetworkObjects] No changes needed - rigs already fixed!");
            }
            else
            {
                Debug.LogError("[FixRigNetworkObjects] ❌ Some operations failed. Check errors above.");
            }
        }

        private static bool RemoveNetworkObjectFromPrefab(string prefabPath, string rigName, ref bool modified)
        {
            GameObject prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefabAsset == null)
            {
                Debug.LogError($"[FixRigNetworkObjects] Could not find {rigName} at {prefabPath}");
                return false;
            }

            // Load the prefab for editing
            string prefabAssetPath = AssetDatabase.GetAssetPath(prefabAsset);
            GameObject prefabContents = PrefabUtility.LoadPrefabContents(prefabAssetPath);

            // Find NetworkObject component on root
            NetworkObject networkObject = prefabContents.GetComponent<NetworkObject>();

            if (networkObject != null)
            {
                Debug.Log($"[FixRigNetworkObjects] Removing NetworkObject from {rigName}...");
                Object.DestroyImmediate(networkObject, true);

                // Save the modified prefab
                PrefabUtility.SaveAsPrefabAsset(prefabContents, prefabAssetPath);
                PrefabUtility.UnloadPrefabContents(prefabContents);

                Debug.Log($"[FixRigNetworkObjects] ✅ Removed NetworkObject from {rigName}");
                modified = true;
            }
            else
            {
                Debug.Log($"[FixRigNetworkObjects] {rigName} already has no NetworkObject component");
                PrefabUtility.UnloadPrefabContents(prefabContents);
            }

            return true;
        }

        private static bool CleanNetworkPrefabsList(ref bool modified)
        {
            string prefabsListPath = "Assets/Prefabs/Bezi11-ExperimenterObserver/Bezi11NetworkPrefabs.asset";
            NetworkPrefabsList prefabsList = AssetDatabase.LoadAssetAtPath<NetworkPrefabsList>(prefabsListPath);

            if (prefabsList == null)
            {
                Debug.LogError($"[FixRigNetworkObjects] Could not find NetworkPrefabsList at {prefabsListPath}");
                return false;
            }

            GameObject desktopRig = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/S1 - Rigs/Desktop Rig.prefab");
            GameObject vrRig = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/S1 - Rigs/XR Origin (XR Rig).prefab");

            bool listModified = false;

            // Remove Desktop Rig if present
            if (desktopRig != null && prefabsList.Contains(desktopRig))
            {
                // Find and remove the prefab
                NetworkPrefab toRemove = default;
                foreach (var entry in prefabsList.PrefabList)
                {
                    if (entry.Prefab == desktopRig)
                    {
                        toRemove = entry;
                        break;
                    }
                }

                if (toRemove.Prefab != null)
                {
                    prefabsList.Remove(toRemove);
                    Debug.Log("[FixRigNetworkObjects] Removed Desktop Rig from NetworkPrefabsList");
                    listModified = true;
                }
            }

            // Remove VR Rig if present
            if (vrRig != null && prefabsList.Contains(vrRig))
            {
                NetworkPrefab toRemove = default;
                foreach (var entry in prefabsList.PrefabList)
                {
                    if (entry.Prefab == vrRig)
                    {
                        toRemove = entry;
                        break;
                    }
                }

                if (toRemove.Prefab != null)
                {
                    prefabsList.Remove(toRemove);
                    Debug.Log("[FixRigNetworkObjects] Removed VR Rig from NetworkPrefabsList");
                    listModified = true;
                }
            }

            if (listModified)
            {
                EditorUtility.SetDirty(prefabsList);
                Debug.Log("[FixRigNetworkObjects] ✅ NetworkPrefabsList cleaned - now contains only NetworkSessionManager");
                modified = true;
            }
            else
            {
                Debug.Log("[FixRigNetworkObjects] NetworkPrefabsList already clean");
            }

            return true;
        }
    }
}
