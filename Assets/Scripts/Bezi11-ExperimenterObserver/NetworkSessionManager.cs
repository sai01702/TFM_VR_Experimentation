using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Bezi11.ExperimenterObserver
{
    public class NetworkSessionManager : NetworkBehaviour
    {
        public static NetworkSessionManager Instance { get; private set; }

        private NetworkVariable<NetworkString> participantId = new NetworkVariable<NetworkString>(
            new NetworkString("N/A"),
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

        private NetworkVariable<NetworkString> currentScene = new NetworkVariable<NetworkString>(
            new NetworkString("N/A"),
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

        private NetworkVariable<NetworkString> gameMode = new NetworkVariable<NetworkString>(
            new NetworkString("N/A"),
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

        public string ParticipantId => participantId.Value.ToString();
        public string CurrentScene => currentScene.Value.ToString();
        public string GameMode => gameMode.Value.ToString();

        private float nextUpdateTime;
        private const float UpdateInterval = 2f;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            Debug.Log("[NetworkSessionManager] Awake - Instance set");
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            Debug.Log($"[NetworkSessionManager] OnNetworkSpawn - IsServer: {IsServer}, IsClient: {IsClient}");

            if (IsServer)
            {
                UpdateSessionData();
                Debug.Log("[NetworkSessionManager] Server: Updated session data immediately");
            }
            else if (IsClient)
            {
                Debug.Log($"[NetworkSessionManager] Client received NetworkSessionManager - Participant: {ParticipantId}, Scene: {CurrentScene}, Mode: {GameMode}");
            }
        }

        void Update()
        {
            // Periodic update on server to ensure data stays fresh
            if (IsServer && Time.time >= nextUpdateTime)
            {
                UpdateSessionData();
                nextUpdateTime = Time.time + UpdateInterval;
            }
        }

        public void UpdateSessionData()
        {
            if (!IsServer) return;

            if (ParticipantSession.Instance != null)
            {
                participantId.Value = new NetworkString(ParticipantSession.Instance.ParticipantId ?? "N/A");
            }

            currentScene.Value = new NetworkString(SceneManager.GetActiveScene().name);

            if (GameSettings.Instance != null)
            {
                gameMode.Value = new NetworkString(GameSettings.Instance.CurrentMode.ToString());
            }

            Debug.Log($"[NetworkSessionManager] Updated session data - Participant: {participantId.Value}, Scene: {currentScene.Value}, Mode: {gameMode.Value}");
        }

        public void SubscribeToParticipantIdChanges(System.Action<string> callback)
        {
            participantId.OnValueChanged += (previous, current) => callback(current.ToString());
        }

        public void SubscribeToSceneChanges(System.Action<string> callback)
        {
            currentScene.OnValueChanged += (previous, current) => callback(current.ToString());
        }

        public void SubscribeToGameModeChanges(System.Action<string> callback)
        {
            gameMode.OnValueChanged += (previous, current) => callback(current.ToString());
        }
    }

    public struct NetworkString : INetworkSerializable
    {
        private string value;

        public NetworkString(string value)
        {
            this.value = value ?? string.Empty;
        }

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref value);
        }

        public override string ToString()
        {
            return value ?? string.Empty;
        }

        public static implicit operator string(NetworkString ns) => ns.ToString();
    }
}
