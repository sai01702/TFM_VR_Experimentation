using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class ModeSelectionUI : MonoBehaviour
{
    [Header("Assign in Inspector")]
    [SerializeField] private GameObject panel;           // ModeSelectPanel
    [SerializeField] private Button vrButton;            // VRButton (Button component)
    [SerializeField] private Button desktopButton;       // DesktopButton (Button component)
    [SerializeField] private SceneRigInstaller sceneRigInstaller; // your SceneRigInstaller in scene

    [Header("Optional")]
    [Tooltip("If you keep a minimal 'UI-only' XR Origin in the scene for pointing at the menu, drag it here to auto-destroy after selection.")]
    [SerializeField] private GameObject uiOnlyXROrigin;  // (optional)

    private bool tempXRStarted;

    void Awake()
    {
        // Ensure the singletons exist
        if (GameSettings.Instance == null)
            new GameObject("GameSettings").AddComponent<GameSettings>();
        if (XRBootstrapper.Instance == null)
            new GameObject("XRBootstrapper").AddComponent<XRBootstrapper>();
    }

    void OnEnable()
    {
        vrButton.onClick.AddListener(OnChooseVR);
        desktopButton.onClick.AddListener(OnChooseDesktop);
    }

    void OnDisable()
    {
        vrButton.onClick.RemoveListener(OnChooseVR);
        desktopButton.onClick.RemoveListener(OnChooseDesktop);
    }

    void Start()
    {
        // If player already chose before → auto-apply + spawn rig, hide panel
        if (GameSettings.Instance.HasSavedMode())
        {
            var saved = GameSettings.Instance.CurrentMode;
            XRBootstrapper.Instance.ApplyMode(saved);
            panel.SetActive(false);
            sceneRigInstaller?.ForceInstallNow();

            panel.SetActive(true);

            return;
        }

        // First time: show the menu
        panel.SetActive(true);

        // Start XR TEMPORARILY so VR users can point at the UI before choosing.
        // (SceneRigInstaller will NOT spawn any gameplay rig yet, so no movement.)
        XRBootstrapper.Instance.ApplyMode(GameMode.VR);
        tempXRStarted = true;

        // Allow both device families while picking
        InputDeviceGate.EnableDesktop(true);
        InputDeviceGate.EnableXR(true);

        // Make sure desktop has a cursor while choosing
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    void OnChooseVR() => Select(GameMode.VR);
    void OnChooseDesktop() => Select(GameMode.Desktop);

    void Select(GameMode mode)
    {
        // Save mode
        GameSettings.Instance.SetMode(mode);

        // Gate devices + start/stop XR appropriately
        if (mode == GameMode.VR)
        {
            // Keep XR running (or start if it wasn't), disable desktop
            InputDeviceGate.EnableDesktop(false);
            InputDeviceGate.EnableXR(true);
            XRBootstrapper.Instance.ApplyMode(GameMode.VR);
        }
        else // Desktop
        {
            // Stop XR and disable XR devices; enable desktop
            InputDeviceGate.EnableXR(false);
            InputDeviceGate.EnableDesktop(true);
            XRBootstrapper.Instance.ApplyMode(GameMode.Desktop);
        }

        // If we had a temporary UI-only XR Origin, remove it now
        if (uiOnlyXROrigin != null) Destroy(uiOnlyXROrigin);

        // Hide menu, spawn correct rig
        panel.SetActive(false);
        sceneRigInstaller?.ForceInstallNow();

        // Desktop: lock cursor again if your controller script expects it
        if (mode == GameMode.Desktop)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        tempXRStarted = false;
    }
}
