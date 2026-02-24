using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Bezi11.ExperimenterObserver
{
    public class ObserverCameraDisplay : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private RawImage streamDisplay;
        [SerializeField] private TextMeshProUGUI streamStatusText;

        private Texture2D displayTexture;
        private bool isReceivingFrames;
        private float lastFrameTime;
        private const float FrameTimeoutSeconds = 2f;

        void Start()
        {
            if (streamDisplay == null)
            {
                Debug.LogError("[ObserverCameraDisplay] StreamDisplay RawImage not assigned!");
            }

            if (streamStatusText != null)
            {
                streamStatusText.text = "Waiting for stream...";
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
            }
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
                }

                lastFrameTime = Time.time;
            }
        }

        void OnDestroy()
        {
            if (displayTexture != null)
            {
                Destroy(displayTexture);
            }
        }
    }
}
