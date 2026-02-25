using Unity.Netcode;
using UnityEngine;
using System.Collections;

namespace Bezi11.ExperimenterObserver
{
    /// <summary>
    /// Captures the player's camera view and streams it to connected observer clients.
    /// Uses NetworkManager's CustomMessagingManager instead of RPCs to avoid NetworkObject requirements.
    /// </summary>
    public class PlayerCameraStreamer : MonoBehaviour
    {
        [Header("Streaming Settings")]
        [SerializeField] private int targetFrameRate = 30;
        [SerializeField] private int renderTextureWidth = 1280;
        [SerializeField] private int renderTextureHeight = 720;
        [SerializeField][Range(50, 100)] private int jpegQuality = 75;

        [Header("Performance")]
        [SerializeField] private bool onlyStreamWhenObserverConnected = true;

        private Camera playerCamera;
        private RenderTexture renderTexture;
        private Texture2D captureTexture;
        private float nextCaptureTime;
        private bool isStreaming;
        private const string CAMERA_FRAME_MESSAGE = "CameraFrame";

        void Start()
        {
            playerCamera = GetComponentInChildren<Camera>();
            if (playerCamera == null)
            {
                playerCamera = Camera.main;
            }

            if (playerCamera == null)
            {
                Debug.LogError("[PlayerCameraStreamer] No camera found on rig!");
                enabled = false;
                return;
            }

            Debug.Log($"[PlayerCameraStreamer] Initialized with camera: {playerCamera.name}");
            
            // Wait for NetworkManager to be ready
            StartCoroutine(WaitForNetworkManagerAndStart());
        }

        private IEnumerator WaitForNetworkManagerAndStart()
        {
            Debug.Log("[PlayerCameraStreamer] Waiting for NetworkManager to become server...");
            
            // Wait until NetworkManager exists and is server/host
            float waitTime = 0f;
            while (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsServer)
            {
                yield return new WaitForSeconds(0.2f);
                waitTime += 0.2f;
                
                if (waitTime >= 10f)
                {
                    Debug.LogError("[PlayerCameraStreamer] Timeout waiting for NetworkManager.IsServer after 10 seconds");
                    yield break;
                }
            }

            Debug.Log($"[PlayerCameraStreamer] NetworkManager ready as server after {waitTime:F1}s, initializing streaming");
            InitializeStreaming();
            
            // CRITICAL: Register for future connections
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
            
            // CRITICAL: Check for ALREADY CONNECTED clients (observer might have joined before rig spawned!)
            Debug.Log($"[PlayerCameraStreamer] Checking for existing connections... Total clients: {NetworkManager.Singleton.ConnectedClientsList.Count}");
            
            // Check if there are any connected clients besides the server itself
            // Note: In some configurations, the observer might have LocalClientId == ServerClientId
            // So we also check if ConnectedClientsList has more than 1 client (server + observer)
            int observerCount = 0;
            
            foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
            {
                Debug.Log($"[PlayerCameraStreamer] Found client: {client.ClientId} (ServerClientId: {NetworkManager.ServerClientId})");
                
                // If this is not the server itself, it's an observer
                if (client.ClientId != NetworkManager.ServerClientId)
                {
                    Debug.Log($"[PlayerCameraStreamer] ✅ FOUND EXISTING OBSERVER: {client.ClientId} - Starting stream NOW!");
                    isStreaming = true;
                    observerCount++;
                }
            }
            
            // FALLBACK: If we have more than 1 connected client total, someone must be observing
            if (observerCount == 0 && NetworkManager.Singleton.ConnectedClientsList.Count > 1)
            {
                Debug.Log($"[PlayerCameraStreamer] ✅ Multiple clients detected ({NetworkManager.Singleton.ConnectedClientsList.Count}), starting stream!");
                isStreaming = true;
            }
            
            if (!isStreaming)
            {
                Debug.Log("[PlayerCameraStreamer] No observers connected yet, waiting for connections...");
            }
        }

        void OnDestroy()
        {
            CleanupStreaming();
            
            if (NetworkManager.Singleton != null)
            {
                NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
                NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
            }
        }

        private void OnClientConnected(ulong clientId)
        {
            if (clientId == NetworkManager.ServerClientId) return;

            Debug.Log($"[PlayerCameraStreamer] Observer client connected: {clientId} - Starting stream");
            isStreaming = true;
        }

        private void OnClientDisconnected(ulong clientId)
        {
            if (clientId == NetworkManager.ServerClientId) return;

            Debug.Log($"[PlayerCameraStreamer] Observer client disconnected: {clientId}");

            // Count remaining observers (exclude server)
            int observerCount = 0;
            foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
            {
                if (client.ClientId != NetworkManager.ServerClientId)
                {
                    observerCount++;
                    Debug.Log($"[PlayerCameraStreamer] Still connected: ClientId {client.ClientId}");
                }
            }
            
            if (observerCount == 0)
            {
                isStreaming = false;
                Debug.Log("[PlayerCameraStreamer] No more observers - Stopping stream");
            }
            else
            {
                Debug.Log($"[PlayerCameraStreamer] {observerCount} observer(s) still connected - Continuing stream");
            }
        }

        private void InitializeStreaming()
        {
            if (renderTexture == null)
            {
                renderTexture = new RenderTexture(renderTextureWidth, renderTextureHeight, 24);
                renderTexture.Create();
            }

            if (captureTexture == null)
            {
                captureTexture = new Texture2D(renderTextureWidth, renderTextureHeight, TextureFormat.RGB24, false);
            }

            isStreaming = !onlyStreamWhenObserverConnected;
            Debug.Log($"[PlayerCameraStreamer] Streaming initialized. Initial streaming state: {isStreaming}");
        }

        private void CleanupStreaming()
        {
            if (renderTexture != null)
            {
                renderTexture.Release();
                Destroy(renderTexture);
                renderTexture = null;
            }

            if (captureTexture != null)
            {
                Destroy(captureTexture);
                captureTexture = null;
            }
        }

        void Update()
        {
            if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsServer) return;
            if (playerCamera == null) return;

            // CRITICAL: Check if we should be streaming
            // Check both: explicit observer detection AND client count > 1 (fallback)
            if (!isStreaming && NetworkManager.Singleton.ConnectedClientsList.Count > 1)
            {
                Debug.Log($"[PlayerCameraStreamer] ⚠️ Detected {NetworkManager.Singleton.ConnectedClientsList.Count} clients but not streaming - starting now!");
                isStreaming = true;
            }

            if (!isStreaming) return;

            if (Time.time >= nextCaptureTime)
            {
                CaptureAndStreamFrame();
                nextCaptureTime = Time.time + (1f / targetFrameRate);
            }
        }

        private void CaptureAndStreamFrame()
        {
            RenderTexture previousRT = RenderTexture.active;
            RenderTexture previousCameraRT = playerCamera.targetTexture;

            playerCamera.targetTexture = renderTexture;
            playerCamera.Render();

            RenderTexture.active = renderTexture;
            captureTexture.ReadPixels(new Rect(0, 0, renderTextureWidth, renderTextureHeight), 0, 0);
            captureTexture.Apply();

            playerCamera.targetTexture = previousCameraRT;
            RenderTexture.active = previousRT;

            byte[] imageData = captureTexture.EncodeToJPG(jpegQuality);

            // Send to all connected clients (observers) using custom message
            SendFrameToObservers(imageData);
        }

        private void SendFrameToObservers(byte[] imageData)
        {
            if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsServer) return;

            using (var writer = new FastBufferWriter(imageData.Length + 128, Unity.Collections.Allocator.Temp))
            {
                writer.WriteValueSafe(imageData.Length);
                writer.WriteBytesSafe(imageData);

                // Send to all connected clients except server
                foreach (var clientId in NetworkManager.Singleton.ConnectedClientsIds)
                {
                    if (clientId != NetworkManager.ServerClientId)
                    {
                        NetworkManager.Singleton.CustomMessagingManager.SendNamedMessage(
                            CAMERA_FRAME_MESSAGE,
                            clientId,
                            writer
                        );
                    }
                }
            }
        }
    }
}
