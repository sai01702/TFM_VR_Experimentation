using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

public class SceneRigInstaller_Puzzle : MonoBehaviour
{
    [Header("Rig Prefabs / Scene References")]
    [SerializeField] private GameObject desktopRigPrefab;          // Normal Desktop rig
    [SerializeField] private GameObject desktopRigPrefab_LargaD;   // Desktop rig for large distance
    [SerializeField] private GameObject vrRigInScene;              // Existing XR Origin rig in scene (set inactive in Editor)
    [SerializeField] private GameObject largaObject;               // Object that determines Desktop LargaD
    [SerializeField] private Transform spawnPoint;

    private GameObject currentRig;

    // ✅ Make Start a normal method that kicks off a coroutine
    void Start()
    {
        // We delay one frame so other Start() methods (like ActivateObjectOnStart)
        // can activate largaObject before we decide which rig to spawn.
        StartCoroutine(DelayedInstall());
    }

    private IEnumerator DelayedInstall()
    {
        // Wait one frame
        yield return null;

        if (GameSettings.Instance != null && GameSettings.Instance.HasSavedMode())
        {
            Install();
        }
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

        // ✅ Now largaObject.activeInHierarchy is correct because all Start() have run.
        if (mode == GameMode.Desktop && largaObject != null && largaObject.activeInHierarchy)
        {
            prefab = desktopRigPrefab_LargaD;
            Debug.Log("[SceneRigInstaller_Puzzle] Using Desktop LargaD rig (largaObject is active).");
        }
        else if (mode == GameMode.VR)
        {
            prefab = vrRigInScene;  // use the in-scene VR rig
            usingSceneVRRig = true;
            Debug.Log("[SceneRigInstaller_Puzzle] Using VR rig in scene.");
        }
        else
        {
            prefab = desktopRigPrefab;
            Debug.Log("[SceneRigInstaller_Puzzle] Using normal Desktop rig.");
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
            string scheme = (mode == GameMode.VR) ? "XR" : "Desktop"; // or "VR" if that's your control scheme name
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