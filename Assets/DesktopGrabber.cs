using UnityEngine;
using UnityEngine.InputSystem;

public class DesktopGrabber : MonoBehaviour
{
    [Header("Input")]
    public InputActionReference interactAction; // Player/Interact

    [Header("References")]
    public Camera cam;
    public Transform holdPoint;

    [Header("Pickup")]
    public float pickupRange = 3f;
    public LayerMask pickupMask = ~0;

    [Header("Drop")]
    public float dropForwardBoost = 0f;

    // XR
    private UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable held;
    public DesktopAimHighlighter aimHighlighter; // optional, for extra feedback

    void Awake()
    {
        if (cam == null) cam = Camera.main;
    }

    void OnEnable()
    {
        if (interactAction != null)
            interactAction.action.performed += OnInteract;
    }

    void OnDisable()
    {
        if (interactAction != null)
            interactAction.action.performed -= OnInteract;
    }

    void OnInteract(InputAction.CallbackContext ctx)
    {
        if (held == null) TryPickup();
        else TryDropOrPlace();
    }

    void TryPickup()
    {
        if (cam == null) return;

        var ray = new Ray(cam.transform.position, cam.transform.forward);
        if (Physics.Raycast(ray, out var hit, pickupRange, pickupMask, QueryTriggerInteraction.Ignore))
        {
            if (!hit.collider.CompareTag("Hat")) return;

            var grab = hit.collider.GetComponentInParent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
            if (grab == null) return;

            // Disable XR grabbing to take manual control in desktop
            grab.enabled = false;

            var rb = grab.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = true;
                rb.useGravity = false;
            }

            grab.transform.SetParent(holdPoint, worldPositionStays: false);
            grab.transform.localPosition = Vector3.zero;
            grab.transform.localRotation = Quaternion.identity;

            // 🔔 Tell the clue script we grabbed this hat (desktop path)
            var clue = grab.GetComponent<ShowClueOnGrab>();
            if (clue != null) clue.HandleGrab();

            var hi = grab.GetComponent<HatHighlighter>();
            if (hi != null)
            {
                hi.DesktopGrabStart();
            }

            held = grab;
        }
        
    }

    void TryDropOrPlace()
    {
        if (held == null) return;

        // Detach from hand
        held.transform.SetParent(null);

        var rb = held.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = false;
            rb.useGravity = true;
            if (cam != null && dropForwardBoost > 0f)
                rb.velocity = cam.transform.forward * dropForwardBoost;
        }

        // 🔔 Tell the clue script we released it (desktop path)
        var clue = held.GetComponent<ShowClueOnGrab>();
        if (clue != null) clue.HandleRelease();

        // Re-enable XR grabbing so VR path works later
        held.enabled = true;
        held = null;

        var hi = held.GetComponent<HatHighlighter>();
        if (hi != null)
        {
            hi.DesktopGrabEnd();
        }
    }
}