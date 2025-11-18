using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class DesktopLongDistancePlacer : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Desktop camera (usually the same camera your crosshair is attached to).")]
    public Camera cam;

    [Tooltip("The same HoldPoint used by DesktopGrabber (where hats sit when grabbed).")]
    public Transform holdPoint;

    [Header("Stand detection")]
    [Tooltip("LayerMask with HatStand objects. If left empty, raycast will hit everything and filter by HatStandTrigger.")]
    public LayerMask standLayerMask;

    [Tooltip("Max distance to detect a HatStand in front of the camera.")]
    public float standMaxDistance = 40f;

    [Header("Crosshair feedback")]
    [Tooltip("Renderer of your crosshair sphere / quad.")]
    public Renderer crosshairRenderer;
    public Color crosshairNormalColor = Color.white;
    public Color crosshairTargetColor = Color.yellow;

    [Header("Input")]
    [Tooltip("Keyboard key to confirm placing to the stand.")]
    public Key placeKey = Key.E;   // <- E by default

    [Tooltip("Also allow left mouse click to place?")]
    public bool allowMouseLeft = true;

    XRGrabInteractable _heldHat;
    Color _cachedOriginalColor;
    bool _hasCrosshair;

    void Awake()
    {
        if (cam == null)
            cam = Camera.main;

        if (crosshairRenderer != null)
        {
            _cachedOriginalColor = crosshairRenderer.material.color;
            if (crosshairNormalColor == default)
                crosshairNormalColor = _cachedOriginalColor;

            _hasCrosshair = true;
        }
    }

    void Update()
    {
        // Only active in Desktop mode
        if (GameSettings.Instance == null || GameSettings.Instance.CurrentMode != GameMode.Desktop)
        {
            SetCrosshair(crosshairNormalColor);
            return;
        }

        if (cam == null || holdPoint == null)
        {
            SetCrosshair(crosshairNormalColor);
            return;
        }

        // 1) Check if we're holding a hat (DesktopGrabber parents hat under holdPoint)
        _heldHat = holdPoint.GetComponentInChildren<XRGrabInteractable>();

        if (_heldHat == null)
        {
            SetCrosshair(crosshairNormalColor);
            return;
        }

        // 2) Raycast to find a stand in front
        HatStandTrigger targetStand = FindStandInFront();
        bool hasStand = targetStand != null;

        SetCrosshair(hasStand ? crosshairTargetColor : crosshairNormalColor);

        if (!hasStand)
            return;

        // 3) Input: E (default) or optional mouse left
        var kb = Keyboard.current;
        var mouse = Mouse.current;
        if (kb == null) return;

        bool pressed = kb[placeKey].wasPressedThisFrame;

        if (!pressed && allowMouseLeft && mouse != null && mouse.leftButton.wasPressedThisFrame)
            pressed = true;

        if (!pressed)
            return;

        Debug.Log($"[DesktopLongDistancePlacer] Placing hat '{_heldHat.name}' on stand '{targetStand.name}'");

        // 4) Ask stand to place the hat (it will also force Desktop release)
        targetStand.ForcePlaceHat(_heldHat.gameObject);

        _heldHat = null;
    }

    HatStandTrigger FindStandInFront()
    {
        Ray ray = new Ray(cam.transform.position, cam.transform.forward);

        // If no layerMask set in Inspector, use everything (~0)
        int mask = (standLayerMask.value == 0) ? ~0 : standLayerMask.value;

        if (Physics.Raycast(ray, out RaycastHit hit, standMaxDistance, mask, QueryTriggerInteraction.Ignore))
        {
            var stand = hit.collider.GetComponentInParent<HatStandTrigger>();
            if (stand != null)
            {
                // Debug to verify detection
                Debug.Log($"[DesktopLongDistancePlacer] Stand hit: {stand.name} via {hit.collider.name}");
                return stand;
            }
        }

        return null;
    }

    void SetCrosshair(Color c)
    {
        if (_hasCrosshair)
            crosshairRenderer.material.color = c;
    }
}
