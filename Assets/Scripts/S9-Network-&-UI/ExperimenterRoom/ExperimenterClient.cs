using Mirror;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class ExperimenterClient : MonoBehaviour
{
    [Header("Connection UI")]
    public GameObject connectionPanel;
    public TMP_InputField ipInputField;
    public Button connectButton;
    public TextMeshProUGUI statusText;

    [Header("Observer UI")]
    public GameObject observerPanel;
    public TextMeshProUGUI participantIDText;
    public TextMeshProUGUI sceneNameText;
    public TextMeshProUGUI modeText;
    public Toggle followToggle;
    public Button disconnectButton;

    [Header("Settings")]
    public string defaultIP = "127.0.0.1";
    public int defaultPort = 7777;

    [Header("Camera")]
    public GameObject experimenterCameraPrefab;

    private VRNetworkManager networkManager;
    private bool isConnected = false;

    void Awake()
    {
        // Keep this object alive across any potential scene transitions so
        // that the scene-loading coroutine is never interrupted.
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        // Setup UI
        if (connectButton != null)
            connectButton.onClick.AddListener(OnConnectClicked);

        if (disconnectButton != null)
            disconnectButton.onClick.AddListener(OnDisconnectClicked);

        if (ipInputField != null)
            ipInputField.text = defaultIP;

        // Initial UI state
        ShowConnectionPanel();

        UpdateStatus("Enter server IP address from QR code", Color.white);
    }

    void Update()
    {
        // Update observer info if connected
        if (isConnected)
        {
            UpdateObserverInfo();
        }
    }

    public void OnConnectClicked()
    {
        string ipAddress = ipInputField.text.Trim();

        if (string.IsNullOrEmpty(ipAddress))
        {
            UpdateStatus("Please enter IP address!", Color.red);
            return;
        }

        // Parse IP and Port
        string[] parts = ipAddress.Split(':');
        string ip = parts[0];
        int port = parts.Length > 1 ? int.Parse(parts[1]) : defaultPort;

        // Find or create network manager
        networkManager = VRNetworkManager.Instance;
        if (networkManager == null)
        {
            networkManager = FindObjectOfType<VRNetworkManager>();
        }

        if (networkManager == null)
        {
            UpdateStatus("Network Manager not found!", Color.red);
            return;
        }

        // Set network manager address
        networkManager.networkAddress = ip;
        var transport = networkManager.GetComponent<TelepathyTransport>();
        if (transport != null)
        {
            transport.port = (ushort)port;
        }

        UpdateStatus($"Connecting to {ip}:{port}...", Color.yellow);

        // Start as client
        networkManager.StartClient();

        // Disable connect button while connecting
        connectButton.interactable = false;

        Debug.Log($"[Experimenter] Attempting connection to {ip}:{port}");
    }

    public void OnDisconnectClicked()
    {
        if (networkManager != null)
        {
            networkManager.StopClient();
        }

        OnDisconnected();
    }

    public void OnConnected()
    {
        isConnected = true;
        UpdateStatus("Connected!", Color.green);
        ShowObserverPanel();

        // Immediately load the participant's scene additively so the observer
        // camera has actual geometry to render.
        // RoomScene is always the participant's scene in the current build.
        const string participantScene = "RoomScene";

        bool alreadyLoaded = false;
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            if (SceneManager.GetSceneAt(i).name == participantScene)
            {
                alreadyLoaded = true;
                break;
            }
        }

        if (!alreadyLoaded)
        {
            Debug.Log($"[Experimenter] Loading {participantScene} additively for observation.");
            SceneManager.LoadSceneAsync(participantScene, LoadSceneMode.Additive);
        }
        else
        {
            Debug.Log($"[Experimenter] {participantScene} already loaded.");
        }

        Debug.Log("[Experimenter] Successfully connected as observer");
    }

    public void OnDisconnected()
    {
        isConnected = false;
        UpdateStatus("Disconnected from server", Color.red);

        // Switch UI
        ShowConnectionPanel();

        connectButton.interactable = true;

        Debug.Log("[Experimenter] Disconnected from server");
    }

    void UpdateObserverInfo()
    {
        // Find the networked player
        var players = FindObjectsOfType<NetworkedPlayer>();

        foreach (var player in players)
        {
            // Skip experimenter controllers
            if (player.GetComponent<ExperimenterController>() != null)
                continue;

            // Update UI with player info
            if (participantIDText != null)
                participantIDText.text = $"Participant: {player.participantID}";

            if (sceneNameText != null)
                sceneNameText.text = $"Scene: {player.currentScene}";

            if (modeText != null)
                modeText.text = $"Mode: {(player.isVRMode ? "VR" : "Desktop")}";

            return;
        }

        // No player found
        if (participantIDText != null)
            participantIDText.text = "Participant: Waiting...";
    }

    void ShowConnectionPanel()
    {
        if (connectionPanel != null)
            connectionPanel.SetActive(true);

        if (observerPanel != null)
            observerPanel.SetActive(false);
    }

    void ShowObserverPanel()
    {
        if (connectionPanel != null)
            connectionPanel.SetActive(false);

        if (observerPanel != null)
            observerPanel.SetActive(true);
    }

    void UpdateStatus(string message, Color color)
    {
        if (statusText != null)
        {
            statusText.text = message;
            statusText.color = color;
        }
        Debug.Log($"[Experimenter] {message}");
    }
}

