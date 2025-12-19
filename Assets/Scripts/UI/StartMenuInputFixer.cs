using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using TMPro;

/// <summary>
/// Ensures the Start Menu UI is properly interactive.
/// Fixes issues with Input System not working on subsequent runs.
/// Add this to any GameObject in the 0-StartMenu scene.
/// </summary>
public class StartMenuInputFixer : MonoBehaviour
{
    [SerializeField] TMP_InputField inputFieldToFocus;
    [SerializeField] bool enableDebugLogs = true;

    void Awake()
    {
        // Ensure cursor is visible and unlocked
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        
        if (enableDebugLogs)
            Debug.Log("[StartMenuInputFixer] Cursor unlocked and visible");
    }

    void Start()
    {
        // Delay to let everything initialize
        Invoke(nameof(FixInputSystem), 0.1f);
    }

    void FixInputSystem()
    {
        // Find and fix EventSystem
        var eventSystem = EventSystem.current;
        if (eventSystem == null)
        {
            eventSystem = FindFirstObjectByType<EventSystem>();
            if (enableDebugLogs)
                Debug.Log($"[StartMenuInputFixer] Found EventSystem by search: {eventSystem}");
        }

        if (eventSystem == null)
        {
            Debug.LogError("[StartMenuInputFixer] No EventSystem found in scene!");
            return;
        }

        // Check for duplicate EventSystems (common issue)
        var allEventSystems = FindObjectsByType<EventSystem>(FindObjectsSortMode.None);
        if (allEventSystems.Length > 1)
        {
            Debug.LogWarning($"[StartMenuInputFixer] Found {allEventSystems.Length} EventSystems! Destroying extras...");
            for (int i = 1; i < allEventSystems.Length; i++)
            {
                Debug.Log($"[StartMenuInputFixer] Destroying duplicate EventSystem: {allEventSystems[i].gameObject.name}");
                Destroy(allEventSystems[i].gameObject);
            }
        }

        // Re-enable the Input System UI Input Module
        var inputModule = eventSystem.GetComponent<InputSystemUIInputModule>();
        if (inputModule != null)
        {
            // Disable and re-enable to force refresh
            inputModule.enabled = false;
            inputModule.enabled = true;
            
            if (enableDebugLogs)
                Debug.Log("[StartMenuInputFixer] InputSystemUIInputModule refreshed");
        }
        else
        {
            Debug.LogWarning("[StartMenuInputFixer] No InputSystemUIInputModule found on EventSystem");
        }

        // Focus the input field if specified
        if (inputFieldToFocus != null)
        {
            eventSystem.SetSelectedGameObject(inputFieldToFocus.gameObject);
            inputFieldToFocus.ActivateInputField();
            
            if (enableDebugLogs)
                Debug.Log($"[StartMenuInputFixer] Focused input field: {inputFieldToFocus.name}");
        }

        if (enableDebugLogs)
        {
            Debug.Log($"[StartMenuInputFixer] EventSystem.current = {EventSystem.current}");
            Debug.Log($"[StartMenuInputFixer] currentInputModule = {eventSystem.currentInputModule}");
        }
    }

    void Update()
    {
        // Debug: Press F1 to see current state (using NEW Input System)
        var keyboard = Keyboard.current;
        if (enableDebugLogs && keyboard != null && keyboard.f1Key.wasPressedThisFrame)
        {
            var es = EventSystem.current;
            Debug.Log($"[StartMenuInputFixer] F1 Debug:");
            Debug.Log($"  - EventSystem.current: {es}");
            Debug.Log($"  - currentSelectedGameObject: {es?.currentSelectedGameObject}");
            Debug.Log($"  - currentInputModule: {es?.currentInputModule}");
            Debug.Log($"  - Cursor.lockState: {Cursor.lockState}");
            Debug.Log($"  - Cursor.visible: {Cursor.visible}");
            
            var allES = FindObjectsByType<EventSystem>(FindObjectsSortMode.None);
            Debug.Log($"  - Total EventSystems in scene: {allES.Length}");
        }
    }
}
