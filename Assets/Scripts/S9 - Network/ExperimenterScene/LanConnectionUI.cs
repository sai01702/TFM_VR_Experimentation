using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class LanConnectionUI : MonoBehaviour
{
    [Header("Connection UI")]
    public TMP_InputField connectionInputField;
    public Button connectButton;
    public Button disconnectButton;
    public TMP_Text statusText;
    public Toggle useRelayToggle;

    [Header("Session Panel")]
    public TMP_Text participantText;
    public TMP_Text sceneNameText;
    public TMP_Text gameModeText;

    UnityTransport transport;

    void Start()
    {
        transport = NetworkManager.Singleton.GetComponent<UnityTransport>();

        connectButton.onClick.AddListener(Connect);
        disconnectButton.onClick.AddListener(Disconnect);

        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;

        UpdateSessionInfo();
        UpdateStatus("Disconnected");
    }

    void Connect()
    {
        string input = connectionInputField.text;

        string[] parts = input.Split(':');

        if (parts.Length != 2)
        {
            UpdateStatus("Invalid address format");
            return;
        }

        string ip = parts[0];
        ushort port = ushort.Parse(parts[1]);

        transport.ConnectionData.Address = ip;
        transport.ConnectionData.Port = port;

        NetworkManager.Singleton.StartClient();

        UpdateStatus("Connecting...");
    }

    void Disconnect()
    {
        NetworkManager.Singleton.Shutdown();
        UpdateStatus("Disconnected");
        UpdateSessionInfo();
    }

    void OnClientConnected(ulong id)
    {
        if (id == NetworkManager.Singleton.LocalClientId)
        {
            UpdateStatus("Connected");
            UpdateSessionInfo();
            connectButton.interactable = false;
            disconnectButton.interactable = true;
        }

    }

    void OnClientDisconnected(ulong id)
    {
        if (id == NetworkManager.Singleton.LocalClientId)
        {
            UpdateStatus("Disconnected");
            UpdateSessionInfo();
            connectButton.interactable = true;
            disconnectButton.interactable = false;
        }
    }

    void UpdateStatus(string msg)
    {
        statusText.text = msg;
    }

    void UpdateSessionInfo()
    {
        // If networking not active
        if (!NetworkManager.Singleton.IsClient && !NetworkManager.Singleton.IsServer)
        {
            participantText.text = "N/A";
            sceneNameText.text = "N/A";
            gameModeText.text = "N/A";
            return;
        }

        int totalClients = NetworkManager.Singleton.ConnectedClientsIds.Count;
        int observers = Mathf.Max(0, totalClients - 1);

        // ----- PARTICIPANT TEXT -----

        string hostId = "Unknown";

        if (NetworkSessionInfo.Instance != null)
            hostId = NetworkSessionInfo.Instance.HostParticipantId.Value.ToString();

        if (NetworkManager.Singleton.IsHost)
        {
            if (observers == 0)
                participantText.text = $"Host ({hostId})";
            else if (observers == 1)
                participantText.text = $"Host ({hostId}) + 1 Observer";
            else
                participantText.text = $"Host ({hostId}) + {observers} Observers";
        }
        else
        {
            participantText.text = $"Observer connected to Host ({hostId})";
        }

        // ----- SCENE NAME -----

        if (NetworkSessionInfo.Instance != null)
            sceneNameText.text = NetworkSessionInfo.Instance.HostSceneName.Value.ToString();
        else
            sceneNameText.text = "N/A";

        // ----- GAME MODE -----

        if (NetworkSessionInfo.Instance != null)
            gameModeText.text = NetworkSessionInfo.Instance.HostGameMode.Value.ToString();
        else
            gameModeText.text = "N/A";
    }

    void Update()
    {
        if (NetworkManager.Singleton.IsClient || NetworkManager.Singleton.IsServer)
        {
            sceneNameText.text = SceneManager.GetActiveScene().name;
        }
    }
}