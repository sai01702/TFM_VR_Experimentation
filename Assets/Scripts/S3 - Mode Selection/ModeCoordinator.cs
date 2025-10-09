using System.Collections;
using UnityEngine;

public class ModeCoordinator : MonoBehaviour
{
    public static ModeCoordinator Instance { get; private set; }
    public bool IsApplying { get; private set; }

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (GameSettings.Instance == null)
            new GameObject("GameSettings").AddComponent<GameSettings>();
        if (XRService.Instance == null)
            new GameObject("XRService").AddComponent<XRService>();
        if (RigManager.Instance == null)
            new GameObject("RigManager").AddComponent<RigManager>();
    }

    public void SelectAndApply(GameMode mode)
    {
        if (!IsApplying) StartCoroutine(ApplyMode_Co(mode));
    }

    IEnumerator ApplyMode_Co(GameMode mode)
    {
        IsApplying = true;

        // 1) Stop both input paths (destroy rig), then switch XR state
        yield return RigManager.Instance.DestroyCurrentRig_Co();

        if (mode == GameMode.VR)
        {
            yield return XRService.Instance.StartXR();
        }
        else
        {
            yield return XRService.Instance.StopXR();
        }

        // 2) Save choice
        GameSettings.Instance.SetMode(mode);

        // 3) Spawn correct rig AFTER XR state is correct
        yield return RigManager.Instance.SpawnRigForCurrentMode_Co();

        IsApplying = false;
    }

    // Call on scene load to enforce consistency (e.g., from a scene bootstrap)
    public void EnsureAppliedForSavedMode()
    {
        if (IsApplying) return;
        StartCoroutine(EnsureApplied_Co());
    }

    IEnumerator EnsureApplied_Co()
    {
        IsApplying = true;

        // Ensure XR matches saved mode
        var target = GameSettings.Instance.CurrentMode;
        if (target == GameMode.VR && !XRService.Instance.IsRunning)
            yield return XRService.Instance.StartXR();
        if (target == GameMode.Desktop && XRService.Instance.IsRunning)
            yield return XRService.Instance.StopXR();

        // Ensure correct rig
        yield return RigManager.Instance.EnsureCorrectRig_Co();

        IsApplying = false;
    }
}
