using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Canvas))]
public class CanvasAlwaysWorldSpace : MonoBehaviour
{
    [Header("Placement")]
    public bool attachToCamera = true;  // follow the active MainCamera
    public float distanceFromCamera = 2f; // meters in front when attached
    public Vector3 worldOffset = Vector3.zero; // add if you don't attach

    [Header("Layer & Sorting (recommended)")]
    public bool putOnUILayer = true;    // put canvas & children on "UI" layer
    public string uiLayerName = "UI";
    public bool overrideSorting = true;
    public string sortingLayerName = "Default"; // or "UI" if you created a Sorting Layer
    public int sortingOrder = 0;

    Canvas _canvas;
    Coroutine _co;

    void Awake() => _canvas = GetComponent<Canvas>();

    void OnEnable()
    {
        if (_co != null) StopCoroutine(_co);
        _co = StartCoroutine(BindWorldSpace());
    }

    void OnDisable()
    {
        if (_co != null) StopCoroutine(_co);
        _co = null;
    }

    IEnumerator BindWorldSpace()
    {
        // Wait for a camera (rig spawner / scene camera)
        while (Camera.main == null) yield return null;

        var cam = Camera.main;

        // Force World Space, always
        _canvas.renderMode = RenderMode.WorldSpace;
        _canvas.worldCamera = cam;

        // Optional: layer setup so the camera can see the UI
        if (putOnUILayer)
        {
            int uiLayer = LayerMask.NameToLayer(uiLayerName);
            if (uiLayer >= 0) SetLayerRecursively(gameObject, uiLayer);

            // ensure camera renders that layer
            if (uiLayer >= 0 && (cam.cullingMask & (1 << uiLayer)) == 0)
                cam.cullingMask |= (1 << uiLayer);
        }

        // Sorting (prevents other systems from bumping it)
        _canvas.overrideSorting = overrideSorting;
        if (overrideSorting)
        {
            int sid = SortingLayer.GetLayerValueFromName(sortingLayerName) == -1
                ? SortingLayer.NameToID("Default")
                : SortingLayer.NameToID(sortingLayerName);
            _canvas.sortingLayerID = sid;
            _canvas.sortingOrder = sortingOrder;
        }

        // Placement
        var rt = (RectTransform)transform;

        if (attachToCamera)
        {
            transform.SetParent(cam.transform, worldPositionStays: false);
            rt.localPosition = new Vector3(0, 0, Mathf.Max(0.05f, distanceFromCamera));
            rt.localRotation = Quaternion.identity;
        }
        else
        {
            // stay in world; just nudge to a safe spot if you want
            transform.SetParent(null, worldPositionStays: true);
            if (worldOffset != Vector3.zero)
                transform.position = cam.transform.position + cam.transform.forward * distanceFromCamera + worldOffset;
        }

        // World-space UI should be tiny scale (meters)
        if (rt.localScale.x > 0.01f) rt.localScale = Vector3.one * 0.001f;

        // Prevent near-clip from cutting the UI if it’s close
        if (cam.nearClipPlane > 0.05f) cam.nearClipPlane = 0.03f;
    }

    static void SetLayerRecursively(GameObject go, int layer)
    {
        if (layer < 0) return;
        go.layer = layer;
        foreach (Transform c in go.transform) SetLayerRecursively(c.gameObject, layer);
    }
}
