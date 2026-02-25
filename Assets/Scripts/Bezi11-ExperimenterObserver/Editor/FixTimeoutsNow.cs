using UnityEditor;
using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;

namespace Bezi11.ExperimenterObserver.Editor
{
    public static class FixTimeoutsNow
    {
        [MenuItem("Tools/Bezi11/FIX TIMEOUTS NOW")]
        public static void FixAllTimeouts()
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
                nm.NetworkConfig.ClientConnectionBufferTimeout = 60;
                nm.NetworkConfig.LoadSceneTimeOut = 300;
                nm.NetworkConfig.SpawnTimeout = 30;
                nm.NetworkConfig.ForceSamePrefabs = false;
                nm.NetworkConfig.ConnectionApproval = false;
                Debug.Log("✅ NetworkManager timeouts increased!");
            }

            if (transport != null)
            {
                SerializedObject so = new SerializedObject(transport);
                so.FindProperty("m_ConnectTimeoutMS").intValue = 10000;
                so.FindProperty("m_MaxConnectAttempts").intValue = 200;
                so.FindProperty("m_DisconnectTimeoutMS").intValue = 60000;
                so.FindProperty("m_HeartbeatTimeoutMS").intValue = 2000;
                so.ApplyModifiedProperties();
                Debug.Log("✅ UnityTransport timeouts increased!");
            }

            PrefabUtility.SaveAsPrefabAsset(prefabContents, assetPath);
            PrefabUtility.UnloadPrefabContents(prefabContents);

            EditorUtility.SetDirty(prefabAsset);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("========================================");
            Debug.Log("✅✅✅ TIMEOUTS FIXED!");
            Debug.Log("Connection timeout: 10 seconds");
            Debug.Log("Max attempts: 200");
            Debug.Log("Disconnect timeout: 60 seconds");
            Debug.Log("NOW TEST AGAIN!");
            Debug.Log("========================================");
        }
    }
}
