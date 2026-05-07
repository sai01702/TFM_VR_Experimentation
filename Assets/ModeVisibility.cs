using UnityEngine;

/// <summary>
/// Shows or hides this GameObject based on <see cref="GameSettings.CurrentMode"/>.
/// Configure which mode(s) are visible from the Inspector.
/// </summary>
[DefaultExecutionOrder(-100)]
public class ModeVisibility : MonoBehaviour
{
    public enum VisibleMode { DesktopOnly, VROnly, Both }

    [Header("Visibility")]
    [Tooltip("Pick which mode this GameObject should be visible in.")]
    public VisibleMode showIn = VisibleMode.DesktopOnly;

    [Tooltip("If GameSettings is missing, fall back to this mode.")]
    public GameMode fallbackMode = GameMode.Desktop;

    void Awake()
    {
        Apply();
    }

    void Apply()
    {
        GameMode current = GameSettings.Instance != null
            ? GameSettings.Instance.CurrentMode
            : fallbackMode;

        bool visible = showIn == VisibleMode.Both
            || (showIn == VisibleMode.DesktopOnly && current == GameMode.Desktop)
            || (showIn == VisibleMode.VROnly && current == GameMode.VR);

        gameObject.SetActive(visible);
    }
}
