
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class DisableLocomotionForLongVR : MonoBehaviour
{
    [Header("Mode selector")]
    [Tooltip("Same LONG-DISTANCE object you use in SceneRigInstaller (V2_Larga_Distancia, etc).")]
    public GameObject largaObject;

    // All movement providers found under this XR Origin
    private ContinuousMoveProviderBase[] moveProviders;

    void Awake()
    {
        // Find any move providers in this rig (usually on Locomotion System)
        moveProviders = GetComponentsInChildren<ContinuousMoveProviderBase>(true);
        if (moveProviders == null || moveProviders.Length == 0)
            Debug.LogWarning("[VRLongDistanceMoveDisabler] No ContinuousMoveProviderBase found under XR Origin.");
    }

    void Update()
    {
        bool disableMove = ShouldDisableMove();

        if (moveProviders == null) return;

        foreach (var mp in moveProviders)
        {
            if (mp == null) continue;
            mp.enabled = !disableMove;       // disable ONLY movement
        }
    }

    bool ShouldDisableMove()
    {
        // must be VR mode
        bool isVR = GameSettings.Instance != null &&
                    GameSettings.Instance.CurrentMode == GameMode.VR;

        // and long-distance flag object must be active
        bool longDistance = largaObject != null && largaObject.activeInHierarchy;

        return isVR && longDistance;
    }
}