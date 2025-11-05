using UnityEngine;
using UnityEngine.SceneManagement;

public class LoadSceneByButton : MonoBehaviour
{
    // Which scene to load per mode
    public string desktopSceneName = "PuzzleScene";
    public string vrSceneName = "PuzzleSceneVR";

    // Same data you already pass forward
    public string objectToActivateInScene;     // e.g. "V1_Basic_Version"
    public string controllerToActivateInScene; // e.g. "Right Controller"

    public void LoadScene()
    {
        // pick target scene based on saved mode (default to Desktop if missing)
        string targetScene =
            (GameSettings.Instance != null && GameSettings.Instance.CurrentMode == GameMode.VR)
            ? vrSceneName
            : desktopSceneName;

        if (string.IsNullOrEmpty(targetScene))
            return;

        // keep your original PlayerPrefs handoff
        PlayerPrefs.SetString("ObjectToActivate", objectToActivateInScene ?? string.Empty);
        PlayerPrefs.SetString("ControllerToActivate", controllerToActivateInScene ?? string.Empty);
        PlayerPrefs.Save();

        UnityEngine.SceneManagement.SceneManager.LoadScene(targetScene);
    }
}
