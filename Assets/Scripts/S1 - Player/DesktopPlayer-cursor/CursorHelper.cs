using UnityEngine;
using UnityEngine.InputSystem;

public class CursorHelper : MonoBehaviour
{
    [Header("Input System")]
    public PlayerInput playerInput;                // auto-picked if null
    public string lookActionName = "Look";         // your mouse-look action name
    public bool lockCursorWhenHidden = true;       // typical FPS behavior

    [Header("Optional Fallback (no Input System)")]
    public Behaviour cameraLookScript;             // e.g., your MouseLook/Controller script

    InputAction _lookAction;

    void Awake()
    {
        if (playerInput == null) playerInput = GetComponentInParent<PlayerInput>();
        if (playerInput != null && !string.IsNullOrEmpty(lookActionName))
            _lookAction = playerInput.actions?[lookActionName];
    }

    public void ShowCursor()
    {
        if (GameSettings.Instance.CurrentMode != GameMode.Desktop) return;

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        // Disable camera look
        if (_lookAction != null && _lookAction.enabled) _lookAction.Disable();
        if (cameraLookScript != null) cameraLookScript.enabled = false;
    }

    public void HideCursor()
    {
        if (GameSettings.Instance.CurrentMode != GameMode.Desktop) return;

        Cursor.visible = !lockCursorWhenHidden;
        Cursor.lockState = lockCursorWhenHidden ? CursorLockMode.Locked : CursorLockMode.None;

        // Re-enable camera look
        if (_lookAction != null && !_lookAction.enabled) _lookAction.Enable();
        if (cameraLookScript != null) cameraLookScript.enabled = true;
    }
}
