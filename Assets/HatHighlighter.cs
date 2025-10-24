using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

[RequireComponent(typeof(UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable))]
public class HatHighlighter : MonoBehaviour
{
    [Header("Highlight")]
    public Material highlightMaterial;     // assign an emissive/outline-like mat
    public bool includeChildren = true;    // collect child renderers too
    public bool keepHighlightWhileHeld = true;

    UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable grab;
    bool vrHovering;
    bool desktopAiming;
    bool isHeld;

    readonly List<Renderer> renderers = new();
    readonly Dictionary<Renderer, Material[]> originalMats = new();

    void Awake()
    {
        grab = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();

        // VR path events
        grab.hoverEntered.AddListener(OnHoverEntered);
        grab.hoverExited.AddListener(OnHoverExited);
        grab.selectEntered.AddListener(OnSelectEntered);
        grab.selectExited.AddListener(OnSelectExited);

        CacheRenderers();
    }

    void OnDestroy()
    {
        if (grab != null)
        {
            grab.hoverEntered.RemoveListener(OnHoverEntered);
            grab.hoverExited.RemoveListener(OnHoverExited);
            grab.selectEntered.RemoveListener(OnSelectEntered);
            grab.selectExited.RemoveListener(OnSelectExited);
        }
        ClearHighlight(); // restore materials on destroy
    }

    void CacheRenderers()
    {
        renderers.Clear();
        originalMats.Clear();

        if (includeChildren)
            GetComponentsInChildren(true, renderers);
        else
        {
            var r = GetComponent<Renderer>();
            if (r != null) renderers.Add(r);
        }

        foreach (var r in renderers)
            if (r != null) originalMats[r] = r.sharedMaterials;
    }

    // ===== VR events =====
    void OnHoverEntered(HoverEnterEventArgs _)
    {
        vrHovering = true;
        UpdateHighlight();
    }

    void OnHoverExited(HoverExitEventArgs _)
    {
        vrHovering = false;
        UpdateHighlight();
    }

    void OnSelectEntered(SelectEnterEventArgs _)
    {
        isHeld = true;
        UpdateHighlight();
    }

    void OnSelectExited(SelectExitEventArgs _)
    {
        isHeld = false;
        UpdateHighlight();
    }

    // ===== Desktop hooks (called by Desktop scripts) =====
    public void DesktopAimEnter()
    {
        desktopAiming = true;
        UpdateHighlight();
    }

    public void DesktopAimExit()
    {
        desktopAiming = false;
        UpdateHighlight();
    }

    public void DesktopGrabStart()
    {
        isHeld = true;
        UpdateHighlight();
    }

    public void DesktopGrabEnd()
    {
        isHeld = false;
        UpdateHighlight();
    }

    // ===== Core =====
    void UpdateHighlight()
    {
        bool shouldHighlight =
            (vrHovering || desktopAiming) ||
            (keepHighlightWhileHeld && isHeld);

        if (shouldHighlight) ApplyHighlight();
        else ClearHighlight();
    }

    void ApplyHighlight()
    {
        if (highlightMaterial == null) return;

        foreach (var r in renderers)
        {
            if (r == null) continue;
            var src = originalMats.TryGetValue(r, out var mats) ? mats : r.sharedMaterials;
            if (src == null || src.Length == 0) continue;

            var swapped = new Material[src.Length];
            for (int i = 0; i < swapped.Length; i++)
                swapped[i] = highlightMaterial;

            r.sharedMaterials = swapped;
        }
    }

    void ClearHighlight()
    {
        foreach (var kvp in originalMats)
        {
            if (kvp.Key != null && kvp.Value != null)
                kvp.Key.sharedMaterials = kvp.Value;
        }
    }
}