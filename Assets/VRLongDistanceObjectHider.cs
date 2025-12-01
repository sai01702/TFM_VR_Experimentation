using System.Collections.Generic;
using UnityEngine;

public class VRLongDistanceObjectHider : MonoBehaviour
{
    [Header("Mode selector")]
    [Tooltip("Same LONG-DISTANCE flag object you use in SceneRigInstaller (e.g. V2_Larga_Distancia).")]
    public GameObject largaObject;

    [Header("Objects to hide when long-distance VR is active")]
    public List<GameObject> targets = new List<GameObject>();

    [Tooltip("Only apply hiding when in VR mode. If false, just checks largaObject.")]
    public bool onlyWhenVR = true;

    // store original active states so we can restore them
    private Dictionary<GameObject, bool> originalStates = new Dictionary<GameObject, bool>();

    void Awake()
    {
        // cache initial activeSelf for every target
        originalStates.Clear();
        foreach (var go in targets)
        {
            if (go == null) continue;
            if (!originalStates.ContainsKey(go))
                originalStates.Add(go, go.activeSelf);
        }
    }

    void Update()
    {
        bool shouldHide = ShouldHide();

        foreach (var go in targets)
        {
            if (go == null) continue;

            // if we never cached it for some reason, add now
            if (!originalStates.ContainsKey(go))
                originalStates[go] = go.activeSelf;

            if (shouldHide)
            {
                // hide
                if (go.activeSelf)
                    go.SetActive(false);
            }
            else
            {
                // restore original state
                bool original;
                if (originalStates.TryGetValue(go, out original))
                {
                    if (go.activeSelf != original)
                        go.SetActive(original);
                }
            }
        }
    }

    bool ShouldHide()
    {
        // is VR?
        bool isVR = true;
        if (onlyWhenVR)
        {
            isVR = GameSettings.Instance != null &&
                   GameSettings.Instance.CurrentMode == GameMode.VR;
        }

        // is long-distance flag active?
        bool longDistance = largaObject != null && largaObject.activeInHierarchy;

        return isVR && longDistance;
    }
}