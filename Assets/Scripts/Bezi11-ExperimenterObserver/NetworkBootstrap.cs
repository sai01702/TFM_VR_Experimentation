using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Bezi11.ExperimenterObserver
{
    public class NetworkBootstrap : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private GameObject networkManagerPrefab;
        [SerializeField] private GameObject relayManagerPrefab;
        [SerializeField] public GameObject networkSessionManagerPrefab; // Made public for ConnectionBoardActivator
        [SerializeField] private string roomSceneName = "RoomScene";
        [SerializeField] private bool autoStartHosting = false; // DISABLED by default - use ConnectionBoardActivator instead

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
                    Debug.Log("[NetworkBootstrap] RoomScene loaded - auto-starting host session...");
                    StartHosting();
                }
                else
                {
                    Debug.Log("[NetworkBootstrap] RoomScene loaded but already hosting/connected, skipping.");
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

            Debug.Log($"[NetworkBootstrap] Starting host session...");

            bool usingRelay = RelayConnectionManager.Instance != null && RelayConnectionManager.Instance.IsUsingRelay;

            if (usingRelay)
            {
                Debug.Log("[NetworkBootstrap] Relay mode - allocating relay...");
                string joinCode = await RelayConnectionManager.Instance.StartHostWithRelay();
                if (string.IsNullOrEmpty(joinCode))
                {
                    Debug.LogError("[NetworkBootstrap] Failed to start relay hosting");
                    return;
                }
                Debug.Log($"[NetworkBootstrap] Relay code: {joinCode}");
            }
            else
            {
                Debug.Log("[NetworkBootstrap] LAN mode - hosting on port 7777...");
                var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
                if (transport != null)
                {
                    transport.ConnectionData.Address = "0.0.0.0";
                    transport.ConnectionData.Port = 7777;
                    transport.ConnectionData.ServerListenAddress = "0.0.0.0";
                }
            }

            bool started = NetworkManager.Singleton.StartHost();

            if (started)
            {
                Debug.Log($"[NetworkBootstrap] ✅ StartHost() succeeded!");

                await System.Threading.Tasks.Task.Delay(500);
                SpawnNetworkSessionManager();

                if (ParticipantSession.Instance != null)
                {
                    ParticipantSession.Instance.AppendLog("[Network] Started hosting session");
                }
                
                await System.Threading.Tasks.Task.Delay(500);
                Debug.Log("[NetworkBootstrap] ✅ Hosting setup complete!");
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
