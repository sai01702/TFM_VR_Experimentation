using UnityEngine;

public class ExitAppButton : MonoBehaviour
{
    // Este método puede asignarse al evento OnClick del botón
    public void ExitApplication()
    {
        // Cierra la aplicación en una build
        Application.Quit();
        PlayerPrefs.DeleteAll();

        // Este log aparece solo en el editor para confirmar el cierre
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}
