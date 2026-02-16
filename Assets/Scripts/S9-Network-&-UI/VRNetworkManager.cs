using Mirror;
using UnityEngine;
using System.Net;
using System.Net.Sockets;
using UnityEngine.Events;

public class VRNetworkManager : NetworkManager
{
    public static VRNetworkManager Instance { get; private set; }

    [Header("Experimenter Settings")]
    public GameObject experimenterPrefab;
    public GameObject playerPrefab;

    [Header("Events")]
    public UnityEvent onServerStarted;

    private string localIPAddress;
    private bool serverReady = false;

    public bool IsServerReady => serverReady;

    public override void Awake()
    {
        base.Awake();

        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    public override void Start()
    {
        base.Start();

        // Get IP address first
        localIPAddress = GetLocalIPAddress();
        Debug.Log($"[VRNetworkManager] Local IP detected: {localIPAddress}");

        // Start server
        StartHost();
    }

    public override void OnStartServer()
    {
        base.OnStartServer();
        serverReady = true;
        Debug.Log($"[VRNetworkManager] Server started successfully at {localIPAddress}:{GetComponent<TelepathyTransport>().port}");

        // Notify listeners that server is ready
        onServerStarted?.Invoke();
    }

    public override void OnServerAddPlayer(NetworkConnectionToClient conn)
    {
        GameObject playerObj;

        if (conn.connectionId == 0) // Host player (first connection)
        {
            playerObj = Instantiate(playerPrefab);
            Debug.Log("Player (Host) connected");
        }
        else // Experimenter client (second connection)
        {
            playerObj = Instantiate(experimenterPrefab);
            Debug.Log($"Experimenter client connected from {conn.address}");
        }

        NetworkServer.AddPlayerForConnection(conn, playerObj);
    }

    public string GetLocalIPAddress()
    {
        try
        {
            // Method 1: Get from DNS
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    // Skip loopback
                    if (!IPAddress.IsLoopback(ip))
                    {
                        return ip.ToString();
                    }
                }
            }

            // Method 2: Connect to external address to find local IP
            using (Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0))
            {
                socket.Connect("8.8.8.8", 65530);
                IPEndPoint endPoint = socket.LocalEndPoint as IPEndPoint;
                return endPoint.Address.ToString();
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"Could not get local IP: {e.Message}");
        }

        return "127.0.0.1";
    }

    public string GetConnectionURL()
    {
        int port = 7777; // Default Mirror port

        // Try to get actual port from transport
        var transport = GetComponent<TelepathyTransport>();
        if (transport != null)
        {
            port = transport.port;
        }

        return $"{localIPAddress}:{port}";
    }

    public string GetConnectionInfo()
    {
        return $"Server: {(serverReady ? "Ready" : "Starting...")}\nIP: {localIPAddress}\nPort: {GetComponent<TelepathyTransport>()?.port ?? 7777}";
    }
}