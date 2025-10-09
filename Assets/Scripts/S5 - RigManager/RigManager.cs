using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using System.Linq;

public class RigManager : MonoBehaviour
{
    public static RigManager Instance { get; private set; }

    [Header("Assign in Inspector or Resources")]
    public GameObject desktopRigPrefab;
    public GameObject vrRigPrefab;

    [Header("Spawn")]
    public Transform customSpawnPoint; // optional; else auto-find

    private GameObject currentRig;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    Transform FindSpawnPoint()
    {
        if (customSpawnPoint) return customSpawnPoint;

        // Try a tagged spawn
        var go = GameObject.FindGameObjectWithTag("PlayerSpawn");
        if (go) return go.transform;

        // Try a named object
        var named = GameObject.Find("SpawnPoint");
        if (named) return named.transform;

        // Fallback: scene origin
        return null;
    }

    GameObject LoadPrefab(GameMode mode)
    {
        if (mode == GameMode.VR)
        {
            if (vrRigPrefab) return vrRigPrefab;
            return Resources.Load<GameObject>("VRRig");
        }
        else
        {
            if (desktopRigPrefab) return desktopRigPrefab;
            return Resources.Load<GameObject>("DesktopRig");
        }
    }

    public IEnumerator DestroyCurrentRig_Co()
    {
        if (currentRig != null)
        {
            Destroy(currentRig);
            currentRig = null;
        }
        // Let destruction finish
        yield return null;
    }

    public IEnumerator SpawnRigForCurrentMode_Co()
    {
        var mode = GameSettings.Instance.CurrentMode;
        var prefab = LoadPrefab(mode);
        if (prefab == null)
        {
            Debug.LogError($"RigManager: Missing prefab for {mode}. Assign in Inspector or place in Resources.");
            yield break;
        }

        var sp = FindSpawnPoint();
        var pos = sp ? sp.position : Vector3.zero;
        var rot = sp ? sp.rotation : Quaternion.identity;

        currentRig = Instantiate(prefab, pos, rot);

        // Explicitly select control scheme to avoid "sometimes Desktop, sometimes VR"
        var pi = currentRig.GetComponent<PlayerInput>();
        if (pi != null)
        {
            if (mode == GameMode.Desktop)
            {
                // Desktop: force KB + Mouse devices to the scheme
                pi.defaultControlScheme = "Desktop";
                pi.SwitchCurrentControlScheme("Desktop", Keyboard.current, Mouse.current);
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
            else
            {
                // VR: prefer letting XRI drive actions; scheme switch is still ok
                pi.defaultControlScheme = "VR";
                pi.SwitchCurrentControlScheme("VR");
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }

        yield return null;
    }

    public IEnumerator EnsureCorrectRig_Co()
    {
        // If the wrong type of rig exists, replace it
        var mode = GameSettings.Instance.CurrentMode;

        if (currentRig != null)
        {
            bool isVRRig = currentRig.name.Contains("XR") || currentRig.name.Contains("VR");
            if ((mode == GameMode.VR && !isVRRig) || (mode == GameMode.Desktop && isVRRig))
                yield return DestroyCurrentRig_Co();
        }

        if (currentRig == null)
            yield return SpawnRigForCurrentMode_Co();
    }
}
