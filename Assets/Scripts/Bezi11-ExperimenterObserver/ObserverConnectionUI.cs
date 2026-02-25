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
        private bool isConnecting = false;
        private bool autoReconnect = true;
        private string lastRelayCode = "";
        private bool wasUsingRelay = false;

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
            isConnecting = true;
            autoReconnect = true; // Enable auto-reconnect
            
            // Store connection info for reconnection
            wasUsingRelay = useRelay;
            if (useRelay)
            {
                lastRelayCode = connectionAddress;
            }

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
                // LAN MODE - Direct IP connection
                if (!ParseConnectionAddress(connectionAddress, out string ipAddress, out int port))
                {
                    UpdateStatusText("Invalid format. Use IP:Port (e.g., 192.168.1.100:7777)");
                    connectButton.interactable = true;
                    return;
                }

                Debug.Log($"[ObserverConnectionUI] LAN mode - Connecting to {ipAddress}:{port}");
                
                var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
                if (transport != null)
                {
                    transport.SetConnectionData(ipAddress, (ushort)port);
                    Debug.Log($"[ObserverConnectionUI] ✅ Transport configured for LAN: {ipAddress}:{port}");
                }
                else
                {
                    Debug.LogError("[ObserverConnectionUI] UnityTransport not found!");
                    UpdateStatusText("Error: Transport not found");
                    connectButton.interactable = true;
                    return;
                }
                
                // Small delay to ensure transport is ready
                await System.Threading.Tasks.Task.Delay(200);
            }

            // CRITICAL: Disable scene management - observer stays in ExperimenterClientScene
            var config = NetworkManager.Singleton.NetworkConfig;
            config.EnableSceneManagement = false;
            
            // CRITICAL: Send "OBSERVER" as connection data so host assigns us ClientId 123+
            config.ConnectionData = System.Text.Encoding.UTF8.GetBytes("OBSERVER");
            
            // CRITICAL: Ensure we're starting as CLIENT not HOST
            if (NetworkManager.Singleton.IsServer || NetworkManager.Singleton.IsHost)
            {
                Debug.LogError("[ObserverConnectionUI] ❌ NetworkManager is already server/host! Shutting down first...");
                NetworkManager.Singleton.Shutdown();
                await System.Threading.Tasks.Task.Delay(500);
            }
            
            Debug.Log("[ObserverConnectionUI] Starting as OBSERVER CLIENT (sending OBSERVER payload)...");
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
            
            // Give Unity a moment to process StartClient
            await System.Threading.Tasks.Task.Delay(100);
            
            // Wait for actual connection - check IsClient not IsConnectedClient
            float waitTime = 0f;
            float timeout = 30f;
            
            while (waitTime < timeout)
            {
                // Check if callback already handled connection
                if (!isConnecting)
                {
                    Debug.Log("[ObserverConnectionUI] Connection confirmed by callback!");
                    return;
                }
                
                // Check if we're actually connected as a client
                if (NetworkManager.Singleton != null && 
                    NetworkManager.Singleton.IsClient && 
                    NetworkManager.Singleton.LocalClientId != ulong.MaxValue)
                {
                    Debug.Log($"[ObserverConnectionUI] ✅ CONNECTED! IsClient: True, LocalClientId: {NetworkManager.Singleton.LocalClientId}");
                    isConnecting = false;
                    UpdateStatusText("Connected!");
                    
                    if (observerPanel != null)
                    {
                        observerPanel.SetActive(true);
                    }
                    
                    if (disconnectButton != null)
                    {
                        disconnectButton.interactable = true;
                    }
                    
                    return;
                }
                
                await System.Threading.Tasks.Task.Delay(500);
                waitTime += 0.5f;
                
                if (waitTime > 5f && ((int)waitTime) % 5 == 0)
                {
                    Debug.Log($"[ObserverConnectionUI] Still connecting... {waitTime:F0}s (IsClient: {NetworkManager.Singleton?.IsClient})");
                    UpdateStatusText($"Connecting... ({waitTime:F0}s)");
                }
            }
            
            // Timeout
            isConnecting = false;
            Debug.LogError($"[ObserverConnectionUI] ❌ TIMEOUT after {timeout}s");
            UpdateStatusText($"Connection timeout");
            connectButton.interactable = true;
            
            if (NetworkManager.Singleton != null)
            {
                NetworkManager.Singleton.Shutdown();
            }
        }

        private void OnDisconnectClicked()
        {
            Debug.Log("[ObserverConnectionUI] Disconnect clicked by user");
            
            // Disable auto-reconnect when user manually disconnects
            autoReconnect = false;
            
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
                Debug.Log("[ObserverConnectionUI] ✅ OnClientConnected callback - We are connected!");
                isConnecting = false;
                
                UpdateStatusText("Connected!");
                
                if (connectButton != null)
                {
                    connectButton.interactable = false;
                }
                
                if (disconnectButton != null)
                {
                    disconnectButton.interactable = true;
                }

                if (observerPanel != null)
                {
                    observerPanel.SetActive(true);
                }
            }
        }

        private void OnClientDisconnected(ulong clientId)
        {
            if (clientId == NetworkManager.Singleton.LocalClientId)
            {
                string reason = NetworkManager.Singleton.DisconnectReason;
                
                Debug.LogWarning($"[ObserverConnectionUI] ⚠️ Disconnected! ClientId: {clientId}");
                Debug.LogWarning($"[ObserverConnectionUI] Reason: {(string.IsNullOrEmpty(reason) ? "Connection interrupted" : reason)}");
                
                // Connection interruptions are NORMAL - auto-reconnect!
                if (autoReconnect)
                {
                    Debug.Log("[ObserverConnectionUI] Auto-reconnecting in 2 seconds...");
                    UpdateStatusText("Connection interrupted - Reconnecting...");
                    StartCoroutine(AttemptReconnect());
                }
                else
                {
                    UpdateStatusText(string.IsNullOrEmpty(reason) ? "Disconnected" : $"Disconnected: {reason}");
                    connectButton.interactable = true;
                    disconnectButton.interactable = false;

                    if (observerPanel != null)
                    {
                        observerPanel.SetActive(false);
                    }
                }
            }
        }
        
        private System.Collections.IEnumerator AttemptReconnect()
        {
            const int maxAttempts = 15; // 15 attempts over ~30 seconds
            int attempt = 0;
            
            while (autoReconnect && attempt < maxAttempts)
            {
                attempt++;
                
                Debug.Log($"[ObserverConnectionUI] Reconnect attempt {attempt}/{maxAttempts}...");
                UpdateStatusText($"Reconnecting... (Attempt {attempt}/{maxAttempts})");
                
                // Wait 2 seconds between attempts
                yield return new WaitForSeconds(2f);
                
                // Shutdown previous connection
                if (NetworkManager.Singleton != null)
                {
                    NetworkManager.Singleton.Shutdown();
                    yield return new WaitForSeconds(0.5f);
                }
                
                // Try to reconnect
                bool success = false;
                
                if (wasUsingRelay && !string.IsNullOrEmpty(lastRelayCode))
                {
                    Debug.Log($"[ObserverConnectionUI] Attempting relay reconnection with code: {lastRelayCode}");
                    
                    if (RelayConnectionManager.Instance != null)
                    {
                        bool relayJoined = false;
                        var joinTask = RelayConnectionManager.Instance.JoinWithRelay(lastRelayCode);
                        
                        // Wait for relay join
                        while (!joinTask.IsCompleted)
                        {
                            yield return null;
                        }
                        
                        relayJoined = joinTask.Result;
                        
                        if (relayJoined)
                        {
                            yield return new WaitForSeconds(0.5f);
                            
                            var config = NetworkManager.Singleton.NetworkConfig;
                            config.EnableSceneManagement = false;
                            config.ConnectionData = System.Text.Encoding.UTF8.GetBytes("OBSERVER");
                            
                            success = NetworkManager.Singleton.StartClient();
                            
                            if (success)
                            {
                                Debug.Log("[ObserverConnectionUI] ✅ Reconnection started, waiting for connection...");
                                
                                // Wait up to 5 seconds to see if we connect
                                float waitTime = 0f;
                                while (waitTime < 5f && !NetworkManager.Singleton.IsClient)
                                {
                                    yield return new WaitForSeconds(0.2f);
                                    waitTime += 0.2f;
                                }
                                
                                if (NetworkManager.Singleton.IsClient)
                                {
                                    Debug.Log("[ObserverConnectionUI] ✅✅✅ RECONNECTED SUCCESSFULLY!");
                                    UpdateStatusText("Reconnected!");
                                    
                                    if (observerPanel != null)
                                    {
                                        observerPanel.SetActive(true);
                                    }
                                    
                                    yield break; // Success! Exit the loop
                                }
                            }
                        }
                    }
                }
                
                Debug.LogWarning($"[ObserverConnectionUI] Attempt {attempt} failed, trying again...");
            }
            
            // All attempts failed
            Debug.LogError("[ObserverConnectionUI] ❌ All reconnection attempts failed after 30 seconds");
            UpdateStatusText("Reconnection failed - Click Connect to retry");
            connectButton.interactable = true;
            disconnectButton.interactable = false;
            autoReconnect = false;
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
