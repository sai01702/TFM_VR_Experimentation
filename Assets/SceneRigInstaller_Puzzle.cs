using UnityEngine;
using UnityEngine.InputSystem;

public class SceneRigInstaller_Puzzle : MonoBehaviour
{
    [Header("Rig Prefabs / Scene References")]
    [SerializeField] private GameObject desktopRigPrefab;          // Normal Desktop rig
    [SerializeField] private GameObject desktopRigPrefab_LargaD;   // Desktop rig for large distance (voice)
    [SerializeField] private GameObject vrRigInScene;              // ✅ Existing XR Origin rig in scene (set inactive in Editor)
    [SerializeField] private GameObject largaObject;               // Object that determines Desktop LargaD
    [SerializeField] private Transform spawnPoint;

    private GameObject currentRig;

    void Start()
    {
        if (GameSettings.Instance.HasSavedMode())
            Install();
        // else: wait until ModeSelectionUI calls ForceInstallNow()
    }

    public void ForceInstallNow()
    {
        if (currentRig != null && currentRig != vrRigInScene)
            Destroy(currentRig);

        Install();
    }

    void Install()
    {
        var mode = GameSettings.Instance.CurrentMode;
        XRBootstrapper.Instance.ApplyMode(mode);

        // --- Decide which rig to use ---
        GameObject prefab = null;
        bool usingSceneVRRig = false;

        if (mode == GameMode.Desktop && largaObject != null && largaObject.activeInHierarchy)
        {
            prefab = desktopRigPrefab_LargaD;
        }
        else if (mode == GameMode.VR)
        {
            prefab = vrRigInScene;  // ✅ use the in-scene VR rig
            usingSceneVRRig = true;
        }
        else
        {
            prefab = desktopRigPrefab;
        }

        if (prefab == null)
        {
            Debug.LogError("[SceneRigInstaller_Puzzle] Rig prefab not assigned in Inspector.");
            return;
        }

        // --- Spawn or activate ---
        if (usingSceneVRRig)
        {
            // Activate existing VR rig instead of instantiating
            if (!vrRigInScene.activeSelf)
            {
                vrRigInScene.SetActive(true);
                Debug.Log("[SceneRigInstaller_Puzzle] Activated existing VR rig in scene.");
            }
            currentRig = vrRigInScene;
        }
        else
        {
            // Instantiate Desktop rig
            if (currentRig != null && currentRig != vrRigInScene)
                Destroy(currentRig);

            var pos = spawnPoint ? spawnPoint.position : Vector3.zero;
            var rot = spawnPoint ? spawnPoint.rotation : Quaternion.identity;
            currentRig = Instantiate(prefab, pos, rot);

            // Ensure active
            if (!currentRig.activeSelf)
            {
                currentRig.SetActive(true);
            }
        }

        // --- Setup Input ---
        var pi = currentRig.GetComponent<PlayerInput>();
        if (pi != null)
        {
            string scheme = (mode == GameMode.VR) ? "XR" : "Desktop"; // ✅ use "XR" (matches Unity's default XR scheme)
            pi.defaultControlScheme = scheme;
            pi.SwitchCurrentControlScheme(scheme);
        }

        // --- Remove extra cameras if any ---
        var cam = Camera.main;
        var rigCam = currentRig.GetComponentInChildren<Camera>();
        if (cam != null && rigCam != null && cam != rigCam)
        {
            Destroy(cam.gameObject);
        }

        Debug.Log($"[SceneRigInstaller_Puzzle] Using {mode} rig: {currentRig.name}");
    }
}
