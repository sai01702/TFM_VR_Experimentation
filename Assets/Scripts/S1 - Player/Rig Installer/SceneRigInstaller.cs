using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;

public class SceneRigInstaller : NetworkBehaviour
{
    public GameObject desktopRigPrefab;
    public GameObject vrRigPrefab;
    public Transform spawnPoint;

    void Start()
    {
        // Only auto-install if the player already has a saved mode
        if (GameSettings.Instance.HasSavedMode())
        {
            Install();
        }
        // Otherwise ModeSelectionUI will call ForceInstallNow()
    }

    public void ForceInstallNow()
    {
        Install();
    }

    void Install()
    {
        // Only the host is allowed to spawn the gameplay rig
        if (!NetworkManager.Singleton.IsHost)
            return;

        var mode = GameSettings.Instance.CurrentMode;

        // Apply XR/Desktop mode locally
        XRBootstrapper.Instance.ApplyMode(mode);

        var prefab = mode == GameMode.VR ? vrRigPrefab : desktopRigPrefab;

        if (prefab == null)
        {
            Debug.LogError("Assign rig prefabs on SceneRigInstaller.");
            return;
        }

        Vector3 pos = spawnPoint ? spawnPoint.position : Vector3.zero;
        Quaternion rot = spawnPoint ? spawnPoint.rotation : Quaternion.identity;

        bool isVR = mode == GameMode.VR;

        // Ask server to spawn the rig across the network
        NetworkRigSpawner.Instance.SpawnRigServerRpc(isVR, pos, rot);

        // Remove temporary lobby camera if one exists
        Camera lobbyCam = Camera.main;
        if (lobbyCam != null)
        {
            Destroy(lobbyCam.gameObject);
        }
    }
}