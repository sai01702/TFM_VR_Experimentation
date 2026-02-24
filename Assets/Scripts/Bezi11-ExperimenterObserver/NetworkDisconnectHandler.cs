using Unity.Netcode;
using UnityEngine;

namespace Bezi11.ExperimenterObserver
{
    public class NetworkDisconnectHandler : MonoBehaviour
    {
        void Start()
        {
            if (NetworkManager.Singleton != null)
            {
                NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
                NetworkManager.Singleton.OnServerStopped += OnServerStopped;
            }
        }

        void OnDestroy()
        {
            if (NetworkManager.Singleton != null)
            {
                NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
                NetworkManager.Singleton.OnServerStopped -= OnServerStopped;
            }
        }

        private void OnClientDisconnected(ulong clientId)
        {
            Debug.Log($"[NetworkDisconnectHandler] Client {clientId} disconnected");

            if (ParticipantSession.Instance != null)
            {
                ParticipantSession.Instance.AppendLog($"[Network] Client {clientId} disconnected");
            }

            if (NetworkManager.Singleton.IsServer && clientId != NetworkManager.ServerClientId)
            {
                Debug.Log("[NetworkDisconnectHandler] Observer disconnected");
            }
        }

        private void OnServerStopped(bool wasHost)
        {
            Debug.Log("[NetworkDisconnectHandler] Server stopped");

            if (ParticipantSession.Instance != null)
            {
                ParticipantSession.Instance.AppendLog("[Network] Server stopped");
            }
        }
    }
}
