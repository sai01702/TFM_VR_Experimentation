using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class ShowClueOnGrabVR : MonoBehaviour
{
    [Tooltip("El Clue correspondiente a este sombrero")]
    public GameObject clueToShow;

    private UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable grab;

    void Awake()
    {
        grab = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
        grab.selectEntered.AddListener(OnGrabbed);
        grab.selectExited.AddListener(OnReleased);
    }

    private void OnDestroy()
    {
        grab.selectEntered.RemoveListener(OnGrabbed);
        grab.selectExited.RemoveListener(OnReleased);
    }

    private void OnGrabbed(SelectEnterEventArgs args)
    {
        HideAllClues();

        if (clueToShow != null)
            clueToShow.SetActive(true);

        // Registrar en logs
        if (PuzzleLogsManager.Instance != null)
        {
            PuzzleLogsManager.Instance.RegistrarAgarreSombrero(gameObject.name);
            if (clueToShow != null)
            {
                PuzzleLogsManager.Instance.RegistrarMostrarPista(gameObject.name);
            }
        }
    }

    private void OnReleased(SelectExitEventArgs args)
    {
        if (clueToShow != null)
            clueToShow.SetActive(false);

        // Registrar en logs
        if (PuzzleLogsManager.Instance != null)
        {
            PuzzleLogsManager.Instance.RegistrarSueltaSombrero(gameObject.name);
        }
    }

    private void HideAllClues()
    {
        GameObject cluesParent = GameObject.Find("CluesPanels");
        if (cluesParent == null) return;

        foreach (Transform clue in cluesParent.transform)
            clue.gameObject.SetActive(false);
    }
}
