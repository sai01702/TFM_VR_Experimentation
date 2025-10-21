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
        // else: your ModeSelectionUI will call ForceInstallNow() after user chooses.
    }

    public void ForceInstallNow()
    {
        if (currentRig != null) Destroy(currentRig);
        Install();
    }

    void Install()
    {
        var mode = GameSettings.Instance.CurrentMode;
        XRBootstrapper.Instance.ApplyMode(mode);

        // Decide which prefab to use
        GameObject prefab = null;

        if (mode == GameMode.Desktop && vozObject != null && vozObject.activeInHierarchy)
        {
            prefab = desktopRigPrefab_Voz;
        }
        else
        {
            prefab = (mode == GameMode.VR) ? vrRigPrefab : desktopRigPrefab;
        }

        if (prefab == null)
        {
            Debug.LogError("[SceneRigInstaller_Shooter] Assign rig prefabs in the Inspector.");
            return;
        }

        var pos = spawnPoint ? spawnPoint.position : Vector3.zero;
        var rot = spawnPoint ? spawnPoint.rotation : Quaternion.identity;

        if (currentRig != null) Destroy(currentRig);
        currentRig = Instantiate(prefab, pos, rot);

        var pi = currentRig.GetComponent<PlayerInput>();
        if (pi != null)
        {
            var scheme = (mode == GameMode.VR) ? "VR" : "Desktop";
            pi.defaultControlScheme = scheme;
            pi.SwitchCurrentControlScheme(scheme);
        }

        // Remove any temporary camera that isn't the rig camera
        var cam = Camera.main;
        var rigCam = currentRig.GetComponentInChildren<Camera>();
        if (cam != null && rigCam != null && cam != rigCam)
            Destroy(cam.gameObject);
    }
}
