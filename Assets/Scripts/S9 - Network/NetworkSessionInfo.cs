using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Collections;

public class NetworkSessionInfo : NetworkBehaviour
{
    public static NetworkSessionInfo Instance;

    public NetworkVariable<FixedString64Bytes> HostParticipantId =
        new NetworkVariable<FixedString64Bytes>("N/A");

    public NetworkVariable<FixedString64Bytes> HostSceneName =
        new NetworkVariable<FixedString64Bytes>("N/A");

    public NetworkVariable<FixedString32Bytes> HostGameMode =
        new NetworkVariable<FixedString32Bytes>("N/A");

    void Awake()
    {
        Instance = this;
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            UpdateHostInfo();

            // Automatically update when host changes scenes
            SceneManager.activeSceneChanged += OnSceneChanged;
        }
    }

    void OnDestroy()
    {
        if (IsServer)
            SceneManager.activeSceneChanged -= OnSceneChanged;
    }

    void OnSceneChanged(Scene oldScene, Scene newScene)
    {
        UpdateHostInfo();
    }

    public void UpdateHostInfo()
    {
        if (!IsServer) return;

        if (ParticipantSession.Instance != null)
            HostParticipantId.Value = ParticipantSession.Instance.ParticipantId;

        HostSceneName.Value = SceneManager.GetActiveScene().name;

        if (GameSettings.Instance != null)
            HostGameMode.Value = GameSettings.Instance.CurrentMode.ToString();
    }
}