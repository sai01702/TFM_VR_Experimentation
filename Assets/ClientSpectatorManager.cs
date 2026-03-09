using Unity.Netcode;
using UnityEngine;

public class ClientSpectatorManager : MonoBehaviour
{
    public GameObject spectatorCameraPrefab;

    void Start()
    {
        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
    }

    void OnClientConnected(ulong clientId)
    {
        // Only run on the client machine
        if (NetworkManager.Singleton.IsHost)
            return;

        // Ensure it's the local client
        if (clientId == NetworkManager.Singleton.LocalClientId)
        {
            Instantiate(spectatorCameraPrefab);
        }
    }
}