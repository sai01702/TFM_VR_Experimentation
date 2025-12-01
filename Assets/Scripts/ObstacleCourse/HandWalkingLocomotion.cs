using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

/// <summary>
/// Walking-in-place locomotion using hand controllers.
/// Detects up/down arm swinging motion and converts it to forward movement.
/// Works with Quest, Vive, or any VR controller.
/// Disables joystick movement when active (but allows rotation).
/// </summary>
public class HandWalkingLocomotion : MonoBehaviour
{
    [Header("Walk Mode Activation")]
    [Tooltip("Leave empty to use this GameObject.")]
    public GameObject walkModeObject;

    [Header("Controller References")]
    [Tooltip("Left hand controller transform. Will auto-find if empty.")]
    public Transform leftController;
    
    [Tooltip("Right hand controller transform. Will auto-find if empty.")]
    public Transform rightController;

    [Header("XR Rig Reference")]
    [Tooltip("The XR Origin/Rig to move. Will auto-find if empty.")]
    public Transform xrRig;
    
    [Tooltip("The camera/head transform for forward direction. Will auto-find if empty.")]
    public Transform headTransform;

    [Header("Movement Settings")]
    [Tooltip("Movement speed multiplier.")]
    public float moveSpeed = 3f;
    
    [Tooltip("Minimum vertical hand movement per frame to count as swinging (meters).")]
    public float minSwingSpeed = 0.3f;
    
    [Tooltip("How quickly movement stops when you stop swinging.")]
    [Range(0.1f, 5f)]
    public float movementDecay = 2f;

    [Header("Joystick Control")]
    [Tooltip("Disable joystick movement when swing walk is active. Rotation still works.")]
    public bool disableJoystickMove = true;

    [Header("Debug")]
    public bool showDebugInfo = false;
    [Tooltip("Show swing values every frame (very spammy).")]
    public bool showFrameDebug = false;

    // Internal state
    private Vector3 leftLastPos;
    private Vector3 rightLastPos;
    private float currentSpeed;
    
    private CharacterController characterController;
    private Behaviour[] moveProviders; // Using Behaviour to support different provider types
    private bool isInitialized;
    private float initRetryTime;
    private bool joystickWasDisabled;

    void Start()
    {
        if (walkModeObject == null)
            walkModeObject = this.gameObject;
            
        TryInitialize();
    }

    void Update()
    {
        if (!isInitialized)
        {
            if (Time.time - initRetryTime > 0.5f)
            {
                initRetryTime = Time.time;
                TryInitialize();
            }
            if (!isInitialized) return;
        }

        // Check if walk mode is active
        bool walkModeActive = (walkModeObject == null || walkModeObject.activeInHierarchy);
        bool isVR = GameSettings.Instance == null || GameSettings.Instance.CurrentMode == GameMode.VR;
        bool shouldBeActive = walkModeActive && isVR;

        // Handle joystick enable/disable
        if (disableJoystickMove)
        {
            UpdateJoystickState(shouldBeActive);
        }

        // Only process swing if active
        if (!shouldBeActive)
        {
            currentSpeed = 0f;
            return;
        }

        DetectSwingAndMove();
    }

    void UpdateJoystickState(bool swingWalkActive)
    {
        if (moveProviders == null || moveProviders.Length == 0) return;

        // Always enforce the state every frame to ensure it stays disabled
        foreach (var mp in moveProviders)
        {
            if (mp == null) continue;
            
            bool shouldBeEnabled = !swingWalkActive;
            
            // Force set enabled state
            mp.enabled = shouldBeEnabled;
            
            if (showDebugInfo && joystickWasDisabled != swingWalkActive)
            {
                Debug.Log($"[HandWalkingLocomotion] {(shouldBeEnabled ? "ENABLED" : "DISABLED")} {mp.GetType().Name} on '{mp.gameObject.name}'");
            }
        }

        joystickWasDisabled = swingWalkActive;
    }

    void OnDisable()
    {
        // Re-enable joystick when this component is disabled
        if (moveProviders != null)
        {
            foreach (var mp in moveProviders)
            {
                if (mp != null)
                    mp.enabled = true;
            }
            joystickWasDisabled = false;
            Debug.Log("[HandWalkingLocomotion] OnDisable - Re-enabled all move providers");
        }
    }

    void TryInitialize()
    {
        // Find XR Rig
        if (xrRig == null)
        {
            GameObject[] allObjects = FindObjectsOfType<GameObject>();
            foreach (var obj in allObjects)
            {
                if (obj.name.Contains("XR Origin") || obj.name.Contains("XR Rig"))
                {
                    xrRig = obj.transform;
                    if (showDebugInfo)
                        Debug.Log($"[HandWalkingLocomotion] Found XR Origin: {xrRig.name}");
                    break;
                }
            }
        }

        if (xrRig == null) return;

        // Find head
        if (headTransform == null)
        {
            var cameras = xrRig.GetComponentsInChildren<Camera>(true);
            foreach (var cam in cameras)
            {
                if (cam.CompareTag("MainCamera") || cam.name.Contains("Main"))
                {
                    headTransform = cam.transform;
                    break;
                }
            }
            if (headTransform == null && Camera.main != null)
                headTransform = Camera.main.transform;
                
            if (headTransform != null && showDebugInfo)
                Debug.Log($"[HandWalkingLocomotion] Found head: {headTransform.name}");
        }

        // Find controllers
        if (leftController == null || rightController == null)
        {
            var allTransforms = xrRig.GetComponentsInChildren<Transform>(true);
            foreach (var t in allTransforms)
            {
                if (leftController == null && t.name == "Left Controller")
                    leftController = t;
                if (rightController == null && t.name == "Right Controller")
                    rightController = t;
            }
            
            if (leftController != null && showDebugInfo)
                Debug.Log($"[HandWalkingLocomotion] Found left: {leftController.name}");
            if (rightController != null && showDebugInfo)
                Debug.Log($"[HandWalkingLocomotion] Found right: {rightController.name}");
        }

        // Get CharacterController - MUST find the same one used by XR locomotion
        characterController = xrRig.GetComponent<CharacterController>();
        
        if (characterController == null)
        {
            // Search in children
            characterController = xrRig.GetComponentInChildren<CharacterController>();
        }
        
        if (characterController == null)
        {
            // Last resort: find any CharacterController in scene
            characterController = FindObjectOfType<CharacterController>();
        }
            
        if (characterController != null)
        {
            if (showDebugInfo)
            {
                Debug.Log($"[HandWalkingLocomotion] Found CharacterController on: {characterController.gameObject.name}");
                Debug.Log($"[HandWalkingLocomotion] CC enabled: {characterController.enabled}, height: {characterController.height}");
            }
        }
        else
        {
            Debug.LogWarning("[HandWalkingLocomotion] NO CharacterController found! Movement will have no collision!");
        }

        // Find ALL move-related locomotion providers to disable joystick movement
        if (disableJoystickMove)
        {
            var providerList = new System.Collections.Generic.List<Behaviour>();
            
            // Method 1: Find ContinuousMoveProviderBase
            var continuousProviders = FindObjectsOfType<ContinuousMoveProviderBase>(true);
            if (continuousProviders != null)
            {
                foreach (var p in continuousProviders)
                    providerList.Add(p);
            }
            
            // Method 2: Find any LocomotionProvider with "Move" in name (but not "Grab Move")
            var allProviders = FindObjectsOfType<UnityEngine.XR.Interaction.Toolkit.Locomotion.LocomotionProvider>(true);
            if (allProviders != null)
            {
                foreach (var p in allProviders)
                {
                    string name = p.gameObject.name.ToLower();
                    // Include "Move" but exclude "Grab Move" and "Turn"
                    if (name.Contains("move") && !name.Contains("grab") && !providerList.Contains(p))
                    {
                        providerList.Add(p);
                    }
                }
            }
            
            // Method 3: Find by exact GameObject name "Move"
            if (xrRig != null)
            {
                var allTransforms = xrRig.GetComponentsInChildren<Transform>(true);
                foreach (var t in allTransforms)
                {
                    if (t.name == "Move")
                    {
                        var providers = t.GetComponents<Behaviour>();
                        foreach (var p in providers)
                        {
                            if (p is UnityEngine.XR.Interaction.Toolkit.Locomotion.LocomotionProvider && !providerList.Contains(p))
                                providerList.Add(p);
                        }
                    }
                }
            }
            
            moveProviders = providerList.ToArray();
            
            if (moveProviders.Length > 0)
            {
                Debug.Log($"[HandWalkingLocomotion] Found {moveProviders.Length} MoveProvider(s) to disable:");
                foreach (var mp in moveProviders)
                {
                    Debug.Log($"  - {mp.GetType().Name} on '{mp.gameObject.name}' (enabled: {mp.enabled})");
                }
            }
            else
            {
                Debug.LogWarning("[HandWalkingLocomotion] No MoveProviders found! Joystick will NOT be disabled.");
            }
        }

        // Initialize positions
        if (leftController != null)
            leftLastPos = leftController.position;
        if (rightController != null)
            rightLastPos = rightController.position;

        isInitialized = (leftController != null || rightController != null) && xrRig != null && headTransform != null;

        if (isInitialized && showDebugInfo)
            Debug.Log("[HandWalkingLocomotion] ✓ Initialized!");
    }

    void DetectSwingAndMove()
    {
        float totalSwingSpeed = 0f;

        // Measure left hand vertical speed
        if (leftController != null)
        {
            Vector3 currentPos = leftController.position;
            float deltaY = Mathf.Abs(currentPos.y - leftLastPos.y);
            float speed = deltaY / Time.deltaTime;
            totalSwingSpeed += speed;
            leftLastPos = currentPos;

            if (showFrameDebug && speed > 0.1f)
                Debug.Log($"[Left] Speed: {speed:F2} m/s");
        }

        // Measure right hand vertical speed
        if (rightController != null)
        {
            Vector3 currentPos = rightController.position;
            float deltaY = Mathf.Abs(currentPos.y - rightLastPos.y);
            float speed = deltaY / Time.deltaTime;
            totalSwingSpeed += speed;
            rightLastPos = currentPos;

            if (showFrameDebug && speed > 0.1f)
                Debug.Log($"[Right] Speed: {speed:F2} m/s");
        }

        // Average if both hands
        if (leftController != null && rightController != null)
            totalSwingSpeed *= 0.5f;

        // Check if swinging fast enough
        if (totalSwingSpeed > minSwingSpeed)
        {
            // Map swing speed to movement speed
            float targetSpeed = Mathf.Clamp(totalSwingSpeed / 2f, 0f, 1f) * moveSpeed;
            currentSpeed = Mathf.Lerp(currentSpeed, targetSpeed, Time.deltaTime * 5f);

            if (showDebugInfo)
                Debug.Log($"[HandWalking] Swing: {totalSwingSpeed:F2} m/s → Move: {currentSpeed:F2} m/s");
        }
        else
        {
            // Decay speed when not swinging
            currentSpeed = Mathf.Lerp(currentSpeed, 0f, Time.deltaTime * movementDecay);
        }

        // Apply movement
        if (currentSpeed > 0.01f)
        {
            ApplyMovement();
        }
    }

    void ApplyMovement()
    {
        if (headTransform == null) return;

        // Forward direction from head (horizontal only)
        Vector3 forward = headTransform.forward;
        forward.y = 0f;
        forward.Normalize();

        // Only horizontal movement - let XR system handle gravity
        Vector3 movement = forward * currentSpeed * Time.deltaTime;

        // ALWAYS try to use CharacterController for collision
        if (characterController != null && characterController.enabled)
        {
            // Don't add our own gravity - the XR locomotion system handles it
            // Just do horizontal movement
            CollisionFlags flags = characterController.Move(movement);

            if (showDebugInfo)
                Debug.Log($"[HandWalking] CC Move: {movement.x:F3}, {movement.z:F3} | Flags: {flags}");
        }
        else if (xrRig != null)
        {
            // Fallback: direct movement (no collision) - only if no CharacterController
            xrRig.position += movement;

            if (showDebugInfo)
                Debug.Log($"[HandWalking] WARNING: No CharacterController! Direct move: {movement.magnitude:F3}m");
        }
    }

    public void Reinitialize()
    {
        isInitialized = false;
        leftController = null;
        rightController = null;
        xrRig = null;
        headTransform = null;
        TryInitialize();
    }
}
