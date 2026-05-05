using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit.Samples.StarterAssets;

/// <summary>
/// Analog sprint for Navigation VR: squeezing the left index trigger ramps move speed from the
/// configured DynamicMoveProvider base speed up to <see cref="sprintMaxSpeed"/>.
/// </summary>
[RequireComponent(typeof(DynamicMoveProvider))]
public class NavigationVRSprint : MonoBehaviour
{
    [SerializeField]
    DynamicMoveProvider moveProvider;

    [SerializeField]
    [Tooltip("Move speed at full left trigger squeeze.")]
    float sprintMaxSpeed = 6f;

    [SerializeField]
    [Tooltip("Trigger values below this map to walk speed (no sprint).")]
    float triggerDeadzone = 0.1f;

    InputAction _leftTriggerAction;
    float _baseMoveSpeed;

    void Awake()
    {
        if (moveProvider == null)
            moveProvider = GetComponent<DynamicMoveProvider>();

        _leftTriggerAction = new InputAction(
            name: "NavigationLeftTrigger",
            type: InputActionType.Value,
            binding: "<XRController>{LeftHand}/trigger");
    }

    void OnEnable()
    {
        if (moveProvider != null)
            _baseMoveSpeed = moveProvider.moveSpeed;

        _leftTriggerAction?.Enable();
    }

    void OnDisable()
    {
        _leftTriggerAction?.Disable();

        if (moveProvider != null)
            moveProvider.moveSpeed = _baseMoveSpeed;
    }

    void OnDestroy()
    {
        _leftTriggerAction?.Dispose();
        _leftTriggerAction = null;
    }

    void Update()
    {
        if (moveProvider == null)
            return;

        float raw = _leftTriggerAction != null ? _leftTriggerAction.ReadValue<float>() : 0f;
        float t = Mathf.Clamp01(Mathf.InverseLerp(triggerDeadzone, 1f, raw));
        moveProvider.moveSpeed = Mathf.Lerp(_baseMoveSpeed, sprintMaxSpeed, t);
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        if (moveProvider == null)
            moveProvider = GetComponent<DynamicMoveProvider>();
        sprintMaxSpeed = Mathf.Max(0f, sprintMaxSpeed);
        triggerDeadzone = Mathf.Clamp01(triggerDeadzone);
    }
#endif
}
