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
        [SerializeField] private string roomSceneName = "RoomScene";

        private static bool networkManagerCreated;
        private static bool relayManagerCreated;

        void Awake()
        {
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
                string joinCode = await RelayConnectionManager.Instance.StartHostWithRelay();
                if (string.IsNullOrEmpty(joinCode))
                {
                    Debug.LogError("[NetworkBootstrap] Failed to start relay hosting");
                    return;
                }
            }

            bool started = NetworkManager.Singleton.StartHost();

            if (started)
            {
                Debug.Log("[NetworkBootstrap] Started hosting in RoomScene");

                if (ParticipantSession.Instance != null)
                {
                    ParticipantSession.Instance.AppendLog("[Network] Started hosting session");
                }
            }
            else
            {
                Debug.LogWarning("[NetworkBootstrap] Failed to start hosting");
            }
        }
    }
}
