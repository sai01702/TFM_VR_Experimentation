using Unity.Netcode;
using UnityEngine;

public class SpectatorCanvasController : MonoBehaviour
{
    void Start()
    {
        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;

        gameObject.SetActive(false);
    }

    void OnClientConnected(ulong id)
    {
        if (id != NetworkManager.Singleton.LocalClientId)
            return;

        if (!NetworkManager.Singleton.IsHost)
            gameObject.SetActive(true);
    }
}