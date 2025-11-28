using UnityEngine;

using UnityEngine.XR.Interaction.Toolkit.Filtering;

/// <summary>
/// Attach to a hand interactor (direct or ray). While that interactor is already
/// holding a hat, this filter returns false so the same hand cannot grab
/// additional hats. It uses the select-filter pipeline so visuals stay intact.
/// </summary>
public class HatSelectBlocker : MonoBehaviour, IXRSelectFilter
{
    [SerializeField] UnityEngine.XR.Interaction.Toolkit.Interactors.XRBaseInteractor interactor;

    void Reset()
    {
        if (interactor == null)
            interactor = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactors.XRBaseInteractor>();
    }

     void OnEnable()
     {
         if (interactor == null)
             interactor = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactors.XRBaseInteractor>();
 
         if (interactor != null)
             interactor.selectFilters.Add(this);
     }

    void OnDisable()
    {
        if (interactor != null)
            interactor.selectFilters.Remove(this);
    }

    public bool canProcess => interactor != null;

    public bool Process(UnityEngine.XR.Interaction.Toolkit.Interactors.IXRSelectInteractor xrInteractor, UnityEngine.XR.Interaction.Toolkit.Interactables.IXRSelectInteractable interactable)
    {
        if (xrInteractor != interactor || interactable == null)
            return true;

        // Only care about hats.
        if (!IsHat(interactable))
            return true;

        // If this interactor is not holding anything yet, allow.
        if (!interactor.hasSelection)
            return true;

        // If it's already holding THIS same hat, allow to keep holding it.
        foreach (var selected in interactor.interactablesSelected)
        {
            if (ReferenceEquals(selected, interactable))
                return true;
        }

        // Already holding a different object -> block this extra hat.
        return false;
    }

    bool IsHat(UnityEngine.XR.Interaction.Toolkit.Interactables.IXRSelectInteractable interactable)
    {
        var t = interactable.transform;
        if (t == null)
            return false;

        if (t.CompareTag("Hat"))
            return true;

        return t.GetComponent<PlaceHatWithRaycast>() != null;
    }
}

