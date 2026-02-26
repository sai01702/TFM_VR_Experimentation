using UnityEditor;
using UnityEngine;

namespace Bezi11.ExperimenterObserver.Editor
{
    public class SetRelayModeToLAN : MonoBehaviour
    {
        [MenuItem("Tools/Bezi11/SET RELAY MODE TO LAN (DEFAULT)")]
        public static void SetToLANMode()
        {
            // Load the RelayConnectionManager prefab
            string prefabPath = "Assets/Prefabs/Bezi11-ExperimenterObserver/RelayConnectionManager.prefab";
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            
            if (prefab == null)
            {
                Debug.LogError("[SetRelayModeToLAN] Could not find RelayConnectionManager prefab!");
                return;
            }
            
            // Get the RelayConnectionManager component
            var relayManager = prefab.GetComponent<RelayConnectionManager>();
            
            if (relayManager == null)
            {
                Debug.LogError("[SetRelayModeToLAN] RelayConnectionManager component not found!");
                return;
            }
            
            // Set to LAN mode using SerializedObject
            SerializedObject so = new SerializedObject(relayManager);
            so.FindProperty("useRelayForInternetConnection").boolValue = false;
            so.ApplyModifiedProperties();
            
            // Mark prefab dirty and save
            EditorUtility.SetDirty(prefab);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            
            Debug.Log("========================================");
            Debug.Log("✅✅✅ RELAY MODE SET TO LAN (DEFAULT)!");
            Debug.Log("RelayConnectionManager prefab now defaults to LAN mode");
            Debug.Log("Host will use direct IP connection by default");
            Debug.Log("========================================");
        }
    }
}
