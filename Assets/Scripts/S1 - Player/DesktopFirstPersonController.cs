using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class DesktopFirstPersonController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform cameraPivot; // drag your PlayerCamera here
    [SerializeField] private PlayerInput playerInput; // drag the PlayerInput from the same GO

    [Header("Movement")]
    [SerializeField] private float walkSpeed = 3.5f;
    [SerializeField] private float sprintSpeed = 6.0f;
    [SerializeField] private float jumpHeight = 1.1f;
    [SerializeField] private float gravity = -9.81f;
    [SerializeField] private float groundedStick = -2f; // small downward force to keep grounded

    [Header("Mouse Look")]
    [SerializeField] private float mouseSensitivity = 0.12f; // multiply mouse delta
    [SerializeField] private float minPitch = -80f;
    [SerializeField] private float maxPitch = 80f;

    private CharacterController cc;
    private InputAction moveAction;
    private InputAction lookAction;
    private InputAction jumpAction;
    private InputAction sprintAction;
    private float pitch;
    private Vector3 velocity;

    void Awake()
    {
        cc = GetComponent<CharacterController>();

        if (playerInput == null) playerInput = GetComponent<PlayerInput>();
        if (playerInput != null)
        {
            // Make sure we're in Desktop scheme
            if (playerInput.currentControlScheme != "Desktop")
            {
                playerInput.defaultControlScheme = "Desktop";
                playerInput.SwitchCurrentControlScheme("Desktop");
            }
        }

        var actions = playerInput != null ? playerInput.actions : null;
        if (actions == null)
        {
            Debug.LogError("PlayerInput or Actions not assigned on DesktopFirstPersonController.");
            enabled = false;
            return;
        }

        moveAction = actions["Move"];
        lookAction = actions["Look"];
        jumpAction = actions["Jump"];
        sprintAction = actions["Sprint"];

        if (cameraPivot == null)
            Debug.LogWarning("Camera Pivot not set. Drag your PlayerCamera into the cameraPivot field.");
    }

    void OnEnable()
    {
        moveAction?.Enable();
        lookAction?.Enable();
        jumpAction?.Enable();
        sprintAction?.Enable();
    }

    void OnDisable()
    {
        moveAction?.Disable();
        lookAction?.Disable();
        jumpAction?.Disable();
        sprintAction?.Disable();
    }

    void Update()
    {
        if (cameraPivot == null) return;

        // --- Look ---
        Vector2 look = lookAction.ReadValue<Vector2>() * mouseSensitivity;
        // yaw: rotate body
        transform.Rotate(0f, look.x, 0f);
        // pitch: rotate camera
        pitch = Mathf.Clamp(pitch - look.y, minPitch, maxPitch);
        cameraPivot.localEulerAngles = new Vector3(pitch, 0f, 0f);

        // --- Move ---
        Vector2 move = moveAction.ReadValue<Vector2>();
        Vector3 input = (transform.right * move.x) + (transform.forward * move.y);
        float speed = sprintAction.IsPressed() ? sprintSpeed : walkSpeed;

        Vector3 horizontal = input.normalized * speed;

        // --- Gravity & Jump ---
        if (cc.isGrounded)
        {
            if (velocity.y < 0f) velocity.y = groundedStick;
            if (jumpAction.WasPressedThisFrame())
            {
                // v = sqrt(2gh)
                velocity.y = Mathf.Sqrt(-2f * gravity * jumpHeight);
            }
        }
        velocity.y += gravity * Time.deltaTime;

        // --- Apply movement ---
        Vector3 motion = (horizontal + new Vector3(0, velocity.y, 0)) * Time.deltaTime;
        cc.Move(motion);
    }
}