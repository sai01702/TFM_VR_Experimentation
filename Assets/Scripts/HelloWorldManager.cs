using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using System.Net;
using System.Net.Sockets;

[RequireComponent(typeof(NetworkManager))]
public class HelloWorldManager : MonoBehaviour
{
    private NetworkManager networkManager;
    private UnityTransport transport;

    private ushort port = 7777;
    private string ipAddress = "Detecting...";

    private void Awake()
    {
        networkManager = GetComponent<NetworkManager>();
        transport = networkManager.GetComponent<UnityTransport>();

        ipAddress = GetLocalIPAddress();
    }

    private void Start()
    {
        // Start hosting automatically
        if (!networkManager.IsClient && !networkManager.IsServer)
        {
            transport.ConnectionData.Address = "0.0.0.0";
            transport.ConnectionData.Port = port;

            networkManager.StartHost();

            Debug.Log($"LAN Host started at {ipAddress}:{port}");
        }
    }

    private void OnGUI()
    {
        GUILayout.BeginArea(new Rect(10, 10, 320, 120));

        GUILayout.Label("=== LAN Multiplayer ===");
        GUILayout.Space(10);

        GUILayout.Label($"IP Address: {ipAddress}:{port}");

        GUILayout.Space(10);

        string mode =
            networkManager.IsHost ? "Host" :
            networkManager.IsServer ? "Server" :
            networkManager.IsClient ? "Client" :
            "Offline";

        GUILayout.Label("Mode: " + mode);

        GUILayout.EndArea();
    }

    private string GetLocalIPAddress()
    {
        var host = Dns.GetHostEntry(Dns.GetHostName());

        foreach (var ip in host.AddressList)
        {
            if (ip.AddressFamily == AddressFamily.InterNetwork)
            {
                return ip.ToString();
            }
        }

        return "IP not found";
    }
}