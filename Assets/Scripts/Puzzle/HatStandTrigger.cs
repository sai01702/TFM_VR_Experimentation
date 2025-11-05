using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class HatStandTrigger : MonoBehaviour
{
    [Header("Nombre correcto del sombrero para este stand")]
    [Tooltip("Pon aquí el nombre EXACTO del sombrero correcto (sin '(Clone)')")]
    public string correctHatName;

    // total correct hats across the scene
    private static int correctHatsPlaced = 0;

    // puzzle state for THIS stand
    private bool hatAlreadyPlaced = false;          // did this stand get its correct hat?
    private GameObject placedCorrectHatGO = null;   // which hat counted as correct

    // cooldown so stand doesn't instantly re-grab when you steal a hat
    [Header("Cooldown after removing a hat")]
    [Tooltip("Seconds the stand will IGNORE hats after you take one off.")]
    public float regrabCooldownSeconds = 2f;
    private float cooldownUntil = 0f; // world time until which we ignore snapping

    [Header("Snapping")]
    public GameObject SnapPoint; // Optional: specific point to snap hats to
    [Tooltip("Seconds to keep the hat kinematic while it settles on the stand.")]
    public float settleKinematicTime = 0.15f;

    void Reset()
    {
        var col = GetComponent<Collider>();
        if (col != null) col.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other) => TryAcceptHat(other, fromStay: false);

    private void OnTriggerStay(Collider other) => TryAcceptHat(other, fromStay: true);

    private void OnTriggerExit(Collider other)
    {
        // If the correct hat (the one that counted for score) leaves,
        // undo score and start cooldown.
        if (placedCorrectHatGO != null && other.gameObject == placedCorrectHatGO)
        {
            if (hatAlreadyPlaced)
            {
                hatAlreadyPlaced = false;
                correctHatsPlaced--;
                Debug.Log($"[Stand {name}] Correct hat REMOVED. Total correct now: {correctHatsPlaced}");

                if (PuzzleLogsManager.Instance != null)
                {
                    PuzzleLogsManager.Instance.RegistrarRemoverSombrero(
                        Clean(other.name),
                        correctHatName
                    );
                }
            }

            placedCorrectHatGO = null;
        }

        // no matter what hat left, we trigger cooldown so we don't regrab instantly
        cooldownUntil = Time.time + regrabCooldownSeconds;
    }

    private void TryAcceptHat(Collider other, bool fromStay)
    {
        if (!other.CompareTag("Hat"))
            return;

        if (Time.time < cooldownUntil)
            return;

        GameObject hatGO = other.gameObject;
        string incomingName = Clean(other.name);

        // ALWAYS snap and make sure any hand (desktop or VR) releases
        SnapAndSettle(other);
        ForceHandReleaseDesktop(hatGO);       // desktop release (existing behavior)

        // puzzle correctness logic (unchanged)
        bool isCorrectForThisStand =
            !string.IsNullOrEmpty(correctHatName) &&
            incomingName == correctHatName;

        if (isCorrectForThisStand && !hatAlreadyPlaced)
        {
            hatAlreadyPlaced = true;
            placedCorrectHatGO = hatGO;
            correctHatsPlaced++;

            if (fromStay)
                Debug.Log($"(Stay catch) ✅ Correct hat '{incomingName}' on '{correctHatName}'. Total correct: {correctHatsPlaced}");
            else
                Debug.Log($"✅ Correct hat '{incomingName}' on '{correctHatName}'. Total correct: {correctHatsPlaced}");

            if (PuzzleLogsManager.Instance != null)
            {
                PuzzleLogsManager.Instance.RegistrarColocacionCorrecta(
                    incomingName,
                    correctHatName
                );
            }
        }
        else if (!isCorrectForThisStand && !fromStay)
        {
            Debug.LogWarning($"❌ Wrong hat '{incomingName}' placed on stand '{correctHatName}' (stand object: {gameObject.name})");

            if (PuzzleLogsManager.Instance != null)
            {
                PuzzleLogsManager.Instance.RegistrarColocacionIncorrecta(
                    incomingName,
                    correctHatName
                );
            }
        }
    }

    private string Clean(string raw) => raw.Replace("(Clone)", "").Trim();

    /// <summary>
    /// Put hat on the stand and ensure both Desktop and VR hands have released it.
    /// </summary>
    private void SnapAndSettle(Collider hatCollider)
    {
        if (hatCollider == null) return;

        // --- VR: force release if currently selected by an XR interactor
        var grab = hatCollider.GetComponent<XRGrabInteractable>();
        if (grab != null)
        {
            var xrMgr = grab.interactionManager as XRInteractionManager;
            if (xrMgr != null && grab.interactorsSelecting.Count > 0)
            {
                // Politely ask all selecting interactors to release this grab
                // (copy to array to avoid modifying collection during iteration)
                var selecting = new System.Collections.Generic.List<IXRSelectInteractor>(grab.interactorsSelecting);
                foreach (var interactor in selecting)
                    xrMgr.SelectExit(interactor, grab);
            }

            // Temporarily disable the grab while we snap so it doesn't fight us.
            grab.enabled = false;
        }

        // --- Physics settle while we place
        Rigidbody rb = hatCollider.attachedRigidbody ?? hatCollider.GetComponent<Rigidbody>();
        if (rb == null) return;

        Transform hatT = rb.transform;

        // compute final pose
        Vector3 pos = (SnapPoint != null) ? SnapPoint.transform.position : transform.position;
        Quaternion rot;
        if (SnapPoint != null)
        {
            rot = Quaternion.Euler(0f, 180f, 0f);
            
        }
        else
        {
            // Upright with stand's yaw (your previous behavior used a fixed 180°; keep if you want)
            //var e = transform.eulerAngles;
            rot = SnapPoint.transform.rotation;
        }

        // put in place with a short kinematic settle
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.isKinematic = true;     // no gravity or physics while we snap
        rb.useGravity = false;
        rb.constraints = RigidbodyConstraints.None;

        hatT.SetParent(null, true);
        hatT.position = pos;
        hatT.rotation = rot;

        // finish snap after a brief delay: restore gravity and freeze pose on stand
        StartCoroutine(FinishSnap(rb, grab));
    }

    private IEnumerator FinishSnap(Rigidbody rb, XRGrabInteractable grab)
    {
        yield return new WaitForSeconds(settleKinematicTime);

        // allow resting on the stand but don't drift
        rb.isKinematic = false;
        rb.useGravity = true;
        rb.constraints = RigidbodyConstraints.FreezeAll;

        if (grab != null)
            grab.enabled = true; // re-enable VR grabbing after placement
    }

    // Desktop helper (unchanged)
    private void ForceHandReleaseDesktop(GameObject hatGO)
    {
        DesktopGrabber grabber = FindObjectOfType<DesktopGrabber>();
        if (grabber == null) return;
        grabber.ForceFullReleaseIfHolding(hatGO);
    }

    // Public for desktop ray-drop path
    public void ForcePlaceHat(GameObject hatGO)
    {
        if (hatGO == null) return;

        if (Time.time < cooldownUntil) return;

        Collider col = hatGO.GetComponent<Collider>();
        if (col != null) SnapAndSettle(col);

        ForceHandReleaseDesktop(hatGO);

        string incomingName = Clean(hatGO.name);
        bool isCorrectForThisStand =
            !string.IsNullOrEmpty(correctHatName) &&
            incomingName == correctHatName;

        if (isCorrectForThisStand && !hatAlreadyPlaced)
        {
            hatAlreadyPlaced = true;
            placedCorrectHatGO = hatGO;
            correctHatsPlaced++;

            Debug.Log($"[ForcePlace] ✅ Correct hat '{incomingName}' on '{correctHatName}'. Total correct: {correctHatsPlaced}");

            if (PuzzleLogsManager.Instance != null)
            {
                PuzzleLogsManager.Instance.RegistrarColocacionCorrecta(
                    incomingName,
                    correctHatName
                );
            }
        }
        else if (!isCorrectForThisStand)
        {
            Debug.LogWarning($"[ForcePlace] ❌ Wrong hat '{incomingName}' on stand '{correctHatName}' (not counted).");

            if (PuzzleLogsManager.Instance != null)
            {
                PuzzleLogsManager.Instance.RegistrarColocacionIncorrecta(
                    incomingName,
                    correctHatName
                );
            }
        }
    }
}
