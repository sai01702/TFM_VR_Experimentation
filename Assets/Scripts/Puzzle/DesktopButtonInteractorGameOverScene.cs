using UnityEngine;
using UnityEngine.InputSystem;

public class DesktopButtonInteractorGameOverScene : MonoBehaviour
{
    [Header("References")]
    public Camera playerCamera;

    [Header("Settings")]
    public float interactDistance = 4f;
    public Key pressKey = Key.E;

    void Awake()
    {
        if (playerCamera == null)
        {
            playerCamera = Camera.main;
            if (playerCamera == null)
            {
                Debug.LogWarning("[DesktopButtonInteractor] No camera assigned and no MainCamera found.");
            }
        }
    }

    void Update()
    {
        bool keyPressed = Keyboard.current != null && Keyboard.current[pressKey].wasPressedThisFrame;
        bool mousePressed = Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
        if (!keyPressed && !mousePressed) return;

        TryPressButton();
    }

    void TryPressButton()
    {
        if (playerCamera == null)
        {
            Debug.LogError("[DesktopButtonInteractor] No player camera assigned!");
            return;
        }

        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        Debug.DrawRay(ray.origin, ray.direction * interactDistance, Color.cyan, 1f);

        if (Physics.Raycast(ray, out RaycastHit hit, interactDistance, ~0, QueryTriggerInteraction.Collide))
        {
            Debug.Log($"[DesktopButtonInteractor] HIT: {hit.collider.name} at distance {hit.distance}");

            CheckHatsOnTouch check = hit.collider.GetComponent<CheckHatsOnTouch>();
            if (check == null)
                check = hit.collider.GetComponentInParent<CheckHatsOnTouch>();

            if (check != null)
            {
                Debug.Log("[DesktopButtonInteractor] Found CheckHatsOnTouch → calling ManualPress()");
                check.ManualPress();
            }
            else
            {
                Debug.LogWarning("[DesktopButtonInteractor] Hit object but it has NO CheckHatsOnTouch component.");
            }
        }
        else
        {
            Debug.Log("[DesktopButtonInteractor] Raycast missed! (Try increasing interactDistance or aim closer)");
        }
    }
}
