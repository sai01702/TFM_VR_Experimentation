using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class QRCodePanelController : MonoBehaviour
{
    [Header("UI References")]
    public GameObject qrCodePanel;
    public Button closeButton;

    [Header("VR Interaction")]
    public bool enableVRInteraction = true;
    public float raycastDistance = 10f;

    private bool isFirstSelection = true;

    void Start()
    {
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(ClosePanel);

            // Make sure button is keyboard navigable
            Navigation nav = closeButton.navigation;
            nav.mode = Navigation.Mode.Automatic;
            closeButton.navigation = nav;
        }
        else
        {
            Debug.LogError("Close button not assigned!");
        }

        // Show panel initially
        if (qrCodePanel != null)
        {
            qrCodePanel.SetActive(true);
        }
    }

    void Update()
    {
        // Keyboard navigation support
        HandleKeyboardInput();

        // VR controller raycast support
        if (enableVRInteraction && GameSettings.Instance != null)
        {
            // Check if in VR mode (adjust based on your GameSettings implementation)
            HandleVRInput();
        }
    }

    void HandleKeyboardInput()
    {
        var kb = Keyboard.current;
        if (kb == null) return;

        // Press Tab to select button
        if (kb.tabKey.wasPressedThisFrame)
        {
            if (EventSystem.current != null && closeButton != null)
            {
                EventSystem.current.SetSelectedGameObject(closeButton.gameObject);
                isFirstSelection = false;
            }
        }

        // Press Enter/Return/Space to activate selected button
        if (kb.enterKey.wasPressedThisFrame  ||
            kb.numpadEnterKey.wasPressedThisFrame ||
            kb.spaceKey.wasPressedThisFrame)
        {
            if (EventSystem.current != null &&
                EventSystem.current.currentSelectedGameObject == closeButton.gameObject)
            {
                ClosePanel();
            }
        }

        // ESC key also closes panel
        if (kb.escapeKey.wasPressedThisFrame)
        {
            ClosePanel();
        }
    }

    void HandleVRInput()
    {
        // This handles VR controller pointing at the button
        // The XR Interaction Toolkit should handle this automatically
        // if your Canvas has a Graphic Raycaster and your XR Rig has XR UI Input Module

        // Additional VR trigger support (if needed)
        // Uncomment if you want explicit trigger handling:
        /*
        if (Input.GetButtonDown("XRI_Right_TriggerButton") || 
            Input.GetButtonDown("XRI_Left_TriggerButton"))
        {
            // Check if raycast hits close button
            if (IsPointerOverButton())
            {
                ClosePanel();
            }
        }
        */
    }

    bool IsPointerOverButton()
    {
        if (EventSystem.current == null) return false;

        // Check if pointer is over the close button
        PointerEventData pointerData = new PointerEventData(EventSystem.current)
        {
            position = Input.mousePosition
        };

        var results = new System.Collections.Generic.List<RaycastResult>();
        EventSystem.current.RaycastAll(pointerData, results);

        foreach (var result in results)
        {
            if (result.gameObject == closeButton.gameObject)
            {
                return true;
            }
        }

        return false;
    }

    public void ClosePanel()
    {
        if (qrCodePanel != null)
        {
            qrCodePanel.SetActive(false);
            Debug.Log("QR Code panel closed");
        }
    }

    public void OpenPanel()
    {
        if (qrCodePanel != null)
        {
            qrCodePanel.SetActive(true);
            Debug.Log("QR Code panel opened");
        }
    }
}