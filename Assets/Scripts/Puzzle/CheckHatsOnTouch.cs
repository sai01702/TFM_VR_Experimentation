using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(Collider))]
public class CheckHatsOnTouch : MonoBehaviour
{
    [Header("Configuration")]
    [Tooltip("Hats in the scene")]
    public GameObject[] hats;

    [Tooltip("Hat stands (each with a Collider and a correctHatName)")]
    public HatStandTrigger[] hatStands;

    [Tooltip("Delay before changing scene if all are correct")]
    public float sceneChangeDelay = 2f;

    [Tooltip("Cooldown in seconds between checks (applies to VR trigger and desktop manual press)")]
    public float checkCooldown = 0.5f;

    private Dictionary<GameObject, Vector3> _initialHatPositions;
    private Dictionary<GameObject, Quaternion> _initialHatRotations;
    private float _lastCheckTime = -999f;

    void Reset()
    {
        // Ensure this collider is configured as a trigger (VR presses)
        var col = GetComponent<Collider>();
        if (col != null) col.isTrigger = true;
    }

    void Start()
    {
        // Save initial transforms
        _initialHatPositions = new Dictionary<GameObject, Vector3>();
        _initialHatRotations = new Dictionary<GameObject, Quaternion>();

        foreach (var hat in hats)
        {
            if (hat == null) continue;
            _initialHatPositions[hat] = hat.transform.position;
            _initialHatRotations[hat] = hat.transform.rotation;
        }
    }

    // ─────────────────────────────────────────────
    // VR path: physical trigger by player/hand/controller
    // ─────────────────────────────────────────────
    void OnTriggerEnter(Collider other)
    {
        if (!IsVrActivator(other)) return;

        if (Time.time - _lastCheckTime >= checkCooldown)
        {
            _lastCheckTime = Time.time;
            CheckAndUpdateHats();
        }
    }

    // Optional: if you want the cube to respond even when hand lingers
    void OnTriggerStay(Collider other)
    {
        if (!IsVrActivator(other)) return;

        if (Time.time - _lastCheckTime >= checkCooldown)
        {
            _lastCheckTime = Time.time;
            CheckAndUpdateHats();
        }
    }

    // Helper to decide if the collider belongs to the VR player/hands
    bool IsVrActivator(Collider other)
    {
        // Same gates you had in the base script
        return other.CompareTag("Player")
            || other.name.Contains("Hand")
            || other.name.Contains("Controller");
    }

    // ─────────────────────────────────────────────
    // Desktop path: called by DesktopButtonInteractor when user looks & presses E
    // ─────────────────────────────────────────────
    public void ManualPress()
    {
        if (Time.time - _lastCheckTime < checkCooldown)
            return;

        _lastCheckTime = Time.time;
        CheckAndUpdateHats();
    }

    // ─────────────────────────────────────────────
    // Shared logic: count correct, reset wrong, maybe finish
    // ─────────────────────────────────────────────
    void CheckAndUpdateHats()
    {
        int correctHats = GetCorrectHatsCount();
        int totalHats = hatStands.Length;

        // Update score board UI if present
        if (CronometerScore.Instance != null)
            CronometerScore.Instance.ActualizarSombreros(correctHats);

        Debug.Log($"Correct hats: {correctHats}/{totalHats}");

        if (correctHats >= totalHats)
        {
            Debug.Log("🎉 All hats are in the correct place!");

            // Logs
            if (PuzzleLogsManager.Instance != null)
                PuzzleLogsManager.Instance.RegistrarCompletarPuzzle();

            // Save results
            if (SceneTracker.Instance != null && PuzzleLogsManager.Instance != null)
            {
                int totalIntentos, totalAciertos, totalErrores;
                float tiempoTotal;
                PuzzleLogsManager.Instance.ObtenerEstadisticas(
                    out totalIntentos, out totalAciertos, out totalErrores, out tiempoTotal);

                float accuracy = totalIntentos > 0 ? (float)totalAciertos / totalIntentos * 100f : 0f;
                SceneTracker.Instance.SetPuzzleResults(tiempoTotal, accuracy);
            }

            StartCoroutine(DelayAndLoadScene());
        }
        else
        {
            ResetIncorrectHats();
        }
    }

    int GetCorrectHatsCount()
    {
        int count = 0;

        foreach (var stand in hatStands)
        {
            if (stand == null) continue;

            var standCol = stand.GetComponent<Collider>();
            if (standCol == null) continue;

            // Use the stand collider’s bounds for robust overlap
            Collider[] inside = Physics.OverlapBox(
                standCol.bounds.center,
                standCol.bounds.extents,
                standCol.transform.rotation);

            foreach (var col in inside)
            {
                if (col.CompareTag("Hat") && col.name == stand.correctHatName)
                {
                    count++;
                    break; // only count one per stand
                }
            }
        }

        return count;
    }

    void ResetIncorrectHats()
    {
        foreach (var hat in hats)
        {
            if (hat == null) continue;

            bool placedCorrectly = false;

            // Is this hat currently inside its matching stand volume?
            foreach (var stand in hatStands)
            {
                if (stand == null) continue;
                if (hat.name != stand.correctHatName) continue;

                var standCol = stand.GetComponent<Collider>();
                if (standCol == null) break;

                Collider[] inside = Physics.OverlapBox(
                    standCol.bounds.center,
                    standCol.bounds.extents,
                    standCol.transform.rotation);

                foreach (var col in inside)
                {
                    if (col.gameObject == hat)
                    {
                        placedCorrectly = true;
                        break;
                    }
                }

                break; // we checked the matching stand
            }

            if (!placedCorrectly)
            {
                if (_initialHatPositions.TryGetValue(hat, out var pos) &&
                    _initialHatRotations.TryGetValue(hat, out var rot))
                {
                    var rb = hat.GetComponent<Rigidbody>();
                    if (rb != null)
                    {
                        rb.velocity = Vector3.zero;
                        rb.angularVelocity = Vector3.zero;
                    }

                    hat.transform.SetPositionAndRotation(pos, rot);
                    Debug.Log($"🔄 Hat '{hat.name}' reset to its initial position.");
                }
            }
        }
    }

    IEnumerator DelayAndLoadScene()
    {
        yield return new WaitForSeconds(sceneChangeDelay);

        if (SceneTracker.Instance != null)
            SceneTracker.Instance.PreviousScene = SceneManager.GetActiveScene().name;

        SceneManager.LoadScene("GameOverScene");
    }
}
