using System.Collections.Generic;
using UnityEngine;

public class VRLongDistanceObjectActivator : MonoBehaviour
{
    [Header("Mode selector")]
    [Tooltip("Your LONG-DISTANCE flag object (example: V2_Larga_Distancia).")]
    public GameObject largaObject;

    [Header("Objects to activate only during Long Distance VR")]
    public List<GameObject> targets = new List<GameObject>();

    [Tooltip("If true, activation only happens when GameMode is VR.")]
    public bool onlyWhenVR = true;

    // store starting active states so they can be restored
    private Dictionary<GameObject, bool> originalStates = new Dictionary<GameObject, bool>();

    void Awake()
    {
        originalStates.Clear();
        foreach (var go in targets)
        {
            if (go == null) continue;
            originalStates[go] = go.activeSelf;
        }
    }

    void Update()
    {
        bool shouldActivate = ShouldActivate();

        foreach (var go in targets)
        {
            if (go == null) continue;

            // make sure original state is saved
            if (!originalStates.ContainsKey(go))
                originalStates[go] = go.activeSelf;

            if (shouldActivate)
            {
                // 🔥 ACTIVATION MODE
                if (!go.activeSelf)
                    go.SetActive(true);
            }
            else
            {
                // 🔄 RESTORE original state
                if (originalStates.TryGetValue(go, out bool original))
                {
                    if (go.activeSelf != original)
                        go.SetActive(original);
                }
            }
        }
    }

    bool ShouldActivate()
    {
        // check VR mode
        bool isVR = true;
        if (onlyWhenVR)
        {
            isVR = GameSettings.Instance != null &&
                   GameSettings.Instance.CurrentMode == GameMode.VR;
        }

        // check long-distance flag
        bool longDistance = largaObject != null && largaObject.activeInHierarchy;

        return isVR && longDistance;
    }
}