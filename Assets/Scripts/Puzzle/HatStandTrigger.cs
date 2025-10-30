using UnityEngine;

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

    void Reset()
    {
        var col = GetComponent<Collider>();
        if (col != null) col.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        TryAcceptHat(other, fromStay: false);
    }

    private void OnTriggerStay(Collider other)
    {
        TryAcceptHat(other, fromStay: true);
    }

    private void OnTriggerExit(Collider other)
    {
        // If the correct hat (the one that counted for score) leaves,
        // undo score and start cooldown.
        if (placedCorrectHatGO != null && other.gameObject == placedCorrectHatGO)
        {
            // this stand no longer "has" its correct hat
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
        // 0. only care about hats
        if (!other.CompareTag("Hat"))
            return;

        // 1. if we're on cooldown, ignore (don't snap, don't steal from the hand)
        if (Time.time < cooldownUntil)
        {
            return;
        }

        // 2. get data
        GameObject hatGO = other.gameObject;
        string incomingName = Clean(other.name);

        // 3. ALWAYS physically place the hat here and free player hand
        SnapAndSettle(other);
        ForceHandRelease(hatGO);

        // 4. puzzle correctness logic
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
        else if (!isCorrectForThisStand)
        {
            // wrong hat, allowed physically but not counted
            if (!fromStay)
            {
                Debug.LogWarning(
                    $"❌ Wrong hat '{incomingName}' placed on stand '{correctHatName}' (stand object: {gameObject.name})"
                );

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

    private string Clean(string raw)
    {
        return raw.Replace("(Clone)", "").Trim();
    }

    // Put hat on the stand, freeze in place visually, detach from hand
    private void SnapAndSettle(Collider hatCollider)
    {
        if (hatCollider == null) return;

        Rigidbody rb = hatCollider.attachedRigidbody;
        if (rb == null) rb = hatCollider.GetComponent<Rigidbody>();
        if (rb == null) return;

        Transform hatT = rb.transform;

        // Place hat at stand position. Add vertical offset here if needed.
        hatT.position = transform.position;

        // Upright it: keep yaw, kill tilt
        Vector3 e = hatT.eulerAngles;
        hatT.rotation = Quaternion.Euler(0f, e.y, 0f);

        // Stop physics motion
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        // We don't want hats drifting or falling off.
        rb.isKinematic = false;
        rb.useGravity = true;
        rb.constraints = RigidbodyConstraints.FreezeAll;

        // Make sure it's not still parented to hand
        hatT.SetParent(null, true);
    }

    // Tell DesktopGrabber "you don't hold this anymore"
    private void ForceHandRelease(GameObject hatGO)
    {
        DesktopGrabber grabber = FindObjectOfType<DesktopGrabber>();
        if (grabber == null) return;

        grabber.ForceFullReleaseIfHolding(hatGO);
    }

    // This is called when Desktop drops while looking at a stand.
    // We keep same behavior: place physically, clear hand, then do scoring.
    public void ForcePlaceHat(GameObject hatGO)
    {
        if (hatGO == null) return;

        // respect cooldown here too: if you're still in cooldown,
        // do nothing special — DesktopGrabber already detached it.
        if (Time.time < cooldownUntil)
        {
            return;
        }

        Collider col = hatGO.GetComponent<Collider>();
        if (col != null)
        {
            SnapAndSettle(col);
        }

        ForceHandRelease(hatGO);

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
