using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

/// <summary>
/// Attach once per hand and assign every interactor on that hand (direct + ray).
/// When any of them is holding a hat, all the others have allowSelect = false so
/// they cannot pick up another hat. Hover/visuals remain untouched.
/// </summary>
public class SingleHandHatMutex : MonoBehaviour
{
    [SerializeField] List<UnityEngine.XR.Interaction.Toolkit.Interactors.XRBaseInteractor> interactors = new List<UnityEngine.XR.Interaction.Toolkit.Interactors.XRBaseInteractor>();

    void OnEnable()
    {
        foreach (var interactor in interactors)
        {
            if (interactor == null) continue;
            interactor.selectEntered.AddListener(OnSelect);
            interactor.selectExited.AddListener(OnRelease);
        }

        UpdateLocks();
    }

    void OnDisable()
    {
        foreach (var interactor in interactors)
        {
            if (interactor == null) continue;
            interactor.selectEntered.RemoveListener(OnSelect);
            interactor.selectExited.RemoveListener(OnRelease);
            interactor.allowSelect = true;
        }
    }

    void OnSelect(SelectEnterEventArgs _)
    {
        UpdateLocks();
    }

    void OnRelease(SelectExitEventArgs _)
    {
        UpdateLocks();
    }

    void UpdateLocks()
    {
        bool handHoldingHat = AnyInteractorHoldingHat();

        foreach (var interactor in interactors)
        {
            if (interactor == null) continue;

            bool thisInteractorHoldingHat = IsHoldingHat(interactor);
            // The one currently holding a hat must stay enabled so it keeps control.
            // The others are disabled if someone else already has a hat.
            interactor.allowSelect = thisInteractorHoldingHat || !handHoldingHat;
        }
    }

    bool AnyInteractorHoldingHat()
    {
        foreach (var interactor in interactors)
        {
            if (IsHoldingHat(interactor))
                return true;
        }
        return false;
    }

    static bool IsHoldingHat(UnityEngine.XR.Interaction.Toolkit.Interactors.XRBaseInteractor interactor)
    {
        if (interactor == null || !interactor.hasSelection)
            return false;

        foreach (var selected in interactor.interactablesSelected)
        {
            if (selected != null && IsHat(selected))
                return true;
        }

        return false;
    }

    static bool IsHat(UnityEngine.XR.Interaction.Toolkit.Interactables.IXRSelectInteractable interactable)
    {
        if (interactable == null)
            return false;

        var t = interactable.transform;
        if (t == null)
            return false;

        if (t.CompareTag("Hat"))
            return true;

        return t.GetComponent<PlaceHatWithRaycast>() != null;
    }
}



