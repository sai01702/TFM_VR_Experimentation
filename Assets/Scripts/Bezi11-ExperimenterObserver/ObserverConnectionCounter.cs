using Unity.Netcode;
using UnityEngine;
using TMPro;

namespace Bezi11
{
    /// <summary>
    /// Displays the number of connected observer clients on the host.
    /// Shows "Observers: 0" when no one is watching, "Observers: 2" when 2 people are watching, etc.
    /// </summary>
    public class ObserverConnectionCounter : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI counterText;
        
        [Header("Display Settings")]
        [SerializeField] private string textFormat = "Observers: {0}";
        [SerializeField] private bool showOnlyWhenHosting = true;

        private int observerCount = 0;

        private void Start()
        {
            if (NetworkManager.Singleton != null)
            {
                NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
                NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
                NetworkManager.Singleton.OnServerStarted += OnServerStarted;
            }

            UpdateDisplay();
        }

        private void OnDestroy()
        {
            if (NetworkManager.Singleton != null)
            {
                NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
                NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
                NetworkManager.Singleton.OnServerStarted -= OnServerStarted;
            }
        }

        private void OnServerStarted()
        {
            observerCount = 0;
            UpdateDisplay();
            Debug.Log("[ObserverConnectionCounter] Server started, observer count reset to 0");
        }

        private void OnClientConnected(ulong clientId)
        {
            Debug.Log($"[ObserverConnectionCounter] OnClientConnected fired! ClientId: {clientId}, IsServer: {NetworkManager.Singleton.IsServer}, ServerClientId: {NetworkManager.ServerClientId}");
            
            if (!NetworkManager.Singleton.IsServer)
            {
                Debug.Log("[ObserverConnectionCounter] Not server, ignoring connection");
                return;
            }

            // Server's own client ID is 0, others are observers
            if (clientId != NetworkManager.ServerClientId)
            {
                observerCount++;
                UpdateDisplay();
                Debug.Log($"[ObserverConnectionCounter] ✅ Observer {clientId} connected! Total observers: {observerCount}");
            }
            else
            {
                Debug.Log($"[ObserverConnectionCounter] Server itself connected (ClientId: {clientId}), not counting as observer");
            }
        }

        private void OnClientDisconnected(ulong clientId)
        {
            if (!NetworkManager.Singleton.IsServer) return;

            // Don't count the server disconnecting itself
            if (clientId != NetworkManager.ServerClientId)
            {
                observerCount = Mathf.Max(0, observerCount - 1);
                UpdateDisplay();
                Debug.Log($"[ObserverConnectionCounter] Observer disconnected. Total observers: {observerCount}");
            }
        }

        private void UpdateDisplay()
        {
            if (counterText == null) return;

            // Only show if hosting (server) or always show based on settings
            bool shouldShow = !showOnlyWhenHosting || 
                             (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer);

            if (shouldShow)
            {
                counterText.text = string.Format(textFormat, observerCount);
                counterText.gameObject.SetActive(true);
            }
            else
            {
                counterText.gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// Get the current number of connected observers.
        /// </summary>
        public int GetObserverCount()
        {
            return observerCount;
        }
    }
}
