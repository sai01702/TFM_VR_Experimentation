using System.Collections;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Management;

public class XRBootstrapper : MonoBehaviour
{
    public static XRBootstrapper Instance { get; private set; }

    void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>
    /// Called by SceneRigInstaller whenever we enter a scene and know the chosen mode.
    /// </summary>
    public void ApplyMode(GameMode mode)
    {
        // In VR: ensure XR is running.
        // In Desktop: stop XR subsystems (but keep loader alive so we can switch back).
        if (mode == GameMode.VR)
            StartCoroutine(StartXR());
        else
            StartCoroutine(StopXR(deinitializeLoader: false));
    }

    IEnumerator StartXR()
    {
        var xr = XRGeneralSettings.Instance?.Manager;
        if (xr == null)
            yield break;

        // Already initialized → just start subsystems.
        if (xr.activeLoader != null)
        {
            xr.StartSubsystems();
            yield break;
        }

        // First-time init
        yield return xr.InitializeLoader();

        if (xr.activeLoader == null)
        {
            Debug.LogError("[XRBootstrapper] Failed to initialize XR Loader.");
            yield break;
        }

        xr.StartSubsystems();
    }

    /// <summary>
    /// Runtime stop. If deinitializeLoader == false, we only stop subsystems.
    /// If true, we fully deinit the loader (used when Play ends).
    /// </summary>
    IEnumerator StopXR(bool deinitializeLoader)
    {
        var xr = XRGeneralSettings.Instance?.Manager;
        if (xr == null || xr.activeLoader == null)
            yield break;

        xr.StopSubsystems();

        if (deinitializeLoader)
        {
            xr.DeinitializeLoader();
        }

        yield return null;
    }

    /// <summary>
    /// Called when the Editor exits Play mode / app quits.
    /// Here we do a *full* stop + deinit so SteamVR knows the app is gone.
    /// </summary>
    void OnApplicationQuit()
    {
        StopXRImmediate();
    }

    // This also runs when exiting Play mode in the Editor.
    void OnDisable()
    {
        if (!Application.isPlaying)
            return;

        StopXRImmediate();
    }

    /// <summary>
    /// Synchronous cleanup used on Play-mode exit.
    /// </summary>
    void StopXRImmediate()
    {
        var xr = XRGeneralSettings.Instance?.Manager;
        if (xr == null || xr.activeLoader == null)
            return;

        try
        {
            xr.StopSubsystems();
            xr.DeinitializeLoader();
            Debug.Log("[XRBootstrapper] XR subsystems and loader fully deinitialized on exit.");
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[XRBootstrapper] Error while deinitializing XR on exit: {e}");
        }
    }
}
