using UnityEngine;
using Unity.Netcode;

public class PersistentClientCanvas : MonoBehaviour
{
    void Awake()
    {
        // Keep the UI alive across scene loads
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        // Ensure the canvas is always enabled
        gameObject.SetActive(true);

        // Listen for connection events
        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
    }

    void OnClientConnected(ulong clientId)
    {
        // If this machine is the client, ensure UI stays visible
        if (clientId == NetworkManager.Singleton.LocalClientId)
        {
            gameObject.SetActive(true);
        }
    }

    void OnDestroy()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
        }
    }
}