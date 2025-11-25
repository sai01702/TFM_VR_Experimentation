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

    [Tooltip("Hat stands")]
    public HatStandTrigger[] hatStands;

    [Tooltip("Delay before changing scene if all are correct")]
    public float sceneChangeDelay = 2f;

    [Tooltip("Cooldown in seconds between checks")]
    public float checkCooldown = 0.5f;

    [Header("Button Materials")]
    [Tooltip("Renderer of the moving/top part of the 3D button (e.g. 'Press' mesh).")]
    public Renderer pressRenderer;          // drag 'Press' mesh renderer here
    public Color normalColor = Color.red;   // will be overwritten by material color at Start
    public Color pressedColor = Color.green;
    public float colorResetDelay = 0.3f;

    private Dictionary<GameObject, Vector3> initialHatPositions;
    private Dictionary<GameObject, Quaternion> initialHatRotations;
    private float lastCheckTime = 0f;

    void Reset()
    {
        var col = GetComponent<Collider>();
        if (col != null) col.isTrigger = true;
    }

    private void Start()
    {
        // Save initial positions of the hats
        initialHatPositions = new Dictionary<GameObject, Vector3>();
        initialHatRotations = new Dictionary<GameObject, Quaternion>();

        foreach (GameObject hat in hats)
        {
            if (hat != null)
            {
                initialHatPositions[hat] = hat.transform.position;
                initialHatRotations[hat] = hat.transform.rotation;
            }
        }

        // Capture initial button color
        if (pressRenderer != null)
            normalColor = pressRenderer.material.color;
    }

    // ───────────── VR trigger path ─────────────
    private void OnTriggerEnter(Collider other)
    {
        if ((other.CompareTag("Player") || other.name.Contains("Hand") || other.name.Contains("Controller")) &&
            Time.time - lastCheckTime >= checkCooldown)
        {
            lastCheckTime = Time.time;
            StartCoroutine(FlashButtonColor());
            CheckAndUpdateHats();
        }
    }

    // ───────────── Desktop path (called from DesktopButtonInteractor) ─────────────
    public void ManualPress()
    {
        if (Time.time - lastCheckTime < checkCooldown)
            return;

        lastCheckTime = Time.time;
        StartCoroutine(FlashButtonColor());
        CheckAndUpdateHats();
    }

    // ───────────── Shared logic (same as original VR, plus a second-pass reset) ─────────────
    private void CheckAndUpdateHats()
    {
        int correctHats = GetCorrectHatsCount();
        int totalHats = hatStands.Length;

        // Update marker on the board
        if (CronometerScore.Instance != null)
        {
            CronometerScore.Instance.ActualizarSombreros(correctHats);
        }

        Debug.Log($"Correct hats: {correctHats}/{totalHats}");

        if (correctHats >= totalHats)
        {
            // All hats are correct, change scene
            Debug.Log("🎉 All hats are in the correct place!");

            // Log puzzle completion
            if (PuzzleLogsManager.Instance != null)
            {
                PuzzleLogsManager.Instance.RegistrarCompletarPuzzle();
            }

            // Save results in SceneTracker
            if (SceneTracker.Instance != null && PuzzleLogsManager.Instance != null)
            {
                int totalIntentos, totalAciertos, totalErrores;
                float tiempoTotal;
                PuzzleLogsManager.Instance.ObtenerEstadisticas(
                    out totalIntentos, out totalAciertos, out totalErrores, out tiempoTotal);

                float accuracy = totalIntentos > 0 ? (float)totalAciertos / totalIntentos * 100 : 0;
                SceneTracker.Instance.SetPuzzleResults(tiempoTotal, accuracy);
            }

            StartCoroutine(DelayAndLoadScene());
        }
        else
        {
            // FIRST PASS: same as original
            ResetIncorrectHats();

            // SECOND PASS: simulate “second press” after physics has had a frame to update
            StartCoroutine(SecondPassReset());
        }
    }

    private IEnumerator SecondPassReset()
    {
        // wait for one physics step so any hats that started falling finish moving out of triggers
        yield return new WaitForFixedUpdate();
        ResetIncorrectHats();
    }

    private int GetCorrectHatsCount()
    {
        int count = 0;

        foreach (HatStandTrigger stand in hatStands)
        {
            // Check if there's a correct hat in this stand
            Collider[] collidersInTrigger = Physics.OverlapBox(
                stand.transform.position,
                stand.GetComponent<Collider>().bounds.size / 2,
                stand.transform.rotation
            );

            foreach (Collider col in collidersInTrigger)
            {
                if (col.CompareTag("Hat") && col.name == stand.correctHatName)
                {
                    count++;
                    break;
                }
            }
        }

        return count;
    }

    private void ResetIncorrectHats()
    {
        foreach (GameObject hat in hats)
        {
            if (hat == null) continue;

            bool isCorrectlyPlaced = false;

            // Check if this hat is in the correct stand
            foreach (HatStandTrigger stand in hatStands)
            {
                if (hat.name == stand.correctHatName)
                {
                    // Check if it's in the correct stand's trigger
                    Collider[] collidersInTrigger = Physics.OverlapBox(
                        stand.transform.position,
                        stand.GetComponent<Collider>().bounds.size / 2,
                        stand.transform.rotation
                    );

                    foreach (Collider col in collidersInTrigger)
                    {
                        if (col.gameObject == hat)
                        {
                            isCorrectlyPlaced = true;
                            break;
                        }
                    }
                    break;
                }
            }

            // If not correctly placed, reset position
            if (!isCorrectlyPlaced)
            {
                if (initialHatPositions.ContainsKey(hat) && initialHatRotations.ContainsKey(hat))
                {
                    // Temporarily disable physics to avoid interference
                    Rigidbody rb = hat.GetComponent<Rigidbody>();
                    if (rb != null)
                    {
                        rb.velocity = Vector3.zero;
                        rb.angularVelocity = Vector3.zero;
                    }

                    hat.transform.position = initialHatPositions[hat];
                    hat.transform.rotation = initialHatRotations[hat];

                    Debug.Log($"🔄 Hat '{hat.name}' reset to initial position");
                }
            }
        }
    }

    private IEnumerator DelayAndLoadScene()
    {
        yield return new WaitForSeconds(sceneChangeDelay);
        if (SceneTracker.Instance != null)
        {
            SceneTracker.Instance.PreviousScene = SceneManager.GetActiveScene().name;
        }
        SceneManager.LoadScene("GameOverScene");
    }

    // ───────────── Button color feedback ─────────────
    private IEnumerator FlashButtonColor()
    {
        if (pressRenderer == null)
            yield break;

        pressRenderer.material.color = pressedColor;
        yield return new WaitForSeconds(colorResetDelay);
        pressRenderer.material.color = normalColor;
    }
}