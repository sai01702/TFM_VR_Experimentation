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

                Debug.Log($"[ObserverConnectionUI] Joining relay: {connectionAddress}");
                bool success = await RelayConnectionManager.Instance.JoinWithRelay(connectionAddress);
                
                if (!success)
                {
                    Debug.LogError("[ObserverConnectionUI] Relay join FAILED");
                    UpdateStatusText("Failed to join relay - check code");
                    connectButton.interactable = true;
                    return;
                }
                
                Debug.Log("[ObserverConnectionUI] ✅ Relay joined! Waiting 2s for transport...");
                UpdateStatusText("Relay joined, connecting...");
                await System.Threading.Tasks.Task.Delay(2000);
            }
            else
            {
                if (!ParseConnectionAddress(connectionAddress, out string ipAddress, out int port))
                {
                    UpdateStatusText("Invalid format");
                    connectButton.interactable = true;
                    return;
                }

                var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
                if (transport != null)
                {
                    transport.SetConnectionData(ipAddress, (ushort)port);
                }
            }

            // Disable scene management - observer stays in ExperimenterClientScene
            var config = NetworkManager.Singleton.NetworkConfig;
            config.EnableSceneManagement = false;
            
            Debug.Log("[ObserverConnectionUI] Starting client NOW...");
            bool started = NetworkManager.Singleton.StartClient();
            
            if (!started)
            {
                Debug.LogError("[ObserverConnectionUI] ❌ StartClient FAILED!");
                UpdateStatusText("Failed to start client");
                connectButton.interactable = true;
                return;
            }

            Debug.Log("[ObserverConnectionUI] StartClient succeeded, waiting for connection...");
            UpdateStatusText("Connecting to host...");
            
            // Wait for actual connection with long timeout
            float waitTime = 0f;
            float timeout = 30f;
            
            while (waitTime < timeout)
            {
                await System.Threading.Tasks.Task.Delay(500);
                waitTime += 0.5f;
                
                if (NetworkManager.Singleton.IsConnectedClient)
                {
                    Debug.Log($"[ObserverConnectionUI] ✅ CONNECTED after {waitTime:F1}s!");
                    UpdateStatusText("Connected!");
                    return;
                }
                
                if (waitTime > 5f && ((int)waitTime) % 5 == 0)
                {
                    Debug.Log($"[ObserverConnectionUI] Still connecting... {waitTime:F0}s");
                    UpdateStatusText($"Connecting... ({waitTime:F0}s)");
                }
            }
            
            // Timeout
            Debug.LogError($"[ObserverConnectionUI] ❌ TIMEOUT after {timeout}s");
            UpdateStatusText($"Connection timeout - Is host ready?");
            connectButton.interactable = true;
            
            if (NetworkManager.Singleton != null)
            {
                NetworkManager.Singleton.Shutdown();
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
