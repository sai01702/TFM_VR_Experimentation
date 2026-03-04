using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

[RequireComponent(typeof(NetworkManager))]
public class HelloWorldManager : MonoBehaviour
{
    private NetworkManager networkManager;
    private UnityTransport transport;

    private string ipAddress = "127.0.0.1";
    private ushort port = 7777;

    private void Awake()
    {
        networkManager = GetComponent<NetworkManager>();
        transport = networkManager.GetComponent<UnityTransport>();
    }

    private void OnGUI()
    {
        GUILayout.BeginArea(new Rect(10, 10, 320, 400));
        GUILayout.Label("=== LAN Multiplayer Debug ===");

        if (!networkManager.IsClient && !networkManager.IsServer)
        {
            DrawConnectionInputs();
            DrawStartButtons();
        }
        else
        {
            DrawStatus();
            DrawDisconnectButton();
        }

        GUILayout.EndArea();
    }

    private void DrawConnectionInputs()
    {
        GUILayout.Space(10);

        GUILayout.Label("IP Address:");
        ipAddress = GUILayout.TextField(ipAddress);

        GUILayout.Label("Port:");
        string portString = GUILayout.TextField(port.ToString());
        ushort.TryParse(portString, out port);

        GUILayout.Space(10);
    }

    private void DrawStartButtons()
    {
        if (GUILayout.Button("Start Host"))
        {
            transport.ConnectionData.Address = "0.0.0.0";
            transport.ConnectionData.Port = port;

            networkManager.StartHost();
        }

        if (GUILayout.Button("Start Client"))
        {
            transport.ConnectionData.Address = ipAddress;
            transport.ConnectionData.Port = port;

            networkManager.StartClient();
        }

        if (GUILayout.Button("Start Server (Headless)"))
        {
            transport.ConnectionData.Address = "0.0.0.0";
            transport.ConnectionData.Port = port;

            networkManager.StartServer();
        }
    }

    private void DrawStatus()
    {
        GUILayout.Space(10);

        string mode =
            networkManager.IsHost ? "Host" :
            networkManager.IsServer ? "Server" :
            "Client";

        GUILayout.Label("Mode: " + mode);
        GUILayout.Label("Transport: " + transport.GetType().Name);
        GUILayout.Label("Connected Clients: " + networkManager.ConnectedClientsIds.Count);
    }

    private void DrawDisconnectButton()
    {
        GUILayout.Space(15);

        if (GUILayout.Button("Shutdown"))
        {
            networkManager.Shutdown();
        }
    }
}