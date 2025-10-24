using UnityEngine;

public class DesktopAimHighlighter : MonoBehaviour
{
    public Camera cam;                 // if null, uses Camera.main
    public float range = 3f;
    public LayerMask mask = ~0;        // set to your Interactable layer if you have one
    public string requiredTag = "Hat"; // only highlight hats

    HatHighlighter current;

    void Awake()
    {
        if (cam == null) cam = Camera.main;
    }

    void Update()
    {
        var next = RaycastForHat();
        if (next != current)
        {
            if (current != null) current.DesktopAimExit();
            current = next;
            if (current != null) current.DesktopAimEnter();
        }
    }

    HatHighlighter RaycastForHat()
    {
        if (cam == null) return null;

        var ray = new Ray(cam.transform.position, cam.transform.forward);
        if (Physics.Raycast(ray, out var hit, range, mask, QueryTriggerInteraction.Ignore))
        {
            if (!hit.collider.CompareTag(requiredTag)) return null;
            return hit.collider.GetComponentInParent<HatHighlighter>();
        }
        return null;
    }

    // Allow DesktopGrabber to query current aiming target (optional)
    public HatHighlighter Current => current;
}