using UnityEngine;
using TMPro;
using UnityEngine.InputSystem;

public class DesktopHint : MonoBehaviour
{
    [Header("Text")]
    public TMP_Text label; // drag your TMP text here (or it’ll auto-find)
    [TextArea] public string desktopMessage = "• Switch buttons: Tab\n• Confirm: Space";

    [Header("Behavior")]
    public float autoHideAfterSeconds = 6f;   // set 0 to never auto-hide
    public bool hideWhenAnyKeyUsed = true;    // hides on first Tab/Space press

    void Start()
    {
        // Show ONLY for Desktop
        if (GameSettings.Instance == null || GameSettings.Instance.CurrentMode != GameMode.Desktop)
        {
            gameObject.SetActive(false);
            return;
        }

        if (label == null) label = GetComponentInChildren<TMP_Text>(true);
        if (label != null) label.text = desktopMessage;

        if (autoHideAfterSeconds > 0f)
            Invoke(nameof(Hide), autoHideAfterSeconds);
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
