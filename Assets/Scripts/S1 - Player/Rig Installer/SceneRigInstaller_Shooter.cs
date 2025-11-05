using UnityEngine;
using UnityEngine.InputSystem;

public class SceneRigInstaller_Shooter : MonoBehaviour
{
    [Header("Rig Prefabs")]
    [SerializeField] private GameObject desktopRigPrefab;          // normal Desktop rig
    [SerializeField] private GameObject desktopRigPrefab_Voz;      // Desktop rig for voice control
    [SerializeField] private GameObject vrRigPrefab;               // XR Origin rig

    [Header("Scene References")]
    [SerializeField] private GameObject vozObject;                 // if active -> use voice desktop rig
    [SerializeField] private Transform spawnPoint;

    private GameObject currentRig;

    void Start()
    {
        if (GameSettings.Instance.HasSavedMode())
            Install();
        // else ModeSelectionUI will call ForceInstallNow()
    }

    public void ForceInstallNow()
    {
        if (currentRig != null)
            Destroy(currentRig);

        Install();
    }

    void Install()
    {
        var mode = GameSettings.Instance.CurrentMode;
        XRBootstrapper.Instance.ApplyMode(mode);

        // --- pick rig prefab ---
        GameObject prefab = null;

        if (mode == GameMode.Desktop && vozObject != null && vozObject.activeInHierarchy)
        {
            // desktop with voice
            prefab = desktopRigPrefab_Voz;
        }
        else
        {
            // either normal desktop or vr
            prefab = (mode == GameMode.VR) ? vrRigPrefab : desktopRigPrefab;
        }

        if (prefab == null)
        {
            Debug.LogError("[SceneRigInstaller_Shooter] Assign rig prefabs in the Inspector.");
            return;
        }

        // --- spawn rig ---
        var pos = spawnPoint ? spawnPoint.position : Vector3.zero;
        var rot = spawnPoint ? spawnPoint.rotation : Quaternion.identity;

        if (currentRig != null)
            Destroy(currentRig);

        currentRig = Instantiate(prefab, pos, rot);

        // --- set control scheme on PlayerInput if present ---
        var pi = currentRig.GetComponent<PlayerInput>();
        if (pi != null)
        {
            var scheme = (mode == GameMode.VR) ? "VR" : "Desktop";
            pi.defaultControlScheme = scheme;
            pi.SwitchCurrentControlScheme(scheme);
        }

        // --- kill any leftover temp camera (lobby cam etc.) ---
        var cam = Camera.main;
        var rigCam = currentRig.GetComponentInChildren<Camera>();
        if (cam != null && rigCam != null && cam != rigCam)
        {
            Destroy(cam.gameObject);
        }

        // --- NEW: activate the correct sub-objects for this rig ---
        ApplyControllerSelectionToRig(currentRig);
    }

    /// <summary>
    /// Reads PlayerPrefs ("ObjectToActivate", "ControllerToActivate")
    /// and activates those objects INSIDE the spawned rig.
    /// Works for both Desktop rigs and VR rig.
    /// </summary>
    void ApplyControllerSelectionToRig(GameObject rigRoot)
    {
        if (rigRoot == null) return;

        string objectName = PlayerPrefs.GetString("ObjectToActivate", "");
        string controllerName = PlayerPrefs.GetString("ControllerToActivate", "");

        // Example:
        //  objectName:       "V1_Basic_Version_Controller"
        //  controllerName:   "Right Controller"  (or whatever you stored)

        if (!string.IsNullOrEmpty(objectName))
        {
            var obj = FindChildRecursive(rigRoot.transform, objectName);
            if (obj != null)
            {
                obj.SetActive(true);
                Debug.Log($"[RigInstall] Activated object in rig: {objectName}");
            }
            else
            {
                Debug.LogWarning($"[RigInstall] Could not find '{objectName}' under rig '{rigRoot.name}'");
            }
        }

        if (!string.IsNullOrEmpty(controllerName))
        {
            var ctrl = FindChildRecursive(rigRoot.transform, controllerName);
            if (ctrl != null)
            {
                ctrl.SetActive(true);
                Debug.Log($"[RigInstall] Activated controller in rig: {controllerName}");
            }
            else
            {
                Debug.LogWarning($"[RigInstall] Could not find controller '{controllerName}' under rig '{rigRoot.name}'");
            }
        }
    }

    /// <summary>
    /// Recursively search by name in children of root.
    /// (Works even if the object is inactive in the prefab.)
    /// </summary>
    GameObject FindChildRecursive(Transform root, string targetName)
    {
        foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
        {
            if (t.name == targetName)
                return t.gameObject;
        }
        return null;
    }
}
