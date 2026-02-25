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
            if (NetworkManager.Singleton == null) return;

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
                Debug.Log("[NetworkBootstrap] Started hosting in RoomScene");

                // CRITICAL: Spawn NetworkSessionManager after hosting starts
                SpawnNetworkSessionManager();

                if (ParticipantSession.Instance != null)
                {
                    ParticipantSession.Instance.AppendLog("[Network] Started hosting session");
                }
            }
            else
            {
                Debug.LogError("[NetworkBootstrap] Failed to start hosting");
            }
        }

        private void SpawnNetworkSessionManager()
        {
            if (networkSessionManagerPrefab == null)
            {
                Debug.LogError("[NetworkBootstrap] NetworkSessionManager prefab not assigned!");
                return;
            }

            if (spawnedSessionManager != null)
            {
                Debug.LogWarning("[NetworkBootstrap] NetworkSessionManager already spawned");
                return;
            }

            // Instantiate and spawn the NetworkSessionManager
            spawnedSessionManager = Instantiate(networkSessionManagerPrefab);
            
            NetworkObject networkObject = spawnedSessionManager.GetComponent<NetworkObject>();
            if (networkObject != null)
            {
                networkObject.Spawn();
                Debug.Log("[NetworkBootstrap] NetworkSessionManager spawned successfully");
            }
            else
            {
                Debug.LogError("[NetworkBootstrap] NetworkSessionManager prefab missing NetworkObject component!");
                Destroy(spawnedSessionManager);
                spawnedSessionManager = null;
            }
        }
    }
}
