using UnityEngine;
using UnityEngine.InputSystem;

public class DesktopLongDistanceHatPlacer : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Desktop camera used for aiming the ray. If left empty, will try Camera.main.")]
    public Camera cam;

    [Tooltip("The long-distance grabber script on this rig.")]
    public DesktopGrabberLongDistance desktopGrabberLongDistance;

    [Header("Raycast")]
    [Tooltip("Max distance for aiming at a HatStand.")]
    public float maxDistance = 8f;

    [Tooltip("LayerMask that contains the HatStandTrigger colliders.")]
    public LayerMask standLayerMask;

    [Header("Highlight")]
    [Tooltip("Material used to highlight the stand being aimed at.")]
    public Material highlightMaterial;

    [Tooltip("Input action for interaction (E key). Use the same action as your Desktop grab/use.")]
    public InputActionReference interactAction;

    private HatStandTrigger currentStand;
    private Renderer currentRenderer;
    private Material originalMaterial;

    void Awake()
    {
        if (desktopGrabberLongDistance == null)
            desktopGrabberLongDistance = GetComponentInChildren<DesktopGrabberLongDistance>();
    }

    void OnEnable()
    {
        if (interactAction != null)
        {
            interactAction.action.performed += OnInteract;
            interactAction.action.Enable();
        }
    }

    void OnDisable()
    {
        if (interactAction != null)
        {
            interactAction.action.performed -= OnInteract;
            interactAction.action.Disable();
        }

        ClearHighlight();
    }

    void Update()
    {
        // Only run for Desktop mode
        if (GameSettings.Instance != null &&
            GameSettings.Instance.CurrentMode != GameMode.Desktop)
        {
            ClearHighlight();
            return;
        }

        // Camera might be spawned later by rig spawner → refresh every frame if null
        if (cam == null)
            cam = Camera.main;

        if (cam == null || desktopGrabberLongDistance == null)
        {
            ClearHighlight();
            return;
        }

        // Only highlight stands when we are actually holding a hat
        GameObject heldHat = desktopGrabberLongDistance.CurrentHeldHat;
        if (heldHat == null)
        {
            ClearHighlight();
            return;
        }

        // Raycast from camera forward
        Ray ray = new Ray(cam.transform.position, cam.transform.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, maxDistance, standLayerMask, QueryTriggerInteraction.Ignore))
        {
            HatStandTrigger stand = hit.collider.GetComponentInParent<HatStandTrigger>();
            SetHighlight(stand);
        }
        else
        {
            ClearHighlight();
        }
    }

    private void OnInteract(InputAction.CallbackContext ctx)
    {
        if (currentStand == null || desktopGrabberLongDistance == null)
            return;

        GameObject heldHat = desktopGrabberLongDistance.CurrentHeldHat;
        if (heldHat == null)
            return;

        // Use your existing stand logic to place + score
        currentStand.ForcePlaceHat(heldHat);
        ClearHighlight();
    }

    private void SetHighlight(HatStandTrigger stand)
    {
        // Same stand as last frame → nothing to change
        if (stand == currentStand)
            return;

        // Remove old highlight
        ClearHighlight();

        if (stand == null || highlightMaterial == null)
        {
            currentStand = null;
            return;
        }

        currentStand = stand;

        // Try to find a renderer on the stand or its children
        currentRenderer = stand.GetComponentInChildren<Renderer>();
        if (currentRenderer != null)
        {
            originalMaterial = currentRenderer.material;
            currentRenderer.material = highlightMaterial;
        }
        else
        {
            // No renderer found → nothing to visually highlight
            currentStand = null;
        }
    }

    private void ClearHighlight()
    {
        if (currentRenderer != null && originalMaterial != null)
        {
            currentRenderer.material = originalMaterial;
        }

        currentStand = null;
        currentRenderer = null;
        originalMaterial = null;
    }
}