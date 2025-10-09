using UnityEngine;

public class CursorHelper : MonoBehaviour
{
    void Start()
    {
        // if (GameSettings.Instance.CurrentMode != GameMode.VR)
         //   Cursor.lockState = CursorLockMode.None; // unlock mouse
       // else
           // Cursor.lockState = CursorLockMode.Locked; // keep hidden in VR

        //Cursor.visible = GameSettings.Instance.CurrentMode == GameMode.Desktop;
        
    }

    public void ShowCursor()
    {
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    public void HideCursor()
    {
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }
}
