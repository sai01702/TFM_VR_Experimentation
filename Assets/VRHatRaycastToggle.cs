using UnityEngine;

public class VRHatRaycastToggle : MonoBehaviour
{
    [Header("Long-distance flag object")]
    [SerializeField] private GameObject largaObject;   // drag your V2_Larga_Distancia here

    [Header("Hats that use PlaceHatWithRaycast")]
    [SerializeField] private PlaceHatWithRaycast[] hats;   // leave empty to auto-find

    bool lastState = false;

    void Awake()
    {
        // Auto-find all hats in the scene if you didn’t assign them manually
        if (hats == null || hats.Length == 0)
        {
            hats = FindObjectsOfType<PlaceHatWithRaycast>(true);
        }

        ApplyState(ShouldBeEnabled());
    }

    void Update()
    {
        bool state = ShouldBeEnabled();
        if (state != lastState)
        {
            ApplyState(state);
        }
    }

    bool ShouldBeEnabled()
    {
        // need GameSettings, in VR mode, and largaObject active
        if (GameSettings.Instance == null) return false;

        return GameSettings.Instance.CurrentMode == GameMode.VR &&
               largaObject != null &&
               largaObject.activeInHierarchy;
    }

    void ApplyState(bool enable)
    {
        lastState = enable;

        if (hats == null) return;

        foreach (var h in hats)
        {
            if (h == null) continue;
            h.enabled = enable;
        }
    }
}