using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

public class AutoHostStarter : MonoBehaviour
{
    void Start()
    {
        if (!NetworkManager.Singleton.IsServer && !NetworkManager.Singleton.IsClient)
        {
            var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();

            transport.ConnectionData.Address = "0.0.0.0";
            transport.ConnectionData.Port = 7777;

            NetworkManager.Singleton.StartHost();

            Debug.Log("LAN Host started automatically.");
        }
    }
}