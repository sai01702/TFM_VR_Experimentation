using UnityEngine;

public class UICursorTrigger : MonoBehaviour
{
    [Tooltip("Assign the player's desktop rig here if you want to cache it manually")]
    public CursorHelper desktopRig;

    private void OnTriggerEnter(Collider other)
    {
        // If player has CursorHelper, show cursor
        var cursor = other.GetComponentInParent<CursorHelper>();
        if (cursor != null && GameSettings.Instance.CurrentMode == GameMode.Desktop)
            cursor.ShowCursor();
    }

    private void OnTriggerExit(Collider other)
    {
        var cursor = other.GetComponentInParent<CursorHelper>();
        if (cursor != null && GameSettings.Instance.CurrentMode == GameMode.Desktop)
            cursor.HideCursor();
    }
}
