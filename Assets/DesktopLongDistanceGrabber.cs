using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class DesktopLongDistanceGrabber : MonoBehaviour
{
    [Header("Input")]
    public InputActionReference interactAction;   // same Interact action as other rigs

    [Header("References")]
    public Camera cam;
    public Transform holdPoint;
    public float pickupRange = 8f;
    public LayerMask pickupMask = ~0;
    public float dropForwardBoost = 0f;

    [Header("Stand placing")]
    [Tooltip("LayerMask for HatStandTrigger objects (HatStand layer).")]
    public LayerMask standLayerMask;
    public float standMaxDistance = 50f;

    [Header("Crosshair")]
    [Tooltip("Renderer of your crosshair sphere/quad under the camera.")]
    public Renderer crosshairRenderer;
    public Color crosshairTargetColor = Color.yellow;

    private XRGrabInteractable held;
    private GameObject heldGO;
    private Color _crosshairOriginalColor;
    private bool _hasCrosshair;

    // cache clue component for current hat
    private ShowClueOnGrab heldClue;

    void Awake()
    {
        if (cam == null)
            cam = Camera.main;

        if (crosshairRenderer != null)
        {
            _crosshairOriginalColor = crosshairRenderer.material.color;
            _hasCrosshair = true;
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
    }

    void Update()
    {
        UpdateCrosshairHighlight();
    }

    void OnInteract(InputAction.CallbackContext ctx)
    {
        if (held == null)
            TryPickup();
        else
            TryDropOrPlace();
    }

    // ─────────────────────────────────────
    // PICKUP (long distance)
    // ─────────────────────────────────────
    void TryPickup()
    {
        if (cam == null) return;

        Ray ray = new Ray(cam.transform.position, cam.transform.forward);
        if (!Physics.Raycast(ray, out RaycastHit hit, pickupRange, pickupMask, QueryTriggerInteraction.Ignore))
            return;

        if (!hit.collider.CompareTag("Hat")) return;

        var grab = hit.collider.GetComponentInParent<XRGrabInteractable>();
        if (grab == null) return;

        // Take manual control
        var rb = grab.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
            rb.constraints = RigidbodyConstraints.None;
        }

        grab.enabled = false;

        grab.transform.SetParent(holdPoint, worldPositionStays: false);
        grab.transform.localPosition = Vector3.zero;
        grab.transform.localRotation = Quaternion.identity;

        held = grab;
        heldGO = grab.gameObject;

        // 🔸 mimic VR clue behaviour
        HandleClueOnGrab(heldGO);

        // 🔸 hide crosshair while holding
        SetCrosshairVisible(false);
    }

    // ─────────────────────────────────────
    // DROP or PLACE on stand
    // ─────────────────────────────────────
    void TryDropOrPlace()
    {
        if (held == null) return;

        // Try to find stand in front
        HatStandTrigger stand = FindStandInFront();

        // Always detach from hand first
        held.transform.SetParent(null, true);

        var rb = held.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = false;
            rb.useGravity = true;
            rb.constraints = RigidbodyConstraints.None;
        }

        if (stand != null)
        {
            // Let stand snap + handle scoring
            stand.ForcePlaceHat(held.gameObject);
        }
        else
        {
            // Normal drop in world with optional forward boost
            if (rb != null && cam != null && dropForwardBoost > 0f)
                rb.velocity = cam.transform.forward * dropForwardBoost;
        }

        // Re-enable XR grab so VR/desktop can pick up later
        held.enabled = true;

        // 🔸 hide clue like VR release
        HandleClueOnRelease(heldGO);

        held = null;
        heldGO = null;

        // 🔸 show crosshair again
        SetCrosshairVisible(true);
    }

    // Called by HatStandTrigger.ForceHandReleaseDesktop()
    public void ForceFullReleaseIfHolding(GameObject go)
    {
        if (held == null || heldGO == null) return;
        if (heldGO != go) return;

        held.transform.SetParent(null, true);

        var rb = held.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = false;
            rb.useGravity = true;
            rb.constraints = RigidbodyConstraints.None;
        }

        held.enabled = true;

        // 🔸 hide clue on forced release too
        HandleClueOnRelease(heldGO);

        held = null;
        heldGO = null;

        SetCrosshairVisible(true);
    }

    // ─────────────────────────────────────
    // Stand detection from camera forward
    // ─────────────────────────────────────
    HatStandTrigger FindStandInFront()
    {
        if (cam == null) return null;

        Ray ray = new Ray(cam.transform.position, cam.transform.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, standMaxDistance, standLayerMask))
        {
            return hit.collider.GetComponentInParent<HatStandTrigger>();
        }

        return null;
    }

    // ─────────────────────────────────────
    // Crosshair feedback (highlight stand)
    // ─────────────────────────────────────
    void UpdateCrosshairHighlight()
    {
        if (!_hasCrosshair) return;
        if (!crosshairRenderer.enabled) return;

        // Not holding a hat → always base color
        if (held == null)
        {
            crosshairRenderer.material.color = _crosshairOriginalColor;
            return;
        }

        // Holding a hat → yellow if a stand is in front
        HatStandTrigger stand = FindStandInFront();
        crosshairRenderer.material.color =
            (stand != null) ? crosshairTargetColor : _crosshairOriginalColor;
    }

    void SetCrosshairVisible(bool visible)
    {
        if (!_hasCrosshair) return;
        crosshairRenderer.enabled = visible;
    }

    // ─────────────────────────────────────
    // CLUES (simulate ShowClueOnGrab behaviour)
    // ─────────────────────────────────────
    void HandleClueOnGrab(GameObject hat)
    {
        heldClue = hat.GetComponent<ShowClueOnGrab>();
        if (heldClue == null) return;

        // Same idea as HideAllClues()
        GameObject cluesParent = GameObject.Find("CluesPanels");
        if (cluesParent != null)
        {
            foreach (Transform child in cluesParent.transform)
                child.gameObject.SetActive(false);
        }

        if (heldClue.clueToShow != null)
            heldClue.clueToShow.SetActive(true);

        if (PuzzleLogsManager.Instance != null)
        {
            PuzzleLogsManager.Instance.RegistrarAgarreSombrero(hat.name);
            if (heldClue.clueToShow != null)
                PuzzleLogsManager.Instance.RegistrarMostrarPista(hat.name);
        }
    }

    void HandleClueOnRelease(GameObject hat)
    {
        var clueComp = heldClue != null ? heldClue : hat.GetComponent<ShowClueOnGrab>();
        if (clueComp != null && clueComp.clueToShow != null)
            clueComp.clueToShow.SetActive(false);

        if (PuzzleLogsManager.Instance != null)
            PuzzleLogsManager.Instance.RegistrarSueltaSombrero(hat.name);

        heldClue = null;
    }
}