using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Localization;
using UnityEngine.Localization.Components;
using UnityEngine.SceneManagement;

public class DesktopHint : MonoBehaviour
{
    [Header("Text")]
    public TMP_Text label;
    [TextArea] public string desktopMessage = "• Switch buttons: Tab\n• Confirm: Space";

    [Header("Behavior")]
    public float autoHideAfterSeconds = 0f;   // 0 = never auto-hide
    public bool hideWhenAnyKeyUsed = false;   // Don't hide on key press
    public bool alwaysShowInStartMenu = true; // Always visible in start menu scene
    public bool onlyShowForDesktopMode = true; // Hide in VR mode (except StartMenu)

    [Header("Start Menu Scene")]
    public string startMenuSceneName = "0-StartMenu";

    void Start()
    {
        // Check if we're in the start menu
        bool isStartMenu = SceneManager.GetActiveScene().name == startMenuSceneName;

        // In start menu, always show (mode not selected yet)
        if (isStartMenu && alwaysShowInStartMenu)
        {
            SetupLabel();
            return; // Don't hide
        }

        // In other scenes, only show for Desktop mode
        if (onlyShowForDesktopMode)
        {
            if (GameSettings.Instance == null || GameSettings.Instance.CurrentMode != GameMode.Desktop)
            {
                gameObject.SetActive(false);
                return;
            }
        }

        SetupLabel();

        if (autoHideAfterSeconds > 0f)
            Invoke(nameof(Hide), autoHideAfterSeconds);
    }

    void SetupLabel()
    {
        if (label == null) label = GetComponentInChildren<TMP_Text>(true);

        // Check if there's a LocalizeStringEvent component - if so, don't overwrite the text
        if (label != null)
        {
            LocalizeStringEvent localizeStringEvent = label.GetComponent<LocalizeStringEvent>();
            if (localizeStringEvent == null)
            {
                // Only set the text if there's no LocalizeStringEvent component
                label.text = desktopMessage;
            }
            // If LocalizeStringEvent exists, it will handle the text automatically
        }
    }

    void Update()
    {
        if (!hideWhenAnyKeyUsed) return;
        if (Keyboard.current == null) return;

        // If player starts using the keys, hide the hint
        if (Keyboard.current.tabKey.wasPressedThisFrame || Keyboard.current.spaceKey.wasPressedThisFrame)
            Hide();
    }

    void Hide()
    {
        gameObject.SetActive(false);
    }
}