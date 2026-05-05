using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

/// <summary>
/// Ensures GameOverScene UI (Exit button, etc.) receives pointer clicks after gameplay.
/// Desktop: unlocks/shows cursor if it was left locked by FPS-style rigs.
/// All modes: dedupes EventSystems and refreshes UI input modules (XR + Input System).
/// </summary>
public class GameOverInputFixer : MonoBehaviour
{
    [SerializeField] bool enableDebugLogs;

    void Awake()
    {
        ApplyDesktopCursorUnlock();
    }

    void Start()
    {
        Invoke(nameof(FixInputSystem), 0.1f);
    }

    void ApplyDesktopCursorUnlock()
    {
        bool desktop = GameSettings.Instance == null ||
                       GameSettings.Instance.CurrentMode == GameMode.Desktop;

        if (!desktop)
            return;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (enableDebugLogs)
            Debug.Log("[GameOverInputFixer] Cursor unlocked for Desktop.");
    }

    void FixInputSystem()
    {
        var attachedEs = GetComponent<EventSystem>();
        var eventSystem = attachedEs != null ? attachedEs : EventSystem.current ?? FindFirstObjectByType<EventSystem>();
        if (eventSystem == null)
        {
            Debug.LogError("[GameOverInputFixer] No EventSystem found in scene.");
            return;
        }

        var allEventSystems = FindObjectsByType<EventSystem>(FindObjectsSortMode.None);
        if (allEventSystems.Length > 1)
        {
            if (enableDebugLogs)
                Debug.LogWarning($"[GameOverInputFixer] Found {allEventSystems.Length} EventSystems — destroying duplicates.");

            // Prefer keeping this GameObject's EventSystem (this script lives on it).
            var keep = attachedEs != null ? attachedEs : eventSystem;
            foreach (var es in allEventSystems)
            {
                if (es != null && es != keep)
                    Destroy(es.gameObject);
            }

            eventSystem = EventSystem.current ?? keep;
        }

        if (eventSystem.currentSelectedGameObject != null)
            eventSystem.SetSelectedGameObject(null);

        foreach (var module in eventSystem.GetComponents<BaseInputModule>())
        {
            if (module == null)
                continue;
            module.enabled = false;
            module.enabled = true;
        }

        var inputSystemModule = eventSystem.GetComponent<InputSystemUIInputModule>();
        if (inputSystemModule != null && enableDebugLogs)
            Debug.Log("[GameOverInputFixer] Refreshed BaseInputModule(s) including InputSystemUIInputModule.");

        if (enableDebugLogs)
            Debug.Log($"[GameOverInputFixer] EventSystem.current = {EventSystem.current}, module = {eventSystem.currentInputModule}");
    }
}
