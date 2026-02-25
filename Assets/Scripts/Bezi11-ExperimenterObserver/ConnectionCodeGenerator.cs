using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Net;
using System.Net.Sockets;
using System.Collections;

namespace Bezi11.ExperimenterObserver
{
    public class ConnectionCodeGenerator : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI connectionInfoText;
        [SerializeField] private TextMeshProUGUI connectionModeText;
        [SerializeField] private GameObject connectionPanel;
        [SerializeField] private Toggle connectionModeToggle;

        [Header("Mode Labels")]
        [SerializeField] private string lanModeLabel = "LAN (Direct)";
        [SerializeField] private string relayModeLabel = "Internet (Relay)";

        private const int DefaultPort = 7777;
        private bool isHosting;
        private bool useRelayMode;

        void Start()
        {
            Debug.Log("[ConnectionCodeGenerator] Start() called");
            
            if (connectionPanel != null)
            {
                connectionPanel.SetActive(false);
                Debug.Log("[ConnectionCodeGenerator] Set panel inactive");
            }
            else
            {
                Debug.LogError("[ConnectionCodeGenerator] connectionPanel is NULL in Start()!");
            }

            if (connectionModeToggle != null)
            {
                connectionModeToggle.isOn = false;
                useRelayMode = false;
                connectionModeToggle.onValueChanged.AddListener(OnConnectionModeToggled);
                Debug.Log("[ConnectionCodeGenerator] Toggle registered, starting in LAN mode");
            }
            else
            {
                Debug.LogError("[ConnectionCodeGenerator] connectionModeToggle is NULL! Toggle will not work!");
                Debug.LogError("[ConnectionCodeGenerator] Please assign the toggle reference in the Inspector!");
            }

            StartCoroutine(WaitForNetworkManager());
        }

        private IEnumerator WaitForNetworkManager()
        {
            Debug.Log("[ConnectionCodeGenerator] Waiting for NetworkManager.Singleton...");
            
            float timeout = 10f;
            float elapsed = 0f;
            
            while (NetworkManager.Singleton == null && elapsed < timeout)
            {
                yield return new WaitForSeconds(0.1f);
                elapsed += 0.1f;
            }
            
            if (NetworkManager.Singleton == null)
            {
                Debug.LogError("[ConnectionCodeGenerator] NetworkManager.Singleton is still NULL after 10 seconds!");
                yield break;
            }
            
            Debug.Log("[ConnectionCodeGenerator] NetworkManager.Singleton found!");
            RegisterNetworkCallbacks();
        }

        void OnEnable()
        {
            RegisterNetworkCallbacks();
        }

        void OnDisable()
        {
            UnregisterNetworkCallbacks();
        }

        void OnDestroy()
        {
            UnregisterNetworkCallbacks();
            
            if (connectionModeToggle != null)
            {
                connectionModeToggle.onValueChanged.RemoveListener(OnConnectionModeToggled);
            }
        }

        private void OnConnectionModeToggled(bool isRelay)
        {
            Debug.Log($"[ConnectionCodeGenerator] Mode toggled to: {(isRelay ? "Relay" : "LAN")}");
            useRelayMode = isRelay;

            if (isHosting)
            {
                Debug.Log("[ConnectionCodeGenerator] Already hosting, restarting to apply new mode...");
                StartCoroutine(RestartHostingWithNewMode());
            }
        }

        private IEnumerator RestartHostingWithNewMode()
        {
            if (NetworkManager.Singleton == null)
            {
                Debug.LogError("[ConnectionCodeGenerator] NetworkManager.Singleton is NULL during mode switch!");
                yield break;
            }

            Debug.Log("[ConnectionCodeGenerator] Shutting down current host...");
            NetworkManager.Singleton.Shutdown();
            isHosting = false;

            yield return new WaitForSeconds(0.5f);

            Debug.Log("[ConnectionCodeGenerator] Restarting host with new mode...");
            
            if (useRelayMode && RelayConnectionManager.Instance != null)
            {
                RelayConnectionManager.Instance.SetConnectionMode(true);
                StartCoroutine(RestartWithRelay());
            }
            else
            {
                if (RelayConnectionManager.Instance != null)
                {
                    RelayConnectionManager.Instance.SetConnectionMode(false);
                }
                
                bool started = NetworkManager.Singleton.StartHost();
                if (started)
                {
                    OnServerStarted();
                }
            }
        }

        private IEnumerator RestartWithRelay()
        {
            // Show "Creating code..." message
            if (connectionInfoText != null)
            {
                connectionInfoText.text = "Creating code...";
                Debug.Log("[ConnectionCodeGenerator] Showing 'Creating code...' message");
            }
            
            if (connectionModeText != null)
            {
                connectionModeText.text = relayModeLabel;
            }

            var relayTask = RelayConnectionManager.Instance.StartHostWithRelay();
            
            while (!relayTask.IsCompleted)
            {
                yield return null;
            }
            
            string joinCode = relayTask.Result;
            
            if (!string.IsNullOrEmpty(joinCode))
            {
                Debug.Log($"[ConnectionCodeGenerator] Relay join code received: {joinCode}");
                yield return new WaitForSeconds(0.1f); // Small delay to ensure transport is configured
                
                bool started = NetworkManager.Singleton.StartHost();
                Debug.Log($"[ConnectionCodeGenerator] StartHost() returned: {started}");
                
                if (started)
                {
                    // Manually call OnServerStarted since we're restarting
                    yield return new WaitForSeconds(0.1f);
                    OnServerStarted();
                }
                else
                {
                    Debug.LogError("[ConnectionCodeGenerator] Failed to start host with relay!");
                    
                    // Show error in UI
                    if (connectionInfoText != null)
                    {
                        connectionInfoText.text = "Failed to start relay";
                    }
                }
            }
            else
            {
                Debug.LogError("[ConnectionCodeGenerator] Failed to get relay join code!");
                
                // Fall back to LAN mode
                Debug.Log("[ConnectionCodeGenerator] Falling back to LAN mode...");
                useRelayMode = false;
                if (connectionModeToggle != null)
                {
                    connectionModeToggle.isOn = false;
                }
                
                if (RelayConnectionManager.Instance != null)
                {
                    RelayConnectionManager.Instance.SetConnectionMode(false);
                }
                
                bool started = NetworkManager.Singleton.StartHost();
                if (started)
                {
                    yield return new WaitForSeconds(0.1f);
                    OnServerStarted();
                }
            }
        }

        private void RegisterNetworkCallbacks()
        {
            if (NetworkManager.Singleton != null && !isHosting)
            {
                NetworkManager.Singleton.OnServerStarted -= OnServerStarted;
                NetworkManager.Singleton.OnServerStarted += OnServerStarted;
                
                if (NetworkManager.Singleton.IsServer || NetworkManager.Singleton.IsHost)
                {
                    OnServerStarted();
                }
            }
        }

        private void UnregisterNetworkCallbacks()
        {
            if (NetworkManager.Singleton != null)
            {
                NetworkManager.Singleton.OnServerStarted -= OnServerStarted;
            }
        }

        private async void OnServerStarted()
        {
            if (isHosting)
            {
                Debug.Log("[ConnectionCodeGenerator] Already hosting, skipping...");
                return;
            }

            Debug.Log("[ConnectionCodeGenerator] OnServerStarted called! Waiting for full initialization...");
            
            // CRITICAL: Wait for NetworkBootstrap to finish spawning NetworkSessionManager
            await System.Threading.Tasks.Task.Delay(2000);
            
            isHosting = true;
            Debug.Log("[ConnectionCodeGenerator] Server initialization wait complete, showing connection info");

            bool usingRelay = useRelayMode && RelayConnectionManager.Instance != null && RelayConnectionManager.Instance.IsUsingRelay;
            string connectionInfo;
            string mode;

            if (usingRelay)
            {
                Debug.Log("[ConnectionCodeGenerator] Using Relay mode");
                string joinCode = RelayConnectionManager.Instance.JoinCode;
                
                if (string.IsNullOrEmpty(joinCode))
                {
                    joinCode = await RelayConnectionManager.Instance.StartHostWithRelay();
                }

                connectionInfo = joinCode ?? "Relay Error";
                mode = relayModeLabel;
            }
            else
            {
                Debug.Log("[ConnectionCodeGenerator] Using LAN mode");
                string localIP = GetLocalIPAddress();
                int port = GetNetworkPort();
                connectionInfo = $"{localIP}:{port}";
                mode = lanModeLabel;
            }

            Debug.Log($"[ConnectionCodeGenerator] Connection Info: {connectionInfo}");
            Debug.Log($"[ConnectionCodeGenerator] Mode: {mode}");

            if (connectionInfoText != null)
            {
                connectionInfoText.text = connectionInfo;
                Debug.Log("[ConnectionCodeGenerator] Set connectionInfoText");
            }
            else
            {
                Debug.LogWarning("[ConnectionCodeGenerator] connectionInfoText is NULL!");
            }

            if (connectionModeText != null)
            {
                connectionModeText.text = mode;
                Debug.Log("[ConnectionCodeGenerator] Set connectionModeText");
            }
            else
            {
                Debug.LogWarning("[ConnectionCodeGenerator] connectionModeText is NULL!");
            }

            if (connectionPanel != null)
            {
                connectionPanel.SetActive(true);
                Debug.Log("[ConnectionCodeGenerator] Activated connectionPanel");
            }
            else
            {
                Debug.LogWarning("[ConnectionCodeGenerator] connectionPanel is NULL!");
            }

            Debug.Log($"[ConnectionCodeGenerator] Hosting - Mode: {mode}, Connection: {connectionInfo}");

            if (ParticipantSession.Instance != null)
            {
                ParticipantSession.Instance.AppendLog($"[Network] Started hosting - Mode: {mode}, Connection: {connectionInfo}");
            }
        }

        private string GetLocalIPAddress()
        {
            try
            {
                string localIP = "127.0.0.1";
                var host = Dns.GetHostEntry(Dns.GetHostName());

                foreach (var ip in host.AddressList)
                {
                    if (ip.AddressFamily == AddressFamily.InterNetwork)
                    {
                        localIP = ip.ToString();
                        break;
                    }
                }

                return localIP;
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[ConnectionCodeGenerator] Failed to get local IP: {e.Message}");
                return "127.0.0.1";
            }
        }

        private int GetNetworkPort()
        {
            if (NetworkManager.Singleton != null)
            {
                var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
                if (transport != null)
                {
                    return transport.ConnectionData.Port;
                }
            }

            return DefaultPort;
        }
    }
}
