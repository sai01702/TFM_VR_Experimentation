using UnityEngine;
using UnityEngine.InputSystem;

public interface IInteractable
{
    void Interact();
}

public class DesktopInteract : MonoBehaviour
{
    [SerializeField] private Camera cam;
    [SerializeField] private float range = 3f;
    [SerializeField] private LayerMask interactLayers = ~0;
    [SerializeField] private PlayerInput playerInput;

    private InputAction interactAction;

    void Awake()
    {
        if (!cam) cam = Camera.main;
        if (!playerInput) playerInput = GetComponent<PlayerInput>();
        interactAction = playerInput.actions["Interact"];
    }

    void OnEnable() => interactAction.Enable();
    void OnDisable() => interactAction.Disable();

    void Update()
    {
        if (!interactAction.WasPressedThisFrame()) return;

        if (Physics.Raycast(cam.transform.position, cam.transform.forward, out var hit, range, interactLayers))
        {
            hit.collider.GetComponentInParent<IInteractable>()?.Interact();
        }
    }
}