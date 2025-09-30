using System.Collections;
using UnityEngine;
using UnityEngine.XR.Management;

public class XRBootstrapper : MonoBehaviour
{
    public static XRBootstrapper Instance { get; private set; }

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void ApplyMode(GameMode mode)
    {
        if (mode == GameMode.VR) StartCoroutine(StartXR());
        else StartCoroutine(StopXR());
    }

    IEnumerator StartXR()
    {
        var xr = XRGeneralSettings.Instance?.Manager;
        if (xr == null) yield break;

        if (xr.activeLoader != null) { xr.StartSubsystems(); yield break; }

        yield return xr.InitializeLoader();
        if (xr.activeLoader == null) { Debug.LogError("Failed to init XR Loader"); yield break; }
        xr.StartSubsystems();
    }

    IEnumerator StopXR()
    {
        var xr = XRGeneralSettings.Instance?.Manager;
        if (xr?.activeLoader != null)
        {
            xr.StopSubsystems();
            xr.DeinitializeLoader();
        }
        yield return null;
    }
}