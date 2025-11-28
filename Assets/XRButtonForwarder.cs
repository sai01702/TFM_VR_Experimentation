using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

[RequireComponent(typeof(XRSimpleInteractable))]
public class XRButtonForwarder : MonoBehaviour
{
    [Tooltip("The CheckHatsOnTouch that should run when this button is pressed.")]
    public CheckHatsOnTouch target;

    private XRSimpleInteractable simple;

    void Awake()
    {
        simple = GetComponent<XRSimpleInteractable>();
        simple.selectEntered.AddListener(OnSelectEntered);
    }

    void OnDestroy()
    {
        if (simple != null)
            simple.selectEntered.RemoveListener(OnSelectEntered);
    }

    void OnSelectEntered(SelectEnterEventArgs _)
    {
        // This is what the ray / hand “click” will trigger
        if (target != null)
            target.ManualPress();
    }
}
