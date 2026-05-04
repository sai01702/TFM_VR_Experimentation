using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class ShowTeleportUI : MonoBehaviour
{
    public GameObject uiTeleportA;
    public GameObject uiTeleportB;
    public GameObject uiTeleportC;
    public GameObject uiTeleportF;

    public enum TeleportType { A, B, C, F }
    public TeleportType teleportType;

    void Awake()
    {
        TryResolveTeleportF();
    }

    /// <summary>If uiTeleportF is not wired in the Inspector, finds inactive <c>UI_TeleportF</c> in this scene (built via Tools / Room / Build UI_TeleportF).</summary>
    void TryResolveTeleportF()
    {
        if (uiTeleportF != null || teleportType != TeleportType.F)
            return;

        foreach (Transform t in Resources.FindObjectsOfTypeAll<Transform>())
        {
            if (t == null || t.name != "UI_TeleportF")
                continue;
            var go = t.gameObject;
            if (!go.scene.IsValid() || go.scene != gameObject.scene)
                continue;
            uiTeleportF = go;
            return;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") || other.GetComponent<XRController>())
        {
            TryResolveTeleportF();
            // Oculta todos primero
            if (uiTeleportA) uiTeleportA.SetActive(false);
            if (uiTeleportB) uiTeleportB.SetActive(false);
            if (uiTeleportC) uiTeleportC.SetActive(false);
            if (uiTeleportF) uiTeleportF.SetActive(false);

            // Muestra el canvas correspondiente
            switch (teleportType)
            {
                case TeleportType.A:
                    if (uiTeleportA) uiTeleportA.SetActive(true);
                    break;
                case TeleportType.B:
                    if (uiTeleportB) uiTeleportB.SetActive(true);
                    break;
                case TeleportType.C:
                    if (uiTeleportC) uiTeleportC.SetActive(true);
                    break;
                case TeleportType.F:
                    if (uiTeleportF) uiTeleportF.SetActive(true);
                    break;
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player") || other.GetComponent<XRController>())
        {
            // Oculta solo el UI correspondiente al tipo de teleport
            switch (teleportType)
            {
                case TeleportType.A:
                    if (uiTeleportA) uiTeleportA.SetActive(false);
                    break;
                case TeleportType.B:
                    if (uiTeleportB) uiTeleportB.SetActive(false);
                    break;
                case TeleportType.C:
                    if (uiTeleportC) uiTeleportC.SetActive(false);
                    break;
                case TeleportType.F:
                    if (uiTeleportF) uiTeleportF.SetActive(false);
                    break;
                
            }
        }
    }
}
