using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

namespace Bezi11.ExperimenterObserver
{
    /// <summary>
    /// Receives camera stream frames from the host and displays them on a RawImage.
    /// Registers for custom network messages to receive frames.
    /// </summary>
    public class ObserverCameraDisplay : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private RawImage streamDisplay;
        [SerializeField] private TextMeshProUGUI streamStatusText;

        private Texture2D displayTexture;
        private bool isReceivingFrames;
        private float lastFrameTime;
        private const float FrameTimeoutSeconds = 2f;
        private const string CAMERA_FRAME_MESSAGE = "CameraFrame";
        private bool isRegistered = false;

        void Start()
        {
            if (streamDisplay == null)
            {
                Debug.LogError("[ObserverCameraDisplay] StreamDisplay RawImage not assigned!");
            }

            if (streamStatusText != null)
            {
                streamStatusText.text = "Waiting for connection...";
            }

            // Wait for NetworkManager and register for messages
            StartCoroutine(WaitForNetworkManagerAndRegister());
        }

        private IEnumerator WaitForNetworkManagerAndRegister()
        {
            // Wait until NetworkManager exists and we're connected as client
            while (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsClient || NetworkManager.Singleton.IsServer)
            {
                yield return new WaitForSeconds(0.5f);
            }

            Debug.Log("[ObserverCameraDisplay] NetworkManager ready as client, registering for camera frames");
            
            if (streamStatusText != null)
            {
                streamStatusText.text = "Waiting for stream...";
            }

            // Register handler for camera frame messages
            NetworkManager.Singleton.CustomMessagingManager.RegisterNamedMessageHandler(
                CAMERA_FRAME_MESSAGE,
                OnCameraFrameReceived
            );
            
            isRegistered = true;
            Debug.Log("[ObserverCameraDisplay] Registered for camera frame messages");
        }

        void OnDestroy()
        {
            if (displayTexture != null)
            {
                Destroy(displayTexture);
            }

            // Unregister from custom messages
            if (isRegistered && NetworkManager.Singleton != null && NetworkManager.Singleton.CustomMessagingManager != null)
            {
                NetworkManager.Singleton.CustomMessagingManager.UnregisterNamedMessageHandler(CAMERA_FRAME_MESSAGE);
            }
        }

        void Update()
        {
            if (isReceivingFrames && Time.time - lastFrameTime > FrameTimeoutSeconds)
            {
                isReceivingFrames = false;
                if (streamStatusText != null)
                {
                    streamStatusText.text = "Stream lost...";
                }
                Debug.LogWarning("[ObserverCameraDisplay] Stream timeout - no frames received");
            }
        }

        private void OnCameraFrameReceived(ulong senderId, FastBufferReader reader)
        {
            // Read the frame data
            reader.ReadValueSafe(out int dataLength);
            byte[] imageData = new byte[dataLength];
            reader.ReadBytesSafe(ref imageData, dataLength);

            // Process the frame
            ReceiveFrame(imageData);
        }

        public void ReceiveFrame(byte[] imageData)
        {
            if (imageData == null || imageData.Length == 0) return;

            if (displayTexture == null)
            {
                displayTexture = new Texture2D(2, 2);
            }

            if (displayTexture.LoadImage(imageData))
            {
                if (streamDisplay != null)
                {
                    streamDisplay.texture = displayTexture;
                }

                if (!isReceivingFrames)
                {
                    isReceivingFrames = true;
                    if (streamStatusText != null)
                    {
                        streamStatusText.text = string.Empty;
                    }
                    Debug.Log("[ObserverCameraDisplay] Started receiving camera stream");
                }

                lastFrameTime = Time.time;
            }
            else
            {
                Debug.LogWarning("[ObserverCameraDisplay] Failed to decode image data");
            }
        }
    }
}
