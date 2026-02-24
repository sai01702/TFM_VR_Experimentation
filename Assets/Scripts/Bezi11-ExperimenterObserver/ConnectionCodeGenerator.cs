using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using TMPro;
using System.Net;
using System.Net.Sockets;

namespace Bezi11.ExperimenterObserver
{
    public class ConnectionCodeGenerator : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI connectionInfoText;
        [SerializeField] private TextMeshProUGUI connectionModeText;
        [SerializeField] private GameObject connectionPanel;

        private const int DefaultPort = 7777;
        private bool isHosting;

        void Start()
        {
            if (connectionPanel != null)
            {
                connectionPanel.SetActive(false);
            }

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

            Debug.Log("[ConnectionCodeGenerator] OnServerStarted called!");
            isHosting = true;

            bool usingRelay = RelayConnectionManager.Instance != null && RelayConnectionManager.Instance.IsUsingRelay;
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
                mode = "Internet (Relay)";
            }
            else
            {
                Debug.Log("[ConnectionCodeGenerator] Using LAN mode");
                string localIP = GetLocalIPAddress();
                int port = GetNetworkPort();
                connectionInfo = $"{localIP}:{port}";
                mode = "LAN (Direct)";
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
