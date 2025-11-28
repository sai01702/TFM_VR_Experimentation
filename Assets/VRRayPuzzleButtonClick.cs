using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

/// <summary>
/// VR Ray Interactor button click handler for long-distance mode.
/// Uses XRRayInteractor's built-in raycast for proper ray visualization.
/// </summary>
public class VRRayPuzzleButtonClick : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The XRRayInteractor on this hand (long-distance ray). Auto-finds if not assigned.")]
    public XRRayInteractor rayInteractor;

    [Tooltip("Input action for the trigger press (e.g. RightHand/Activate or Select).")]
    public InputActionReference triggerAction;

    [Header("Fallback Raycast Settings")]
    [Tooltip("Max distance for fallback button raycast.")]
    public float maxDistance = 15f;

    [Tooltip("LayerMask for the button/collider.")]
    public LayerMask buttonMask = ~0;

    [Tooltip("Optional: only accept colliders that have this tag. Leave empty to ignore tag filter.")]
    public string buttonTag = "";

    [Header("Debug")]
    [Tooltip("Enable debug logs to diagnose issues.")]
    public bool debugMode = true;

    private bool wasPressed = false;

    void Awake()
    {
        // Auto-find ray interactor if not assigned
        if (rayInteractor == null)
        {
            rayInteractor = GetComponent<XRRayInteractor>();
            if (rayInteractor == null)
                rayInteractor = GetComponentInChildren<XRRayInteractor>();
            if (rayInteractor == null)
                rayInteractor = GetComponentInParent<XRRayInteractor>();
        }

        if (rayInteractor == null && debugMode)
            Debug.LogError("[VRRayPuzzleButtonClick] No XRRayInteractor found! Please assign one.");
    }

    void OnEnable()
    {
        if (triggerAction != null && triggerAction.action != null)
            triggerAction.action.Enable();
    }

    void OnDisable()
    {
        if (triggerAction != null && triggerAction.action != null)
            triggerAction.action.Disable();
    }

    void Update()
    {
        if (rayInteractor == null)
            return;

        // Check for trigger press
        bool isPressed = false;
        
        if (triggerAction != null && triggerAction.action != null)
        {
            isPressed = triggerAction.action.IsPressed();
        }

        // Detect press edge (rising edge)
        bool justPressed = isPressed && !wasPressed;
        wasPressed = isPressed;

        if (!justPressed)
            return;

        if (debugMode)
            Debug.Log("[VRRayPuzzleButtonClick] Trigger pressed! Checking for button...");

        // METHOD 1: Use XRRayInteractor's built-in raycast (preferred - shows the ray properly)
        if (rayInteractor.TryGetCurrent3DRaycastHit(out RaycastHit xrHit))
        {
            if (debugMode)
                Debug.Log($"[VRRayPuzzleButtonClick] XR Ray hit: {xrHit.collider.name} at distance {xrHit.distance}");

            if (TryPressButton(xrHit.collider))
                return;
        }

        // METHOD 2: Fallback manual raycast (in case XRRayInteractor isn't hitting triggers)
        Transform t = rayInteractor.attachTransform != null ? rayInteractor.attachTransform : rayInteractor.transform;
        Ray ray = new Ray(t.position, t.forward);
        
        if (debugMode)
            Debug.DrawRay(ray.origin, ray.direction * maxDistance, Color.yellow, 1f);

        if (Physics.Raycast(ray, out RaycastHit hit, maxDistance, buttonMask, QueryTriggerInteraction.Collide))
        {
            if (debugMode)
                Debug.Log($"[VRRayPuzzleButtonClick] Fallback ray hit: {hit.collider.name} at distance {hit.distance}");

            TryPressButton(hit.collider);
        }
        else if (debugMode)
        {
            Debug.Log("[VRRayPuzzleButtonClick] No raycast hit detected.");
        }
    }

    bool TryPressButton(Collider hitCollider)
    {
        // Optional tag filter
        if (!string.IsNullOrEmpty(buttonTag) && !hitCollider.CompareTag(buttonTag))
        {
            if (debugMode)
                Debug.Log($"[VRRayPuzzleButtonClick] Hit {hitCollider.name} but tag doesn't match '{buttonTag}'");
            return false;
        }

        // Look for CheckHatsOnTouch on this object or its parents
        var button = hitCollider.GetComponent<CheckHatsOnTouch>();
        if (button == null)
            button = hitCollider.GetComponentInParent<CheckHatsOnTouch>();

        if (button != null)
        {
            button.ManualPress();
            Debug.Log($"[VRRayPuzzleButtonClick] ✓ Button pressed via ray on: {hitCollider.name}");
            return true;
        }

        if (debugMode)
            Debug.Log($"[VRRayPuzzleButtonClick] Hit {hitCollider.name} but no CheckHatsOnTouch component found.");

        return false;
    }
}