using Unity.Netcode;
using UnityEngine;

namespace Bezi11.ExperimenterObserver
{
    /// <summary>
    /// Diagnostic script to log network state and help debug connection issues.
    /// Attach to a GameObject in the scene.
    /// </summary>
    public class NetworkDiagnostics : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private float logInterval = 2f;
        
        private float nextLogTime;

        void Update()
        {
            if (Time.time < nextLogTime) return;
            nextLogTime = Time.time + logInterval;

            if (NetworkManager.Singleton == null)
            {
                Debug.Log("[NetworkDiagnostics] NetworkManager.Singleton is NULL");
                return;
            }

            var nm = NetworkManager.Singleton;
            Debug.Log($"[NetworkDiagnostics] IsServer: {nm.IsServer}, IsClient: {nm.IsClient}, IsHost: {nm.IsHost}, ConnectedClients: {nm.ConnectedClientsIds.Count}");

            if (nm.IsServer)
            {
                Debug.Log($"[NetworkDiagnostics] SERVER - Connected client IDs: {string.Join(", ", nm.ConnectedClientsIds)}");
                
                // Check for PlayerCameraStreamer
                var streamers = FindObjectsOfType<PlayerCameraStreamer>();
                Debug.Log($"[NetworkDiagnostics] Found {streamers.Length} PlayerCameraStreamer(s) in scene");
            }

            if (nm.IsClient && !nm.IsServer)
            {
                Debug.Log($"[NetworkDiagnostics] CLIENT - LocalClientId: {nm.LocalClientId}");
                
                // Check for ObserverCameraDisplay
                var displays = FindObjectsOfType<ObserverCameraDisplay>();
                Debug.Log($"[NetworkDiagnostics] Found {displays.Length} ObserverCameraDisplay(s) in scene");
            }

            // Check NetworkSessionManager
            if (NetworkSessionManager.Instance != null)
            {
                Debug.Log($"[NetworkDiagnostics] NetworkSessionManager EXISTS - Participant: {NetworkSessionManager.Instance.ParticipantId}, Scene: {NetworkSessionManager.Instance.CurrentScene}");
            }
            else
            {
                Debug.Log("[NetworkDiagnostics] NetworkSessionManager.Instance is NULL");
            }
        }
    }
}
