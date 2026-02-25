using Unity.Netcode;
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

            // Start hosting with Relay if configured
            if (RelayConnectionManager.Instance != null && RelayConnectionManager.Instance.IsUsingRelay)
            {
                Debug.Log("[ConnectionBoardActivator] Starting relay hosting...");
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

            // Start hosting
            bool started = NetworkManager.Singleton.StartHost();

            if (started)
            {
                Debug.Log("[ConnectionBoardActivator] ✅ Hosting started!");
                
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
