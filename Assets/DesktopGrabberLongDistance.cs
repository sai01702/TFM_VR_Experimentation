using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class DesktopGrabberLongDistance : MonoBehaviour
{
    [Header("Input")]
    public InputActionReference interactAction;   // E key / interact

    [Header("Camera & Hold Point")]
    public Camera cam;
    public Transform holdPoint;

    [Header("Pickup Settings")]
    public float pickupRange = 8f;
    public LayerMask pickupMask = ~0;            // layers that contain hats
    public float dropForwardBoost = 4f;

    [Header("UI")]
    [Tooltip("Crosshair / aim reticle object to hide while holding a hat.")]
    public GameObject crosshair;

    private XRGrabInteractable held;

    /// <summary>
    /// Public read-only access so DesktopLongDistanceHatPlacer can know if
    /// we are holding a hat and which one it is.
    /// </summary>
    public GameObject CurrentHeldHat => held != null ? held.gameObject : null;

    void Awake()
    {
        if (cam == null)
            cam = Camera.main;
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
        if (held == null)
            TryPickup();
        else
            TryDrop();
    }

    // -------------------------------------------------------
    // PICKUP
    // -------------------------------------------------------
    void TryPickup()
    {
        if (cam == null) return;

        Ray ray = new Ray(cam.transform.position, cam.transform.forward);

        if (Physics.Raycast(ray, out RaycastHit hit, pickupRange, pickupMask,
                            QueryTriggerInteraction.Ignore))
        {
            if (!hit.collider.CompareTag("Hat")) return;

            var grab = hit.collider.GetComponentInParent<XRGrabInteractable>();
            if (grab == null) return;

            // Take control of the hat
            grab.enabled = false;

            Rigidbody rb = grab.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = true;
                rb.useGravity = false;
                rb.velocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }

            grab.transform.SetParent(holdPoint, worldPositionStays: false);
            grab.transform.localPosition = Vector3.zero;
            grab.transform.localRotation = Quaternion.identity;

            held = grab;

            // 🔹 Hide crosshair while holding
            if (crosshair != null)
                crosshair.SetActive(false);
        }
    }

    // -------------------------------------------------------
    // DROP (simple drop – your HatPlacer script will do snapping)
    // -------------------------------------------------------
    void TryDrop()
    {
        if (held == null) return;

        held.transform.SetParent(null);

        Rigidbody rb = held.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = false;
            rb.useGravity = true;
            rb.velocity = cam != null ? cam.transform.forward * dropForwardBoost : Vector3.zero;
        }

        held.enabled = true;
        held = null;

        // 🔹 Show crosshair again
        if (crosshair != null)
            crosshair.SetActive(true);
    }

    /// <summary>
    /// Called by HatStandTrigger when it wants to forcibly clear the hand
    /// (for example after snapping the hat to a stand).
    /// </summary>
    public void ForceFullReleaseIfHolding(GameObject hatGO)
    {
        if (held == null) return;
        if (held.gameObject != hatGO) return;

        held.transform.SetParent(null);

        Rigidbody rb = held.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = false;
            rb.useGravity = true;
        }

        held.enabled = true;
        held = null;

        // 🔹 Also show crosshair when stand force-releases the hat
        if (crosshair != null)
            crosshair.SetActive(true);
    }
}