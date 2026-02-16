using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using ZXing;
using ZXing.QrCode;
using static UnityEngine.UIElements.VisualElement;

public class QRCodeGenerator : MonoBehaviour
{
    [Header("UI References")]
    public RawImage qrCodeImage;
    public Text urlText; // Legacy
    public TextMeshProUGUI urlTextTMP; // TMP

    [Header("Settings")]
    public bool generateOnStart = true;
    public float retryInterval = 0.5f;
    public int maxRetries = 10;

    private Texture2D qrTexture;
    private int retryCount = 0;

    void Start()
    {
        if (generateOnStart)
        {
            StartCoroutine(WaitForServerAndGenerate());
        }
    }

    IEnumerator WaitForServerAndGenerate()
    {
        // Wait for VRNetworkManager to exist
        while (VRNetworkManager.Instance == null && retryCount < maxRetries)
        {
            Debug.Log($"Waiting for VRNetworkManager... ({retryCount + 1}/{maxRetries})");
            retryCount++;
            yield return new WaitForSeconds(retryInterval);
        }

        if (VRNetworkManager.Instance == null)
        {
            Debug.LogError("VRNetworkManager not found after waiting!");
            SetErrorText("Network Manager not found!");
            yield break;
        }

        // Wait for server to be ready
        retryCount = 0;
        while (!VRNetworkManager.Instance.IsServerReady && retryCount < maxRetries)
        {
            Debug.Log($"Waiting for server to start... ({retryCount + 1}/{maxRetries})");
            SetStatusText("Starting server...");
            retryCount++;
            yield return new WaitForSeconds(retryInterval);
        }

        if (!VRNetworkManager.Instance.IsServerReady)
        {
            Debug.LogError("Server failed to start!");
            SetErrorText("Server failed to start!");
            yield break;
        }

        // Small additional delay to ensure everything is initialized
        yield return new WaitForSeconds(0.5f);

        // Now generate QR code
        GenerateQRCode();
    }

    public void GenerateQRCode()
    {
        if (VRNetworkManager.Instance == null)
        {
            Debug.LogError("VRNetworkManager not found!");
            SetErrorText("Network Manager not found!");
            return;
        }

        if (!VRNetworkManager.Instance.IsServerReady)
        {
            Debug.LogWarning("Server not ready yet!");
            SetStatusText("Server starting...");
            return;
        }

        string url = VRNetworkManager.Instance.GetConnectionURL();

        Debug.Log($"=== Generating QR Code ===");
        Debug.Log($"URL: {url}");
        Debug.Log($"Connection Info: {VRNetworkManager.Instance.GetConnectionInfo()}");

        // Generate QR code
        var writer = new BarcodeWriter
        {
            Format = BarcodeFormat.QR_CODE,
            Options = new QrCodeEncodingOptions
            {
                Height = 256,
                Width = 256,
                Margin = 1
            }
        };

        try
        {
            var pixels = writer.Write(url);
            qrTexture = new Texture2D(256, 256);
            qrTexture.SetPixels32(pixels);
            qrTexture.Apply();

            // Display QR code
            if (qrCodeImage != null)
            {
                qrCodeImage.texture = qrTexture;
                Debug.Log("QR Code image updated");
            }
            else
            {
                Debug.LogWarning("QR Code Image reference is null!");
            }

            // Display URL text
            string urlDisplayText = $"Experimenter Connection\n\n{url}\n\nScan to connect";

            if (urlText != null)
            {
                urlText.text = urlDisplayText;
            }

            if (urlTextTMP != null)
            {
                urlTextTMP.text = urlDisplayText;
            }

            Debug.Log($"✓ QR Code generated successfully for: {url}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to generate QR code: {e.Message}");
            SetErrorText($"QR Generation Failed:\n{e.Message}");
        }
    }

    void SetStatusText(string message)
    {
        if (urlText != null) urlText.text = message;
        if (urlTextTMP != null) urlTextTMP.text = message;
    }

    void SetErrorText(string error)
    {
        string errorMessage = $"<color=red>ERROR:</color>\n{error}";
        if (urlText != null) urlText.text = errorMessage;
        if (urlTextTMP != null) urlTextTMP.text = errorMessage;
    }

    // Public method to manually regenerate
    public void RegenerateQRCode()
    {
        StopAllCoroutines();
        retryCount = 0;
        StartCoroutine(WaitForServerAndGenerate());
    }
}