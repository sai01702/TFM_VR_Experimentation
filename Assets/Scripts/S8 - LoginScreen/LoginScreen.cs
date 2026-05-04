using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using TMPro;
using UnityEngine.UI;

public class LoginScreen : MonoBehaviour
{
    [Header("UI")]
    public TMP_InputField idInput;
    public Button okButton;
    public TextMeshProUGUI errorText;

    [Header("Flow")]
    public string nextSceneName = "RoomScene";

    [Header("Debug")]
    public bool enableDebug = true;

    [Header("Auto Focus")]
    [Tooltip("Only focus input field once at startup, don't interfere with Tab navigation")]
    public bool autoFocusInputOnStart = true;
    
    private static bool hasAutoFocused = false;

    void Awake()
    {
        // CRITICAL: Reset Input System state on scene load
        // This fixes the "works first time, not after" issue
        ResetInputSystem();
    }

    void Start()
    {
        // Ensure cursor is unlocked for UI interaction
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (errorText != null) errorText.text = "";

        // Pre-fill last ID if we have one
        if (ParticipantSession.Instance != null &&
            !string.IsNullOrEmpty(ParticipantSession.Instance.ParticipantId))
        {
            idInput.text = ParticipantSession.Instance.ParticipantId;
        }

        // Ensure button is interactable and has listener
        if (okButton != null)
        {
            okButton.interactable = true;
            okButton.onClick.RemoveAllListeners();
            okButton.onClick.AddListener(OnOkClicked);
            
            if (enableDebug)
                Debug.Log($"[LoginScreen] Button '{okButton.name}' listener added. Interactable: {okButton.interactable}");
        }
        else
        {
            Debug.LogError("[LoginScreen] okButton is NULL! Please assign it in Inspector.");
        }

        // Use TMP_InputField's onSubmit event (fires when Enter is pressed while focused)
        if (idInput != null)
        {
            idInput.onSubmit.RemoveAllListeners();
            idInput.onSubmit.AddListener((text) => {
                if (enableDebug)
                    Debug.Log($"[LoginScreen] Input field onSubmit fired with: {text}");
                OnOkClicked();
            });
            
            // Also use onEndEdit as backup
            idInput.onEndEdit.RemoveAllListeners();
            idInput.onEndEdit.AddListener((text) => {
                // Only submit if Enter was pressed (not just clicking away)
                var keyboard = Keyboard.current;
                if (keyboard != null && (keyboard.enterKey.isPressed || keyboard.numpadEnterKey.isPressed))
                {
                    if (enableDebug)
                        Debug.Log($"[LoginScreen] Input field onEndEdit with Enter key: {text}");
                    OnOkClicked();
                }
            });
            
            if (enableDebug)
                Debug.Log("[LoginScreen] Input field submit listeners added");
        }

        // Fix: Disable RaycastTarget on background images that might block button clicks
        DisableBackgroundRaycasts();

        // Ensure EventSystem is properly set up BEFORE auto-focus so the input
        // module is stable when ActivateInputField() runs.
        FixEventSystem();

        // Auto-focus the input field after the input module has settled
        // (only once per session)
        if (autoFocusInputOnStart && !hasAutoFocused)
        {
            StartCoroutine(FocusInputFieldDelayed());
        }
    }

    System.Collections.IEnumerator FocusInputFieldDelayed()
    {
        // Real-time delay so the input module finishes its enable cycle and
        // the keyboard text-input pipeline is live before we activate.
        yield return new WaitForSecondsRealtime(0.15f);

        if (idInput == null || EventSystem.current == null) yield break;

        // Don't steal focus if user has already tabbed to something else
        var currentSelection = EventSystem.current.currentSelectedGameObject;
        if (currentSelection != null && currentSelection != idInput.gameObject) yield break;

        // Mimic the click-away-then-back workaround: fully deactivate, wait a
        // frame, then re-select and re-activate. This forces TMP_InputField to
        // re-subscribe to keyboard text input cleanly.
        EventSystem.current.SetSelectedGameObject(null);
        idInput.DeactivateInputField();
        yield return null;

        EventSystem.current.SetSelectedGameObject(idInput.gameObject);
        idInput.ActivateInputField();
        idInput.Select();
        idInput.caretPosition = idInput.text.Length;

        hasAutoFocused = true;

        if (enableDebug)
            Debug.Log("[LoginScreen] Auto-focused input field (one-time)");
    }

    void ResetInputSystem()
    {
        // Force reset Input System - this fixes state issues between play sessions.
        // Skip Keyboard and Mouse: resetting them on a UI scene clears their state
        // right when TMP_InputField is about to subscribe to keyboard text input,
        // leaving the field selected but unable to receive keystrokes.
        if (InputSystem.devices.Count > 0)
        {
            foreach (var device in InputSystem.devices)
            {
                if (device is Keyboard || device is Mouse) continue;
                InputSystem.ResetDevice(device);
            }
            if (enableDebug)
                Debug.Log("[LoginScreen] Input devices reset (keyboard/mouse skipped)");
        }
    }

    void FixEventSystem()
    {
        // Destroy duplicate EventSystems
        var allEventSystems = FindObjectsByType<EventSystem>(FindObjectsSortMode.None);
        if (allEventSystems.Length > 1)
        {
            Debug.LogWarning($"[LoginScreen] Found {allEventSystems.Length} EventSystems! Keeping first, destroying others.");
            for (int i = 1; i < allEventSystems.Length; i++)
            {
                Destroy(allEventSystems[i].gameObject);
            }
        }

        var eventSystem = EventSystem.current;
        if (eventSystem == null)
        {
            eventSystem = FindFirstObjectByType<EventSystem>();
        }

        if (eventSystem != null)
        {
            // Force refresh the input module
            var inputModule = eventSystem.GetComponent<InputSystemUIInputModule>();
            if (inputModule != null)
            {
                // Disable and re-enable to force refresh
                inputModule.enabled = false;
                inputModule.enabled = true;
                
                // Make sure actions are enabled
                if (inputModule.actionsAsset != null)
                {
                    inputModule.actionsAsset.Enable();
                }
                
                if (enableDebug)
                    Debug.Log("[LoginScreen] InputSystemUIInputModule refreshed and actions enabled");
            }

            // Force update the EventSystem
            eventSystem.enabled = false;
            eventSystem.enabled = true;
            
            if (enableDebug)
                Debug.Log($"[LoginScreen] EventSystem refreshed: {eventSystem.name}");
        }
    }

    void DisableBackgroundRaycasts()
    {
        // Find RawImage components and disable their raycast if they're not interactive
        var canvas = GetComponentInParent<Canvas>();
        if (canvas == null) canvas = FindFirstObjectByType<Canvas>();
        
        if (canvas != null)
        {
            var rawImages = canvas.GetComponentsInChildren<RawImage>(true);
            foreach (var img in rawImages)
            {
                // Only disable if it's not a button's child
                if (img.GetComponent<Button>() == null && 
                    img.GetComponentInParent<Button>() == null &&
                    img.GetComponent<TMP_InputField>() == null &&
                    img.GetComponentInParent<TMP_InputField>() == null)
                {
                    img.raycastTarget = false;
                    if (enableDebug)
                        Debug.Log($"[LoginScreen] Disabled raycast on: {img.name}");
                }
            }
        }
    }

    void Update()
    {
        var keyboard = Keyboard.current;
        var mouse = Mouse.current;
        
        // Allow Enter key to submit (using NEW Input System)
        if (keyboard != null && (keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame))
        {
            if (enableDebug)
                Debug.Log("[LoginScreen] Enter key pressed - calling OnOkClicked");
            OnOkClicked();
        }
    }

    // Use OnGUI for reliable click detection (bypasses Input System issues)
    void OnGUI()
    {
        Event e = Event.current;
        if (e.type == EventType.MouseDown && e.button == 0)
        {
            // GUI coordinates are flipped on Y axis
            Vector2 mousePos = new Vector2(e.mousePosition.x, Screen.height - e.mousePosition.y);
            
            // Check if clicking on input field
            if (idInput != null)
            {
                RectTransform inputRect = idInput.GetComponent<RectTransform>();
                if (inputRect != null && RectTransformUtility.RectangleContainsScreenPoint(inputRect, mousePos, null))
                {
                    if (enableDebug)
                        Debug.Log("[LoginScreen] OnGUI click on input field - focusing");
                    
                    EventSystem.current?.SetSelectedGameObject(idInput.gameObject);
                    idInput.ActivateInputField();
                    idInput.Select();
                    return;
                }
            }
            
            // Check if clicking on button
            if (okButton != null)
            {
                RectTransform buttonRect = okButton.GetComponent<RectTransform>();
                if (buttonRect != null && RectTransformUtility.RectangleContainsScreenPoint(buttonRect, mousePos, null))
                {
                    if (enableDebug)
                        Debug.Log("[LoginScreen] OnGUI click on button - invoking");
                    OnOkClicked();
                    return;
                }
            }
        }
    }

    bool IsPointerOverButton(Button button, Vector2 screenPosition)
    {
        if (button == null) return false;
        
        RectTransform rectTransform = button.GetComponent<RectTransform>();
        if (rectTransform == null) return false;

        Vector2 localPoint;
        Canvas canvas = button.GetComponentInParent<Canvas>();
        Camera cam = null;
        
        if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
        {
            cam = canvas.worldCamera;
            if (cam == null) cam = Camera.main;
        }

        bool isInside = RectTransformUtility.ScreenPointToLocalPointInRectangle(
            rectTransform, 
            screenPosition, 
            cam, 
            out localPoint
        );

        if (isInside)
        {
            isInside = rectTransform.rect.Contains(localPoint);
        }

        return isInside;
    }

    void OnOkClicked()
    {
        if (enableDebug)
            Debug.Log("[LoginScreen] OnOkClicked called!");

        if (idInput == null)
        {
            Debug.LogError("[LoginScreen] idInput is NULL!");
            return;
        }

        string raw = idInput.text.Trim();
        Debug.Log($"[LoginScreen] Input text: '{raw}'");

        if (string.IsNullOrEmpty(raw))
        {
            Debug.Log("[LoginScreen] Input is empty, showing error");
            if (errorText != null)
                errorText.text = "Please enter a Participant ID.";
            return;
        }

        if (ParticipantSession.Instance == null)
        {
            Debug.LogError("[LoginScreen] ParticipantSession.Instance is NULL!");
            // Try to find it
            var ps = FindFirstObjectByType<ParticipantSession>();
            if (ps != null)
            {
                Debug.Log("[LoginScreen] Found ParticipantSession, proceeding...");
                ps.SetParticipant(raw);
                ps.AppendLog("SessionStart");
            }
        }
        else
        {
            ParticipantSession.Instance.SetParticipant(raw);
            ParticipantSession.Instance.AppendLog("SessionStart");
        }

        Debug.Log($"[LoginScreen] Loading scene: {nextSceneName}");
        SceneManager.LoadScene(nextSceneName);
    }
}
