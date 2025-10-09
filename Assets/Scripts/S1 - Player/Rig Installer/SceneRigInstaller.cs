using UnityEngine;
using UnityEngine.InputSystem;

public class SceneRigInstaller : MonoBehaviour
{
    public GameObject desktopRigPrefab; // your Desktop Rig prefab
    public GameObject vrRigPrefab;      // your XR Origin (Action-based) prefab
    public Transform spawnPoint;

    private GameObject currentRig;

    void Start()
    {
        // Only auto-install if the player already has a saved mode
        if (GameSettings.Instance.HasSavedMode())
        {
            Install();
        }
        // else: wait. ModeSelectionUI will call ForceInstallNow() after the user chooses.
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

        var prefab = mode == GameMode.VR ? vrRigPrefab : desktopRigPrefab;
        if (prefab == null)
        {
            Debug.LogError("Assign rig prefabs on SceneRigInstaller.");
            return;
        }

        var pos = spawnPoint ? spawnPoint.position : Vector3.zero;
        var rot = spawnPoint ? spawnPoint.rotation : Quaternion.identity;
        currentRig = Instantiate(prefab, pos, rot);

        var pi = currentRig.GetComponent<PlayerInput>();
        if (pi != null)
        {
            var scheme = mode == GameMode.VR ? "VR" : "Desktop";
            pi.defaultControlScheme = scheme;
            pi.SwitchCurrentControlScheme(scheme);
        }

        // 🔥 Destroy the temporary lobby camera (if one exists)
        var lobbyCam = Camera.main;
        if (lobbyCam != null && !currentRig.GetComponentInChildren<Camera>())
        {
            Destroy(lobbyCam.gameObject);
        }
    }
}
