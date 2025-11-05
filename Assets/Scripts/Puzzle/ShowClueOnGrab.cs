using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class ShowClueOnGrab : MonoBehaviour
{
    [Tooltip("El Clue correspondiente a este sombrero")]
    public GameObject clueToShow;

    private XRGrabInteractable grab;

    void Awake()
    {
        grab = GetComponent<XRGrabInteractable>(); // same as your fully-qualified type
        if (grab != null)
        {
            grab.selectEntered.AddListener(OnGrabbed);
            grab.selectExited.AddListener(OnReleased);
        }
    }

    void OnDestroy()
    {
        if (grab != null)
        {
            grab.selectEntered.RemoveListener(OnGrabbed);
            grab.selectExited.RemoveListener(OnReleased);
        }
    }

    // === XR callbacks (VR path) ===
    private void OnGrabbed(SelectEnterEventArgs _)
    {
        HandleGrab();   // use the common path
    }

    private void OnReleased(SelectExitEventArgs _)
    {
        HandleRelease(); // use the common path
    }

    // === PUBLIC methods you can call from Desktop grab code ===
    public void HandleGrab()
    {
        HideAllClues();

        if (clueToShow != null)
            clueToShow.SetActive(true);

        // logs (optional)
        if (PuzzleLogsManager.Instance != null)
        {
            PuzzleLogsManager.Instance.RegistrarAgarreSombrero(gameObject.name);
            if (clueToShow != null)
                PuzzleLogsManager.Instance.RegistrarMostrarPista(gameObject.name);
        }
    }

    public void HandleRelease()
    {
        if (clueToShow != null)
            clueToShow.SetActive(false);

        if (PuzzleLogsManager.Instance != null)
            PuzzleLogsManager.Instance.RegistrarSueltaSombrero(gameObject.name);
    }

    private void HideAllClues()
    {
        var cluesParent = GameObject.Find("CluesPanels"); // make sure this name matches your hierarchy
        if (cluesParent == null) return;

        foreach (Transform t in cluesParent.transform)
            t.gameObject.SetActive(false);
    }
}
