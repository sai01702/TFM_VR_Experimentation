using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class HatStandTrigger : MonoBehaviour
{
    [Header("Correct hat name (no '(Clone)')")]
    public string correctHatName;

    private static int correctHatsPlaced = 0;

    // scoring state
    private bool hatAlreadyPlaced = false;
    private GameObject placedCorrectHatGO = null;

    // occupancy state
    private GameObject currentOccupant = null;

    [Header("Cooldown & Debounce")]
    public float regrabCooldownSeconds = 2f;   // after removal
    public float occupyDebounceSeconds = 0.15f; // ignore trig events right after snap
    public float releaseDistance = 0.20f;      // how far from SnapPoint to consider "really left"
    private float cooldownUntil = 0f;
    private float ignoreEventsUntil = 0f;

    [Header("Snapping")]
    public GameObject SnapPoint;
    public float settleKinematicTime = 0.15f;

    void Reset()
    {
        var col = GetComponent<Collider>();
        if (col) col.isTrigger = true;
    }

    void OnTriggerEnter(Collider other) => TryAcceptHat(other, false);
    void OnTriggerStay(Collider other) => TryAcceptHat(other, true);

    void OnTriggerExit(Collider other)
    {
        var root = GetHatRoot(other);
        if (root == null) return;

        // Ignore noisy exits during debounce window
        if (Time.time < ignoreEventsUntil) return;

        // Only clear if this exact occupant is truly away from the snap area
        if (currentOccupant != null && root == currentOccupant)
        {
            if (!IsInsideHoldArea(root))
            {
                // If it was the counted correct hat, undo score
                if (placedCorrectHatGO != null && root == placedCorrectHatGO && hatAlreadyPlaced)
                {
                    hatAlreadyPlaced = false;
                    placedCorrectHatGO = null;
                    correctHatsPlaced--;
                    Debug.Log($"[Stand {name}] Correct hat REMOVED. Total: {correctHatsPlaced}");
                    PuzzleLogsManager.Instance?.RegistrarRemoverSombrero(Clean(root.name), correctHatName);
                }

                currentOccupant = null;
                cooldownUntil = Time.time + regrabCooldownSeconds;
            }
        }
    }

    void TryAcceptHat(Collider other, bool fromStay)
    {
        if (Time.time < cooldownUntil) return;

        var hatRoot = GetHatRoot(other);
        if (hatRoot == null) return;

        // Must be tagged on the root OR collider
        if (!(other.CompareTag("Hat") || hatRoot.CompareTag("Hat"))) return;

        // If stand looks occupied (occupant still inside hold area), refuse any other hat
        if (IsOccupiedByAnother(hatRoot)) return;

        // If same hat already registered as occupant (extra colliders firing), ignore
        if (currentOccupant == hatRoot) return;

        // Accept & snap
        SnapAndSettle(hatRoot);
        currentOccupant = hatRoot;
        ignoreEventsUntil = Time.time + occupyDebounceSeconds;

        // Auto release ONLY for Desktop
        if (IsDesktop())
            ForceHandReleaseDesktop(hatRoot);

        // Scoring
        string incoming = Clean(hatRoot.name);
        bool isCorrect = !string.IsNullOrEmpty(correctHatName) && incoming == correctHatName;

        if (isCorrect && !hatAlreadyPlaced)
        {
            hatAlreadyPlaced = true;
            placedCorrectHatGO = hatRoot;
            correctHatsPlaced++;
            if (fromStay)
                Debug.Log($"(Stay) ✅ '{incoming}' on '{correctHatName}'. Total: {correctHatsPlaced}");
            else
                Debug.Log($"✅ '{incoming}' on '{correctHatName}'. Total: {correctHatsPlaced}");
            PuzzleLogsManager.Instance?.RegistrarColocacionCorrecta(incoming, correctHatName);
        }
        else if (!isCorrect && !fromStay)
        {
            Debug.LogWarning($"❌ Wrong hat '{incoming}' on stand '{correctHatName}'.");
            PuzzleLogsManager.Instance?.RegistrarColocacionIncorrecta(incoming, correctHatName);
        }
    }

    GameObject GetHatRoot(Collider col)
    {
        var rb = col.attachedRigidbody ?? col.GetComponentInParent<Rigidbody>();
        return rb ? rb.gameObject : null;
    }

    bool IsOccupiedByAnother(GameObject incomingRoot)
    {
        if (currentOccupant == null) return false;
        // Treat as occupied only if the current occupant is still inside the hold area
        if (IsInsideHoldArea(currentOccupant) && currentOccupant != incomingRoot)
            return true;
        // If occupant drifted out, free it
        if (!IsInsideHoldArea(currentOccupant))
            currentOccupant = null;
        return false;
    }

    bool IsInsideHoldArea(GameObject hatRoot)
    {
        var refPos = (SnapPoint ? SnapPoint.transform.position : transform.position);
        return Vector3.Distance(hatRoot.transform.position, refPos) <= releaseDistance;
    }

    string Clean(string raw) => raw.Replace("(Clone)", "").Trim();

    void SnapAndSettle(GameObject hatRoot)
    {
        var rb = hatRoot.GetComponent<Rigidbody>();
        if (!rb) return;

        Vector3 pos = SnapPoint ? SnapPoint.transform.position : transform.position;
        Quaternion rot = SnapPoint
            ? Quaternion.Euler(0f, 180f, 0f)                  // keep your fixed yaw with SnapPoint
            : Quaternion.Euler(0f, transform.eulerAngles.y, 0f);

        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.isKinematic = true;
        rb.useGravity = false;
        rb.constraints = RigidbodyConstraints.None;

        hatRoot.transform.SetParent(null, true);
        hatRoot.transform.position = pos;
        hatRoot.transform.rotation = rot;

        StartCoroutine(FinishSnap(rb));
    }

    IEnumerator FinishSnap(Rigidbody rb)
    {
        yield return new WaitForSeconds(settleKinematicTime);
        rb.isKinematic = false;
        rb.useGravity = true;
        rb.constraints = RigidbodyConstraints.FreezeAll; // your HatGrabPhysicsReset frees on grab
    }

    void ForceHandReleaseDesktop(GameObject hatRoot)
    {
        var grabber = FindObjectOfType<DesktopGrabber>();
        grabber?.ForceFullReleaseIfHolding(hatRoot);
    }

    public void ForcePlaceHat(GameObject hatRoot)
    {
        if (!hatRoot) return;
        if (Time.time < cooldownUntil) return;
        if (IsOccupiedByAnother(hatRoot)) return;

        SnapAndSettle(hatRoot);
        currentOccupant = hatRoot;
        ignoreEventsUntil = Time.time + occupyDebounceSeconds;

        if (IsDesktop())
            ForceHandReleaseDesktop(hatRoot);

        string incoming = Clean(hatRoot.name);
        bool isCorrect = !string.IsNullOrEmpty(correctHatName) && incoming == correctHatName;
        if (isCorrect && !hatAlreadyPlaced)
        {
            hatAlreadyPlaced = true;
            placedCorrectHatGO = hatRoot;
            correctHatsPlaced++;
            Debug.Log($"[ForcePlace] ✅ '{incoming}' on '{correctHatName}'. Total: {correctHatsPlaced}");
            PuzzleLogsManager.Instance?.RegistrarColocacionCorrecta(incoming, correctHatName);
        }
        else if (!isCorrect)
        {
            Debug.LogWarning($"[ForcePlace] ❌ '{incoming}' on '{correctHatName}' (not counted).");
            PuzzleLogsManager.Instance?.RegistrarColocacionIncorrecta(incoming, correctHatName);
        }
    }

    static bool IsDesktop() =>
        GameSettings.Instance != null && GameSettings.Instance.CurrentMode == GameMode.Desktop;
}
