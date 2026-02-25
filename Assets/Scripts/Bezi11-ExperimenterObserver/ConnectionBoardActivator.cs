using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using TMPro;

namespace Bezi11.ExperimenterObserver
{
    /// <summary>
    /// Activates network hosting when player steps on the trigger.
    /// Place this on a 3D object with a trigger collider in RoomScene.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class ConnectionBoardActivator : MonoBehaviour
    {
        [Header("UI")]
        [SerializeField] private GameObject connectionPanel;
        [SerializeField] private TextMeshProUGUI statusText;
        
        [Header("Settings")]
        [SerializeField] private string playerTag = "Player";
        [SerializeField] private bool activateOnce = true;
        
        private bool isActivated = false;

        void Start()
        {
            if (connectionPanel != null)
            {
                connectionPanel.SetActive(false);
            }
            
            // Ensure this object has a trigger collider
            Collider col = GetComponent<Collider>();
            if (col != null)
            {
                col.isTrigger = true;
                Debug.Log("[ConnectionBoardActivator] Trigger collider ready. Player must step on this to start hosting.");
            }
            else
            {
                Debug.LogError("[ConnectionBoardActivator] No collider found! Add a Box Collider and set it as trigger.");
            }
            
            if (statusText != null)
            {
                statusText.text = "Step here to start observer connection";
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (isActivated && activateOnce) return;
            
            // Check if player entered
            if (other.CompareTag(playerTag))
            {
                Debug.Log("[ConnectionBoardActivator] ✅ Player stepped on trigger!");
                ActivateHosting();
            }
        }

        private async void ActivateHosting()
        {
            if (isActivated && activateOnce)
            {
                Debug.Log("[ConnectionBoardActivator] Already activated, ignoring.");
                return;
            }
            
            isActivated = true;
            
            if (statusText != null)
            {
                statusText.text = "Starting hosting...";
            }

            Debug.Log("[ConnectionBoardActivator] ✅ Player activated! Starting hosting NOW...");

            if (NetworkManager.Singleton == null)
            {
                Debug.LogError("[ConnectionBoardActivator] NetworkManager.Singleton is null!");
                if (statusText != null)
                {
                    statusText.text = "Error: NetworkManager not found";
                }
                return;
            }

            Debug.Log($"[ConnectionBoardActivator] ✅ Player activated! Starting hosting NOW...");

            // Check if using Relay or LAN
            bool usingRelay = RelayConnectionManager.Instance != null && RelayConnectionManager.Instance.IsUsingRelay;
            
            if (usingRelay)
            {
                Debug.Log("[ConnectionBoardActivator] Using RELAY mode");
                string joinCode = await RelayConnectionManager.Instance.StartHostWithRelay();
                if (string.IsNullOrEmpty(joinCode))
                {
                    Debug.LogError("[ConnectionBoardActivator] Failed to start relay hosting");
                    if (statusText != null)
                    {
                        statusText.text = "Failed to start relay";
                    }
                    return;
                }
                Debug.Log($"[ConnectionBoardActivator] Relay code: {joinCode}");
            }
            else
            {
                Debug.Log("[ConnectionBoardActivator] Using LAN mode - Direct IP connection on port 7777");
                
                // For LAN, configure transport for local network
                var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
                if (transport != null)
                {
                    // Set to listen on all interfaces (0.0.0.0) on port 7777
                    transport.ConnectionData.Address = "0.0.0.0";
                    transport.ConnectionData.Port = 7777;
                    transport.ConnectionData.ServerListenAddress = "0.0.0.0";
                    Debug.Log("[ConnectionBoardActivator] ✅ Transport configured for LAN: 0.0.0.0:7777");
                }
            }

            // Start hosting
            bool started = NetworkManager.Singleton.StartHost();

            if (started)
            {
                Debug.Log("[ConnectionBoardActivator] ✅ Hosting started!");
                
                // Enable connection approval to properly handle observer connections
                NetworkManager.Singleton.ConnectionApprovalCallback = ApproveObserverConnection;
                NetworkManager.Singleton.NetworkConfig.ConnectionApproval = true;
                
                Debug.Log("[ConnectionBoardActivator] Connection approval enabled for observers");
                
                // Register disconnect callback to see why observers disconnect
                NetworkManager.Singleton.OnClientDisconnectCallback += OnObserverDisconnected;
                
                // Wait a moment for initialization
                await System.Threading.Tasks.Task.Delay(500);
                
                // Spawn NetworkSessionManager
                SpawnNetworkSessionManager();
                
                await System.Threading.Tasks.Task.Delay(500);
                
                // Show connection panel
                if (connectionPanel != null)
                {
                    connectionPanel.SetActive(true);
                    Debug.Log("[ConnectionBoardActivator] Connection panel activated!");
                    
                    // Notify ConnectionCodeGenerator to show the code
                    var codeGenerator = FindObjectOfType<ConnectionCodeGenerator>();
                    if (codeGenerator != null)
                    {
                        codeGenerator.ActivateAndShowCode();
                        Debug.Log("[ConnectionBoardActivator] ConnectionCodeGenerator activated!");
                    }
                    else
                    {
                        Debug.LogWarning("[ConnectionBoardActivator] ConnectionCodeGenerator not found in scene");
                    }
                }
                
                if (statusText != null)
                {
                    statusText.text = "Hosting active! See panel for code.";
                }
                
                Debug.Log("[ConnectionBoardActivator] ✅✅✅ HOSTING COMPLETE! Observer can connect now!");
                
                if (ParticipantSession.Instance != null)
                {
                    ParticipantSession.Instance.AppendLog("[Network] Observer hosting started via connection board");
                }
            }
            else
            {
                Debug.LogError("[ConnectionBoardActivator] StartHost() failed!");
                if (statusText != null)
                {
                    statusText.text = "Failed to start hosting";
                }
            }
        }
        
        private void OnObserverDisconnected(ulong clientId)
        {
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
            {
                // Only log if this is not the server itself
                if (clientId != NetworkManager.ServerClientId)
                {
                    Debug.LogError($"[ConnectionBoardActivator] ❌ Observer disconnected! ClientId: {clientId}");
                    Debug.LogError($"[ConnectionBoardActivator] Server is still running: {NetworkManager.Singleton.IsServer}");
                    
                    // Count actual clients (excluding server)
                    int actualClients = 0;
                    foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
                    {
                        if (client.ClientId != NetworkManager.ServerClientId)
                        {
                            actualClients++;
                            Debug.LogError($"[ConnectionBoardActivator] Still connected: ClientId {client.ClientId}");
                        }
                    }
                    Debug.LogError($"[ConnectionBoardActivator] Remaining observers: {actualClients}");
                }
            }
        }
        
        private void ApproveObserverConnection(
            Unity.Netcode.NetworkManager.ConnectionApprovalRequest request, 
            Unity.Netcode.NetworkManager.ConnectionApprovalResponse response)
        {
            // ALWAYS APPROVE - We always allow observers to stream!
            response.Approved = true;
            response.CreatePlayerObject = false;
            response.PlayerPrefabHash = null;
            response.Position = null;
            response.Rotation = null;
            response.Pending = false;
            
            Debug.Log($"[ConnectionBoardActivator] ✅ Experimenter client connection AUTO-APPROVED!");
        }

        private void SpawnNetworkSessionManager()
        {
            // Find the NetworkBootstrap and use its prefab reference
            var bootstrap = FindObjectOfType<NetworkBootstrap>();
            if (bootstrap == null)
            {
                Debug.LogWarning("[ConnectionBoardActivator] NetworkBootstrap not found, cannot spawn NetworkSessionManager");
                return;
            }

            // Access the public prefab field
            var prefabField = bootstrap.GetType().GetField("networkSessionManagerPrefab", 
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            
            if (prefabField != null)
            {
                GameObject prefab = prefabField.GetValue(bootstrap) as GameObject;
                if (prefab != null)
                {
                    GameObject instance = Instantiate(prefab);
                    NetworkObject netObj = instance.GetComponent<NetworkObject>();
                    if (netObj != null)
                    {
                        netObj.Spawn();
                        Debug.Log("[ConnectionBoardActivator] NetworkSessionManager spawned!");
                        
                        var sessionMgr = instance.GetComponent<NetworkSessionManager>();
                        if (sessionMgr != null)
                        {
                            sessionMgr.UpdateSessionData();
                        }
                    }
                }
            }
        }
    }
}
