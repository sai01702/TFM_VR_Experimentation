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
            }

            if (useRelayToggle != null)
            {
                useRelayToggle.onValueChanged.RemoveListener(OnRelayToggleChanged);
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

                bool success = await RelayConnectionManager.Instance.JoinWithRelay(connectionAddress);
                if (!success)
                {
                    UpdateStatusText("Failed to join relay. Check join code.");
                    connectButton.interactable = true;
                    return;
                }
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
                }
            }

            NetworkManager.Singleton.StartClient();
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
                UpdateStatusText("Connection lost");
                connectButton.interactable = true;
                disconnectButton.interactable = false;

                if (observerPanel != null)
                {
                    observerPanel.SetActive(false);
                }

                Debug.Log("[ObserverConnectionUI] Disconnected from host");
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
