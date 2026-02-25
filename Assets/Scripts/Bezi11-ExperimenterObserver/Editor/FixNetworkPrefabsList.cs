using UnityEditor;
using UnityEngine;
using Unity.Netcode;

namespace Bezi11.ExperimenterObserver.Editor
{
    public static class FixNetworkPrefabsList
    {
        [MenuItem("Tools/Bezi11/Fix Network Prefabs List (Add Rigs)")]
        public static void AddMissingRigPrefabs()
        {
            string prefabsListPath = "Assets/Prefabs/Bezi11-ExperimenterObserver/Bezi11NetworkPrefabs.asset";
            string desktopRigPath = "Assets/Prefabs/S1 - Rigs/Desktop Rig.prefab";
            string vrRigPath = "Assets/Prefabs/S1 - Rigs/XR Origin (XR Rig).prefab";

            NetworkPrefabsList prefabsList = AssetDatabase.LoadAssetAtPath<NetworkPrefabsList>(prefabsListPath);
            if (prefabsList == null)
            {
                Debug.LogError($"[FixNetworkPrefabsList] Could not find NetworkPrefabsList at {prefabsListPath}");
                return;
            }

            GameObject desktopRig = AssetDatabase.LoadAssetAtPath<GameObject>(desktopRigPath);
            if (desktopRig == null)
            {
                Debug.LogError($"[FixNetworkPrefabsList] Could not find Desktop Rig at {desktopRigPath}");
                return;
            }

            GameObject vrRig = AssetDatabase.LoadAssetAtPath<GameObject>(vrRigPath);
            if (vrRig == null)
            {
                Debug.LogError($"[FixNetworkPrefabsList] Could not find VR Rig at {vrRigPath}");
                return;
            }

            bool modified = false;

            if (!prefabsList.Contains(desktopRig))
            {
                var newEntry = new NetworkPrefab
                {
                    Prefab = desktopRig,
                    Override = NetworkPrefabOverride.None
                };
                prefabsList.Add(newEntry);
                Debug.Log($"[FixNetworkPrefabsList] ✅ Added Desktop Rig to NetworkPrefabsList");
                modified = true;
            }
            else
            {
                Debug.Log("[FixNetworkPrefabsList] Desktop Rig already in list");
            }

            if (!prefabsList.Contains(vrRig))
            {
                var newEntry = new NetworkPrefab
                {
                    Prefab = vrRig,
                    Override = NetworkPrefabOverride.None
                };
                prefabsList.Add(newEntry);
                Debug.Log($"[FixNetworkPrefabsList] ✅ Added VR Rig to NetworkPrefabsList");
                modified = true;
            }
            else
            {
                Debug.Log("[FixNetworkPrefabsList] VR Rig already in list");
            }

            if (modified)
            {
                EditorUtility.SetDirty(prefabsList);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Debug.Log("[FixNetworkPrefabsList] ✅✅✅ NetworkPrefabsList updated successfully!");
                Debug.Log("[FixNetworkPrefabsList] You can now test the Relay connection - it should stay connected!");
            }
            else
            {
                Debug.Log("[FixNetworkPrefabsList] No changes needed - all prefabs already registered");
            }
        }
    }
}
