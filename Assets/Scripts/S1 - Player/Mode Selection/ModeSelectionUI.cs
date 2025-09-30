using UnityEngine;
using UnityEngine.UI;

public class ModeSelectionUI : MonoBehaviour
{
    [Header("Assign in Inspector")]
    [SerializeField] private GameObject panel;     // ModeSelectPanel
    [SerializeField] private Button vrButton;      // VRButton (Button component)
    [SerializeField] private Button desktopButton; // DesktopButton (Button component)

    [Header("Optional")]
    [SerializeField] private SceneRigInstaller sceneRigInstaller; // if you have it

    void Awake()
    {
        if (GameSettings.Instance == null)
            new GameObject("GameSettings").AddComponent<GameSettings>();
        if (XRBootstrapper.Instance == null)
            new GameObject("XRBootstrapper").AddComponent<XRBootstrapper>();
    }

    void Start()
    {
        vrButton.onClick.AddListener(() => Select(GameMode.VR));
        desktopButton.onClick.AddListener(() => Select(GameMode.Desktop));

        if (GameSettings.Instance.HasSavedMode())
        {
            var mode = GameSettings.Instance.CurrentMode;
            XRBootstrapper.Instance.ApplyMode(mode);
            panel.SetActive(false);
            sceneRigInstaller?.ForceInstallNow();
            panel.SetActive(true); // this one !
        }
        else
        {
          //  panel.SetActive(true); need to uncomment this when game is ready to publish, dont forget to delete one at top
        }
    }

    void Select(GameMode mode)
    {
        GameSettings.Instance.SetMode(mode);
        XRBootstrapper.Instance.ApplyMode(mode);
        panel.SetActive(false);
        sceneRigInstaller?.ForceInstallNow();
    }
}