using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

/// <summary>
/// Attach to each XR interactor (direct or ray) on a hand.
/// While that interactor is already holding a hat, its interaction layer mask
/// is temporarily changed so it cannot grab any other hats.
/// </summary>
public class HatGrabLock : MonoBehaviour
{
    [SerializeField] UnityEngine.XR.Interaction.Toolkit.Interactors.XRBaseInteractor interactor;
    [SerializeField] InteractionLayerMask hatLayerMask;

    InteractionLayerMask originalMask;
    bool maskModified;

    void Reset()
    {
        if (interactor == null)
            interactor = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactors.XRBaseInteractor>();
    }

    void OnEnable()
    {
        if (interactor == null)
            interactor = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactors.XRBaseInteractor>();

        if (interactor == null)
            return;

        originalMask = interactor.interactionLayers;
        interactor.selectEntered.AddListener(OnSelect);
        interactor.selectExited.AddListener(OnRelease);
    }

    void OnDisable()
    {
        if (interactor == null)
            return;

        interactor.selectEntered.RemoveListener(OnSelect);
        interactor.selectExited.RemoveListener(OnRelease);

        if (maskModified)
        {
            interactor.interactionLayers = originalMask;
            maskModified = false;
        }
    }

    void OnSelect(SelectEnterEventArgs args)
    {
        if (!IsHat(args.interactableObject))
            return;

        if (maskModified)
            return;

        originalMask = interactor.interactionLayers;
        var newMask = originalMask & ~hatLayerMask;
        interactor.interactionLayers = newMask;
        maskModified = true;
    }

    void OnRelease(SelectExitEventArgs args)
    {
        if (!maskModified)
            return;

        if (IsHat(args.interactableObject))
        {
            interactor.interactionLayers = originalMask;
            maskModified = false;
        }
    }

    bool IsHat(UnityEngine.XR.Interaction.Toolkit.Interactables.IXRSelectInteractable interactable)
    {
        if (interactable == null)
            return false;

        if (interactable.transform.CompareTag("Hat"))
            return true;

        return interactable.transform.GetComponent<PlaceHatWithRaycast>() != null;
    }
}







