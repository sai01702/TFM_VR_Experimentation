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

    [Tooltip("Assign your crosshair/cross-aim object (e.g., the sphere under the camera).")]
    public GameObject crossAim;

    [Header("Pickup")]
    public float pickupRange = 3f;
    public LayerMask pickupMask = ~0;

    [Header("Drop")]
    public float dropForwardBoost = 0f;

    // the hat we're currently holding
    private XRGrabInteractable held;

    void Awake()
    {
        if (cam == null) cam = Camera.main;

        // Optional auto-find if you didn't drag it in:
        if (crossAim == null && cam != null)
        {
            var t = cam.transform.Find("CrossAim");
            if (t != null) crossAim = t.gameObject;
        }
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

        // Safety: if we disable mid-hold, re-show crosshair
        SetCrossAimVisible(true);
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
        if (Physics.Raycast(ray, out RaycastHit hit, pickupRange, pickupMask, QueryTriggerInteraction.Ignore))
        {
            if (!hit.collider.CompareTag("Hat"))
                return;

            var grab = hit.collider.GetComponentInParent<XRGrabInteractable>();
            if (grab == null)
                return;

            // disable XR grabbing so desktop owns it
            grab.enabled = false;

            Rigidbody rb = grab.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = true;
                rb.useGravity = false;
                rb.constraints = RigidbodyConstraints.None; // free while held
            }

            // parent to hold point
            grab.transform.SetParent(holdPoint, worldPositionStays: false);
            grab.transform.localPosition = Vector3.zero;
            grab.transform.localRotation = Quaternion.identity;

            // show clue (if you have it)
            var clue = grab.GetComponent<ShowClueOnGrab>();
            if (clue != null) clue.HandleGrab();

            held = grab;

            // 🔻 Hide cross-aim while holding
            SetCrossAimVisible(false);
        }
    }

    void TryDropOrPlace()
    {
        if (held == null) return;

        GameObject hatGO = held.gameObject;
        Rigidbody rb = hatGO.GetComponent<Rigidbody>();

        // check if we're aiming at a stand
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
            // normal free drop
            if (rb != null && cam != null && dropForwardBoost > 0f)
            {
                rb.velocity = cam.transform.forward * dropForwardBoost;
            }
        }

        // hide clue
        var clueDrop = held.GetComponent<ShowClueOnGrab>();
        if (clueDrop != null) clueDrop.HandleRelease();

        // re-enable XR grabbing so VR can grab later
        held.enabled = true;
        held = null;

        // 🔺 Show cross-aim again after release
        SetCrossAimVisible(true);
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

    // Called by HatStandTrigger when it auto-steals the hat on trigger
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

        // 🔺 Show cross-aim again because we're no longer holding
        SetCrossAimVisible(true);
    }

    void SetCrossAimVisible(bool visible)
    {
        if (crossAim != null && crossAim.activeSelf != visible)
            crossAim.SetActive(visible);
    }
}
