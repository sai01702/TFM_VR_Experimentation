using Unity.Netcode;
using UnityEngine;
using System.Collections;

namespace Bezi11.ExperimenterObserver
{
    public class PlayerCameraStreamer : NetworkBehaviour
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
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            if (IsServer)
            {
                InitializeStreaming();
                NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
                NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
            }
        }

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();

            if (IsServer)
            {
                NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
                NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
            }

            CleanupStreaming();
        }

        void OnDestroy()
        {
            CleanupStreaming();
        }

        private void OnClientConnected(ulong clientId)
        {
            if (clientId == NetworkManager.ServerClientId) return;

            Debug.Log($"[PlayerCameraStreamer] Observer client connected: {clientId}");
            isStreaming = true;
        }

        private void OnClientDisconnected(ulong clientId)
        {
            if (clientId == NetworkManager.ServerClientId) return;

            Debug.Log($"[PlayerCameraStreamer] Observer client disconnected: {clientId}");

            if (NetworkManager.Singleton.ConnectedClientsList.Count <= 1)
            {
                isStreaming = false;
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
            Debug.Log("[PlayerCameraStreamer] Streaming initialized");
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
            if (!IsServer || !isStreaming || playerCamera == null) return;

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

            SendFrameToObserversClientRpc(imageData);
        }

        [ClientRpc]
        private void SendFrameToObserversClientRpc(byte[] imageData)
        {
            if (IsServer) return;

            ObserverCameraDisplay display = FindObjectOfType<ObserverCameraDisplay>();
            if (display != null)
            {
                display.ReceiveFrame(imageData);
            }
        }
    }
}
