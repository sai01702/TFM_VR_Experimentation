using System.Collections;
using UnityEngine;
using UnityEngine.XR.Management;

public class XRService : MonoBehaviour
{
    public static XRService Instance { get; private set; }
    public bool IsRunning { get; private set; }

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public Coroutine StartXR() => StartCoroutine(StartXR_Coroutine());
    public Coroutine StopXR() => StartCoroutine(StopXR_Coroutine());

    IEnumerator StartXR_Coroutine()
    {
        var mgr = XRGeneralSettings.Instance?.Manager;
        if (mgr == null) yield break;

        if (mgr.activeLoader == null)
        {
            yield return mgr.InitializeLoader();
            if (mgr.activeLoader == null) yield break;
        }

        mgr.StartSubsystems();
        // small yield to let subsystems fully come up
        yield return null;
        IsRunning = true;
    }

    IEnumerator StopXR_Coroutine()
    {
        var mgr = XRGeneralSettings.Instance?.Manager;
        if (mgr?.activeLoader != null)
        {
            mgr.StopSubsystems();
            mgr.DeinitializeLoader();
            // let cleanup settle
            yield return null;
        }
        IsRunning = false;
    }
}
