using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Bezi11.ExperimenterObserver
{
    public class ObserverConnectionUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private TMP_InputField connectionInputField;
        [SerializeField] private Button connectButton;
        [SerializeField] private Button disconnectButton;
        [SerializeField] private TextMeshProUGUI statusText;
        [SerializeField] private GameObject observerPanel;
        [SerializeField] private Toggle useRelayToggle;

        private const int DefaultPort = 7777;

        void Start()
        {
            if (connectButton != null)
            {
                connectButton.onClick.AddListener(OnConnectClicked);
            }

            if (disconnectButton != null)
            {
                disconnectButton.onClick.AddListener(OnDisconnectClicked);
                disconnectButton.interactable = false;
            }

            if (observerPanel != null)
            {
                observerPanel.SetActive(false);
            }

            if (useRelayToggle != null)
            {
                useRelayToggle.onValueChanged.AddListener(OnRelayToggleChanged);
                UpdateConnectionInputPlaceholder();
            }

            UpdateStatusText("Enter connection address");

            if (NetworkManager.Singleton != null)
            {
                NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
                NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
                
                // Add connection timeout monitoring
                NetworkManager.Singleton.OnTransportFailure += OnTransportFailure;
                
                Debug.Log("[ObserverConnectionUI] Registered network callbacks");
            }
        }

        void OnDestroy()
        {
            if (connectButton != null)
            {
                connectButton.onClick.RemoveListener(OnConnectClicked);
            }

            if (disconnectButton != null)
            {
                disconnectButton.onClick.RemoveListener(OnDisconnectClicked);
            }

            if (NetworkManager.Singleton != null)
            {
                NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
                NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
                NetworkManager.Singleton.OnTransportFailure -= OnTransportFailure;
            }

            if (useRelayToggle != null)
            {
                useRelayToggle.onValueChanged.RemoveListener(OnRelayToggleChanged);
            }
        }

        private void OnTransportFailure()
        {
            Debug.LogError("[ObserverConnectionUI] Transport failure detected");
            UpdateStatusText("Connection failed - Transport error");
            connectButton.interactable = true;
            disconnectButton.interactable = false;
            
            if (observerPanel != null)
            {
                observerPanel.SetActive(false);
            }
        }

        private void OnRelayToggleChanged(bool useRelay)
        {
            if (RelayConnectionManager.Instance != null)
            {
                RelayConnectionManager.Instance.SetConnectionMode(useRelay);
            }

            UpdateConnectionInputPlaceholder();
        }

        private void UpdateConnectionInputPlaceholder()
        {
            if (connectionInputField == null) return;

            bool useRelay = useRelayToggle != null && useRelayToggle.isOn;
            var placeholder = connectionInputField.placeholder as TextMeshProUGUI;

            if (placeholder != null)
            {
                placeholder.text = useRelay 
                    ? "Enter Join Code (e.g., ABC123)" 
                    : "Enter IP:Port (e.g., 192.168.1.100:7777)";
            }
        }

        private async void OnConnectClicked()
        {
            if (connectionInputField == null || string.IsNullOrWhiteSpace(connectionInputField.text))
            {
                UpdateStatusText("Please enter connection address");
                return;
            }

            string connectionAddress = connectionInputField.text.Trim();
            bool useRelay = useRelayToggle != null && useRelayToggle.isOn;

            if (NetworkManager.Singleton == null)
            {
                UpdateStatusText("NetworkManager not found!");
                return;
            }

            UpdateStatusText("Connecting...");
            connectButton.interactable = false;

            if (useRelay)
            {
                if (RelayConnectionManager.Instance == null)
                {
                    UpdateStatusText("RelayConnectionManager not found!");
                    connectButton.interactable = true;
                    return;
                }

                Debug.Log($"[ObserverConnectionUI] Attempting to join relay with code: {connectionAddress}");
                bool success = await RelayConnectionManager.Instance.JoinWithRelay(connectionAddress);
                
                if (!success)
                {
                    Debug.LogError("[ObserverConnectionUI] Failed to join relay");
                    UpdateStatusText("Failed to join relay. Check join code.");
                    connectButton.interactable = true;
                    return;
                }
                
                Debug.Log("[ObserverConnectionUI] Successfully joined relay, transport configured");
                
                // CRITICAL: Wait longer for relay connection to be ready
                UpdateStatusText("Relay joined, waiting for host...");
                await System.Threading.Tasks.Task.Delay(1000);
            }
            else
            {
                if (!ParseConnectionAddress(connectionAddress, out string ipAddress, out int port))
                {
                    UpdateStatusText("Invalid format. Use IP:Port (e.g., 192.168.1.100:7777)");
                    connectButton.interactable = true;
                    return;
                }

                var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
                if (transport != null)
                {
                    transport.SetConnectionData(ipAddress, (ushort)port);
                    Debug.Log($"[ObserverConnectionUI] Transport configured for {ipAddress}:{port}");
                }
            }

            // CRITICAL: Disable scene management for observer client
            // Observer STAYS in ExperimenterClientScene and does NOT load host's scenes
            var config = NetworkManager.Singleton.NetworkConfig;
            config.EnableSceneManagement = false;
            
            Debug.Log("[ObserverConnectionUI] Starting as OBSERVER client (scene sync DISABLED - staying in ExperimenterClientScene)");
            
            UpdateStatusText("Connecting to host...");
            bool started = NetworkManager.Singleton.StartClient();
            
            if (!started)
            {
                Debug.LogError("[ObserverConnectionUI] Failed to start client");
                UpdateStatusText("Failed to start network client");
                connectButton.interactable = true;
            }
            else
            {
                Debug.Log("[ObserverConnectionUI] Client started successfully, waiting for connection...");
                
                // CRITICAL: Wait for actual connection with timeout
                float waitTime = 0f;
                float timeout = 15f;
                
                while (waitTime < timeout && !NetworkManager.Singleton.IsConnectedClient)
                {
                    await System.Threading.Tasks.Task.Delay(200);
                    waitTime += 0.2f;
                    
                    if (waitTime > 5f && waitTime < 5.5f)
                    {
                        UpdateStatusText($"Still connecting... ({(int)waitTime}s)");
                    }
                }
                
                if (NetworkManager.Singleton.IsConnectedClient)
                {
                    Debug.Log("[ObserverConnectionUI] ✅ Connection verified!");
                    UpdateStatusText("Connected!");
                }
                else
                {
                    Debug.LogError($"[ObserverConnectionUI] ❌ Connection timeout after {waitTime}s");
                    UpdateStatusText("Connection timeout - host not ready?");
                    connectButton.interactable = true;
                    
                    // Clean up
                    if (NetworkManager.Singleton != null)
                    {
                        NetworkManager.Singleton.Shutdown();
                    }
                }
            }
        }

        private void OnDisconnectClicked()
        {
            if (NetworkManager.Singleton != null)
            {
                NetworkManager.Singleton.Shutdown();
            }

            UpdateStatusText("Disconnected");
            connectButton.interactable = true;
            disconnectButton.interactable = false;

            if (observerPanel != null)
            {
                observerPanel.SetActive(false);
            }
        }

        private void OnClientConnected(ulong clientId)
        {
            if (clientId == NetworkManager.Singleton.LocalClientId)
            {
                UpdateStatusText("Connected!");
                disconnectButton.interactable = true;

                if (observerPanel != null)
                {
                    observerPanel.SetActive(true);
                }

                Debug.Log("[ObserverConnectionUI] Successfully connected to host");
            }
        }

        private void OnClientDisconnected(ulong clientId)
        {
            if (clientId == NetworkManager.Singleton.LocalClientId)
            {
                string reason = NetworkManager.Singleton.DisconnectReason;
                if (!string.IsNullOrEmpty(reason))
                {
                    Debug.LogWarning($"[ObserverConnectionUI] Disconnected from host. Reason: {reason}");
                    UpdateStatusText($"Disconnected: {reason}");
                }
                else
                {
                    Debug.LogWarning("[ObserverConnectionUI] Disconnected from host (no reason provided)");
                    UpdateStatusText("Connection lost");
                }
                
                connectButton.interactable = true;
                disconnectButton.interactable = false;

                if (observerPanel != null)
                {
                    observerPanel.SetActive(false);
                }
            }
        }

        private bool ParseConnectionAddress(string address, out string ipAddress, out int port)
        {
            ipAddress = string.Empty;
            port = DefaultPort;

            if (string.IsNullOrWhiteSpace(address))
            {
                return false;
            }

            string[] parts = address.Split(':');

            if (parts.Length == 1)
            {
                ipAddress = parts[0].Trim();
                return !string.IsNullOrWhiteSpace(ipAddress);
            }
            else if (parts.Length == 2)
            {
                ipAddress = parts[0].Trim();
                if (int.TryParse(parts[1].Trim(), out port))
                {
                    return !string.IsNullOrWhiteSpace(ipAddress) && port > 0 && port <= 65535;
                }
            }

            return false;
        }

        private void UpdateStatusText(string message)
        {
            if (statusText != null)
            {
                statusText.text = message;
            }
        }
    }
}
