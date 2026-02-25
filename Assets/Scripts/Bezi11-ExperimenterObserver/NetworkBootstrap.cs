using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Bezi11.ExperimenterObserver
{
    public class NetworkBootstrap : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private GameObject networkManagerPrefab;
        [SerializeField] private GameObject relayManagerPrefab;
        [SerializeField] private GameObject networkSessionManagerPrefab;
        [SerializeField] private string roomSceneName = "RoomScene";

        private static bool networkManagerCreated;
        private static bool relayManagerCreated;
        private static NetworkBootstrap instance;
        private GameObject spawnedSessionManager;

        void Awake()
        {
            if (instance != null && instance != this)
            {
                Debug.Log("[NetworkBootstrap] Instance already exists, destroying duplicate");
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);
            Debug.Log("[NetworkBootstrap] NetworkBootstrap persisted across scenes");

            if (!networkManagerCreated && networkManagerPrefab != null)
            {
                GameObject nmInstance = Instantiate(networkManagerPrefab);
                DontDestroyOnLoad(nmInstance);
                networkManagerCreated = true;
                Debug.Log("[NetworkBootstrap] NetworkManager created and persisted");
            }

            if (!relayManagerCreated && relayManagerPrefab != null)
            {
                GameObject rmInstance = Instantiate(relayManagerPrefab);
                DontDestroyOnLoad(rmInstance);
                relayManagerCreated = true;
                Debug.Log("[NetworkBootstrap] RelayConnectionManager created and persisted");
            }

            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name == roomSceneName && NetworkManager.Singleton != null)
            {
                if (!NetworkManager.Singleton.IsServer && !NetworkManager.Singleton.IsClient)
                {
                    StartHosting();
                }
            }
        }

        private async void StartHosting()
        {
            if (NetworkManager.Singleton == null)
            {
                Debug.LogError("[NetworkBootstrap] NetworkManager.Singleton is null!");
                return;
            }

            Debug.Log($"[NetworkBootstrap] Starting hosting... IsServer: {NetworkManager.Singleton.IsServer}, IsClient: {NetworkManager.Singleton.IsClient}");

            if (RelayConnectionManager.Instance != null && RelayConnectionManager.Instance.IsUsingRelay)
            {
                Debug.Log("[NetworkBootstrap] Starting relay hosting...");
                string joinCode = await RelayConnectionManager.Instance.StartHostWithRelay();
                if (string.IsNullOrEmpty(joinCode))
                {
                    Debug.LogError("[NetworkBootstrap] Failed to start relay hosting");
                    return;
                }
                Debug.Log($"[NetworkBootstrap] Relay hosting started with code: {joinCode}");
            }

            bool started = NetworkManager.Singleton.StartHost();

            if (started)
            {
                Debug.Log($"[NetworkBootstrap] ✅ StartHost() succeeded! IsServer: {NetworkManager.Singleton.IsServer}, ConnectedClients: {NetworkManager.Singleton.ConnectedClientsList.Count}");

                // CRITICAL: Wait for NetworkManager to fully initialize
                await System.Threading.Tasks.Task.Delay(500);

                // Spawn NetworkSessionManager after hosting is ready
                SpawnNetworkSessionManager();

                if (ParticipantSession.Instance != null)
                {
                    ParticipantSession.Instance.AppendLog("[Network] Started hosting session");
                }
                
                // Wait another moment for everything to settle
                await System.Threading.Tasks.Task.Delay(500);
                
                Debug.Log("[NetworkBootstrap] ✅✅✅ Hosting setup COMPLETE - Server is ready for connections!");
            }
            else
            {
                Debug.LogError("[NetworkBootstrap] ❌ StartHost() failed!");
            }
        }

        private void SpawnNetworkSessionManager()
        {
            if (networkSessionManagerPrefab == null)
            {
                Debug.LogError("[NetworkBootstrap] ❌ NetworkSessionManager prefab not assigned!");
                return;
            }

            if (spawnedSessionManager != null)
            {
                Debug.LogWarning("[NetworkBootstrap] NetworkSessionManager already spawned");
                return;
            }

            if (!NetworkManager.Singleton.IsServer)
            {
                Debug.LogError("[NetworkBootstrap] ❌ Cannot spawn NetworkSessionManager - not server!");
                return;
            }

            Debug.Log("[NetworkBootstrap] Spawning NetworkSessionManager...");

            // Instantiate and spawn the NetworkSessionManager
            spawnedSessionManager = Instantiate(networkSessionManagerPrefab);
            
            NetworkObject networkObject = spawnedSessionManager.GetComponent<NetworkObject>();
            if (networkObject != null)
            {
                try
                {
                    networkObject.Spawn();
                    Debug.Log($"[NetworkBootstrap] ✅ NetworkSessionManager spawned! NetworkObjectId: {networkObject.NetworkObjectId}");
                    
                    // Update session data immediately
                    var sessionMgr = spawnedSessionManager.GetComponent<NetworkSessionManager>();
                    if (sessionMgr != null)
                    {
                        sessionMgr.UpdateSessionData();
                        Debug.Log("[NetworkBootstrap] NetworkSessionManager.UpdateSessionData() called");
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"[NetworkBootstrap] ❌ Failed to spawn NetworkSessionManager: {e.Message}");
                    Destroy(spawnedSessionManager);
                    spawnedSessionManager = null;
                }
            }
            else
            {
                Debug.LogError("[NetworkBootstrap] ❌ NetworkSessionManager prefab missing NetworkObject component!");
                Destroy(spawnedSessionManager);
                spawnedSessionManager = null;
            }
        }
    }
}
