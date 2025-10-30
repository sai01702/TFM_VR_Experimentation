using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

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

    private XRGrabInteractable held;

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

        Ray ray = new Ray(cam.transform.position, cam.transform.forward);
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, pickupRange, pickupMask, QueryTriggerInteraction.Ignore))
        {
            if (!hit.collider.CompareTag("Hat"))
                return;

            var grab = hit.collider.GetComponentInParent<XRGrabInteractable>();
            if (grab == null)
                return;

            grab.enabled = false;

            Rigidbody rb = grab.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = true;
                rb.useGravity = false;
                rb.constraints = RigidbodyConstraints.None;
            }

            grab.transform.SetParent(holdPoint, worldPositionStays: false);
            grab.transform.localPosition = Vector3.zero;
            grab.transform.localRotation = Quaternion.identity;

            var clue = grab.GetComponent<ShowClueOnGrab>();
            if (clue != null) clue.HandleGrab();

            held = grab;
        }
    }

    void TryDropOrPlace()
    {
        if (held == null) return;

        GameObject hatGO = held.gameObject;
        Rigidbody rb = hatGO.GetComponent<Rigidbody>();

        HatStandTrigger stand = FindStandInFront();

        // Detach from hand
        held.transform.SetParent(null);

        if (rb != null)
        {
            rb.isKinematic = false;
            rb.useGravity = true;
            rb.constraints = RigidbodyConstraints.None;
        }

        if (stand != null)
        {
            // ask the stand to claim, snap, and evaluate correctness
            stand.ForcePlaceHat(hatGO);
        }
        else
        {
            // free drop
            if (rb != null && cam != null && dropForwardBoost > 0f)
            {
                rb.velocity = cam.transform.forward * dropForwardBoost;
            }
        }

        var clueDrop = held.GetComponent<ShowClueOnGrab>();
        if (clueDrop != null) clueDrop.HandleRelease();

        held.enabled = true;
        held = null;
    }

    HatStandTrigger FindStandInFront()
    {
        if (cam == null) cam = Camera.main;
        if (cam == null) return null;

        float maxDistance = 3f;
        Ray ray = new Ray(cam.transform.position, cam.transform.forward);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, maxDistance, ~0, QueryTriggerInteraction.Collide))
        {
            HatStandTrigger stand = hit.collider.GetComponent<HatStandTrigger>();
            if (stand != null) return stand;

            stand = hit.collider.GetComponentInParent<HatStandTrigger>();
            if (stand != null) return stand;
        }

        return null;
    }

    // Stand calls this to forcefully clear when hat was yanked at trigger time
    public void ForceFullReleaseIfHolding(GameObject hatGO)
    {
        if (held == null) return;
        if (held.gameObject != hatGO) return;

        // unparent
        held.transform.SetParent(null);

        // restore physics
        Rigidbody rb = held.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = false;
            rb.useGravity = true;
            rb.constraints = RigidbodyConstraints.None;
        }

        // allow VR grabbing again
        held.enabled = true;

        // hide clue UI
        var clueDrop = held.GetComponent<ShowClueOnGrab>();
        if (clueDrop != null) clueDrop.HandleRelease();

        // clear ref
        held = null;
    }
}
