using UnityEngine;
using UnityEngine.InputSystem;

public class SceneRigInstaller_ObstacleCourse : MonoBehaviour
{
    [Header("Rig Prefabs / Scene References")]
    [SerializeField] private GameObject desktopRigPrefab;   // prefab for Desktop
    [SerializeField] private GameObject vrRigInScene;       // XR Origin already placed in this scene
    [SerializeField] private Transform spawnPoint;

    private GameObject currentRig;

    private void Start()
    {
        if (GameSettings.Instance != null && GameSettings.Instance.HasSavedMode())
        {
            Install();
        }
        // Otherwise ModeSelectionUI will call ForceInstallNow() after user chooses.
    }

    public void ForceInstallNow()
    {
        // Never destroy the in-scene VR rig, only any previous spawned desktop rig.
        if (currentRig != null && currentRig != vrRigInScene)
        {
            Destroy(currentRig);
        }

        Install();
    }

    private void Install()
    {
        if (GameSettings.Instance == null)
        {
            Debug.LogError("[SceneRigInstaller_ObstacleCourse] GameSettings.Instance is null.");
            return;
        }

        var mode = GameSettings.Instance.CurrentMode;
        XRBootstrapper.Instance?.ApplyMode(mode);

        bool usingSceneVRRig = false;
        GameObject chosenRig = null;

        if (mode == GameMode.VR)
        {
            // Use the XR Origin that is already in the scene
            if (vrRigInScene == null)
            {
                Debug.LogError("[SceneRigInstaller_ObstacleCourse] vrRigInScene reference is missing.");
                return;
            }

            vrRigInScene.SetActive(true);
            chosenRig = vrRigInScene;
            usingSceneVRRig = true;
        }
        else
        {
            // Desktop mode → spawn the desktop rig prefab
            if (desktopRigPrefab == null)
            {
                Debug.LogError("[SceneRigInstaller_ObstacleCourse] desktopRigPrefab is not assigned.");
                return;
            }

            // Clean up any previous spawned rig (but never the in-scene VR rig)
            if (currentRig != null && currentRig != vrRigInScene)
                Destroy(currentRig);

            var pos = spawnPoint ? spawnPoint.position : Vector3.zero;
            var rot = spawnPoint ? spawnPoint.rotation : Quaternion.identity;
            chosenRig = Instantiate(desktopRigPrefab, pos, rot);
            chosenRig.SetActive(true);
        }

        currentRig = chosenRig;

        // ----- Input control scheme -----
        var pi = currentRig.GetComponent<PlayerInput>();
        if (pi != null)
        {
            string scheme = (mode == GameMode.VR) ? "VR" : "Desktop";
            pi.defaultControlScheme = scheme;
            pi.SwitchCurrentControlScheme(scheme);
        }

        // ----- Remove any temporary camera (e.g. from login / menu scene) -----
        var mainCam = Camera.main;
        var rigCam = currentRig.GetComponentInChildren<Camera>();

        if (mainCam != null && rigCam != null && mainCam != rigCam)
        {
            Destroy(mainCam.gameObject);
        }

        Debug.Log($"[SceneRigInstaller_ObstacleCourse] Mode: {mode}, using rig: {currentRig.name}, scene VR: {usingSceneVRRig}");
    }
}