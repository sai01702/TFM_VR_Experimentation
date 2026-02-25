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
            // Wait until NetworkManager exists and is server/host
            while (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsServer)
            {
                yield return new WaitForSeconds(0.5f);
            }

            Debug.Log("[PlayerCameraStreamer] NetworkManager ready as server, initializing streaming");
            InitializeStreaming();
            
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
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

            // Stop streaming if no more observers
            if (NetworkManager.Singleton.ConnectedClientsList.Count <= 1)
            {
                isStreaming = false;
                Debug.Log("[PlayerCameraStreamer] No more observers - Stopping stream");
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
            if (!isStreaming || playerCamera == null) return;

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
