using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

/// <summary>
/// For VR long‑distance mode: highlights only the stand meshes you assign,
/// while this ray interactor is holding a hat.
/// Attach this to the same GameObject that has the XRRayInteractor.
/// </summary>
public class VRRayStandHighlighter : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private XRRayInteractor rayInteractor;

    [Header("Stands to Highlight")]
    [Tooltip("Drag each stand's MeshRenderer here. The script will highlight these when pointed at.")]
    [SerializeField] private List<Renderer> standRenderers = new List<Renderer>();

    [Header("Detection Colliders (Optional)")]
    [Tooltip("Optional: Use separate colliders for easier detection. Must match standRenderers order.")]
    [SerializeField] private List<Collider> detectionColliders = new List<Collider>();

    [Header("Raycast Settings")]
    [Tooltip("Layer mask for stand detection raycast.")]
    [SerializeField] private LayerMask standLayerMask = ~0;
    
    [Tooltip("Max raycast distance.")]
    [SerializeField] private float maxDistance = 20f;

    [Header("Visuals")]
    [Tooltip("Material to apply on the stand while it is targeted.")]
    [SerializeField] private Material highlightMaterial;

    [Header("Behaviour")]
    [Tooltip("Only highlight when the ray is actually holding a hat.")]
    [SerializeField] private bool requireHeldHat = true;

    [Header("Debug")]
    [SerializeField] private bool showDebugRay = false;

    private Renderer currentHighlightedRenderer;
    private Material savedOriginalMaterial;

    void Reset()
    {
        if (rayInteractor == null)
            rayInteractor = GetComponent<XRRayInteractor>();
    }

    void OnDisable()
    {
        ClearHighlight();
    }

    void Update()
    {
        if (rayInteractor == null || highlightMaterial == null)
        {
            ClearHighlight();
            return;
        }

        if (standRenderers.Count == 0)
        {
            ClearHighlight();
            return;
        }

        if (requireHeldHat && !IsHoldingHat())
        {
            ClearHighlight();
            return;
        }

        // Do our own raycast from the ray interactor
        Transform rayOrigin = rayInteractor.transform;
        Vector3 rayDirection = rayOrigin.forward;
        
        // Try to get the actual ray endpoint direction if available
        if (rayInteractor.TryGetCurrent3DRaycastHit(out RaycastHit xrHit))
        {
            rayDirection = (xrHit.point - rayOrigin.position).normalized;
        }

        Ray ray = new Ray(rayOrigin.position, rayDirection);

        if (showDebugRay)
            Debug.DrawRay(ray.origin, ray.direction * maxDistance, Color.yellow);

        // Raycast to find stands
        RaycastHit[] hits = Physics.RaycastAll(ray, maxDistance, standLayerMask, QueryTriggerInteraction.Collide);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        Renderer foundRenderer = null;

        foreach (var hit in hits)
        {
            // Skip hats
            if (hit.collider.CompareTag("Hat"))
                continue;

            // Check if this hit matches any of our detection colliders
            int colliderIndex = detectionColliders.IndexOf(hit.collider);
            if (colliderIndex >= 0 && colliderIndex < standRenderers.Count)
            {
                foundRenderer = standRenderers[colliderIndex];
                if (showDebugRay) Debug.Log($"[Highlighter] Hit detection collider {hit.collider.name} → {foundRenderer.name}");
                break;
            }

            // Check if hit object has one of our renderers
            Renderer hitRenderer = hit.collider.GetComponent<Renderer>();
            if (hitRenderer != null && standRenderers.Contains(hitRenderer))
            {
                foundRenderer = hitRenderer;
                if (showDebugRay) Debug.Log($"[Highlighter] Hit renderer directly: {foundRenderer.name}");
                break;
            }

            // Check parent
            hitRenderer = hit.collider.GetComponentInParent<Renderer>();
            if (hitRenderer != null && standRenderers.Contains(hitRenderer))
            {
                foundRenderer = hitRenderer;
                if (showDebugRay) Debug.Log($"[Highlighter] Hit parent renderer: {foundRenderer.name}");
                break;
            }

            // Check children
            var childRenderers = hit.collider.GetComponentsInChildren<Renderer>();
            foreach (var cr in childRenderers)
            {
                if (standRenderers.Contains(cr))
                {
                    foundRenderer = cr;
                    if (showDebugRay) Debug.Log($"[Highlighter] Hit child renderer: {foundRenderer.name}");
                    break;
                }
            }
            if (foundRenderer != null) break;
        }

        // Apply or clear highlight
        if (foundRenderer != null)
        {
            SetHighlight(foundRenderer);
        }
        else
        {
            ClearHighlight();
        }
    }

    bool IsHoldingHat()
    {
        if (!rayInteractor.hasSelection)
            return false;

        foreach (var sel in rayInteractor.interactablesSelected)
        {
            if (sel is XRGrabInteractable grab &&
                (grab.CompareTag("Hat") || grab.GetComponent<PlaceHatWithRaycast>() != null))
                return true;
        }

        return false;
    }

    void SetHighlight(Renderer targetRenderer)
    {
        if (targetRenderer == currentHighlightedRenderer)
            return;

        // Clear previous
        ClearHighlight();

        if (targetRenderer == null)
            return;

        // Save and apply new
        currentHighlightedRenderer = targetRenderer;
        savedOriginalMaterial = targetRenderer.material;
        targetRenderer.material = highlightMaterial;

        if (showDebugRay)
            Debug.Log($"[Highlighter] Highlighting: {targetRenderer.name}");
    }

    void ClearHighlight()
    {
        if (currentHighlightedRenderer != null && savedOriginalMaterial != null)
        {
            currentHighlightedRenderer.material = savedOriginalMaterial;
        }

        currentHighlightedRenderer = null;
        savedOriginalMaterial = null;
    }

    [ContextMenu("Auto-Find Hat Stands")]
    public void AutoFindHatStands()
    {
        standRenderers.Clear();
        detectionColliders.Clear();

        var allStands = FindObjectsOfType<HatStandTrigger>(true);

        foreach (var stand in allStands)
        {
            var col = stand.GetComponent<Collider>();
            var rend = stand.GetComponent<Renderer>();

            if (rend == null)
                rend = stand.GetComponentInChildren<Renderer>();

            if (rend != null)
            {
                standRenderers.Add(rend);
                
                // Add collider if exists (for detection)
                if (col != null)
                    detectionColliders.Add(col);
            }
        }

        Debug.Log($"[VRRayStandHighlighter] Found {standRenderers.Count} stands.");
    }
}
