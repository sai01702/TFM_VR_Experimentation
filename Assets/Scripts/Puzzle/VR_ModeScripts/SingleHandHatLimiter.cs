using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

/// <summary>
/// Very small helper: on one controller that has both a direct interactor and a
/// ray interactor, only one of them is allowed to GRAB a hat at a time.
/// It only toggles allowSelect (never touches visuals or other settings).
/// </summary>
public class SingleHandHatLimiter : MonoBehaviour
{
    [Header("Interactor on the same hand")]
    [SerializeField] UnityEngine.XR.Interaction.Toolkit.Interactors.XRBaseInteractor directInteractor;
    [SerializeField] UnityEngine.XR.Interaction.Toolkit.Interactors.XRBaseInteractor rayInteractor;

    void OnEnable()
    {
        if (directInteractor != null)
        {
            directInteractor.selectEntered.AddListener(OnDirectSelect);
            directInteractor.selectExited.AddListener(OnDirectRelease);
        }

        if (rayInteractor != null)
        {
            rayInteractor.selectEntered.AddListener(OnRaySelect);
            rayInteractor.selectExited.AddListener(OnRayRelease);
        }

        UpdateAllowStates();
    }

    void OnDisable()
    {
        if (directInteractor != null)
        {
            directInteractor.selectEntered.RemoveListener(OnDirectSelect);
            directInteractor.selectExited.RemoveListener(OnDirectRelease);
        }

        if (rayInteractor != null)
        {
            rayInteractor.selectEntered.RemoveListener(OnRaySelect);
            rayInteractor.selectExited.RemoveListener(OnRayRelease);
        }

        // When this script is disabled, restore normal behaviour.
        if (directInteractor != null)
            directInteractor.allowSelect = true;
        if (rayInteractor != null)
            rayInteractor.allowSelect = true;
    }

    void OnDirectSelect(SelectEnterEventArgs _)
    {
        // If direct has something, ray cannot grab a second hat.
        if (rayInteractor != null)
            rayInteractor.allowSelect = false;
    }

    void OnDirectRelease(SelectExitEventArgs _)
    {
        UpdateAllowStates();
    }

    void OnRaySelect(SelectEnterEventArgs _)
    {
        // If ray has something, direct cannot grab another at the same time.
        if (directInteractor != null)
            directInteractor.allowSelect = false;
    }

    void OnRayRelease(SelectExitEventArgs _)
    {
        UpdateAllowStates();
    }

    void UpdateAllowStates()
    {
        bool directHolding = directInteractor != null && directInteractor.hasSelection;
        bool rayHolding = rayInteractor != null && rayInteractor.hasSelection;

        // If one is holding, the other cannot select; if none are holding, both can.
        if (directInteractor != null)
            directInteractor.allowSelect = !rayHolding;

        if (rayInteractor != null)
            rayInteractor.allowSelect = !directHolding;
    }
}




