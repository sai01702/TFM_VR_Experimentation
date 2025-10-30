using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;

public class CheckHatsOnTouch : MonoBehaviour
{
    [Header("Configuración")]
    [Tooltip("Sombreros en la escena")]
    public GameObject[] hats;

    [Tooltip("Stands de sombreros")]
    public HatStandTrigger[] hatStands;

    [Tooltip("Delay antes de cambiar de escena si todos son correctos")]
    public float sceneChangeDelay = 2f;

    [Tooltip("Cooldown en segundos entre verificaciones")]
    public float checkCooldown = 0.5f;

    private Dictionary<GameObject, Vector3> initialHatPositions;
    private Dictionary<GameObject, Quaternion> initialHatRotations;
    private float lastCheckTime = 0f;

    private void Start()
    {
        // Guardar posiciones iniciales de los sombreros
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
    }

    // ─────────────────────────────────────────────
    // VR / physical trigger path
    // ─────────────────────────────────────────────
    private void OnTriggerEnter(Collider other)
    {
        // Original trigger gate, unchanged:
        // only "player"/hands/etc. can activate
        if ((other.CompareTag("Player")
             || other.name.Contains("Hand")
             || other.name.Contains("Controller"))
            && Time.time - lastCheckTime >= checkCooldown)
        {
            RunCheckNow();
        }
    }

    // ─────────────────────────────────────────────
    // Desktop "press E while looking" path
    // This gets called by DesktopButtonInteractor
    // ─────────────────────────────────────────────
    public void ManualPress()
    {
        // Respect same cooldown so you can't spam
        if (Time.time - lastCheckTime < checkCooldown)
            return;

        Debug.Log("[CheckHatsOnTouch] ManualPress called (desktop).");
        RunCheckNow();
    }

    // ─────────────────────────────────────────────
    // Shared logic: score hats, reset wrong ones,
    // maybe finish puzzle.
    // ─────────────────────────────────────────────
    private void RunCheckNow()
    {
        lastCheckTime = Time.time;

        int correctHats = GetCorrectHatsCount();
        int totalHats = hatStands.Length;

        // Actualizar marcador en la pizarra
        if (CronometerScore.Instance != null)
        {
            CronometerScore.Instance.ActualizarSombreros(correctHats);
        }

        Debug.Log($"Sombreros correctos: {correctHats}/{totalHats}");

        if (correctHats >= totalHats)
        {
            // Todos correctos → victoria
            Debug.Log("🎉 ¡Todos los sombreros están en el lugar correcto!");

            // Registrar completar puzzle en logs
            if (PuzzleLogsManager.Instance != null)
            {
                PuzzleLogsManager.Instance.RegistrarCompletarPuzzle();
            }

            // Guardar resultados en SceneTracker
            if (SceneTracker.Instance != null && PuzzleLogsManager.Instance != null)
            {
                // Obtener estadísticas del puzzle
                int totalIntentos, totalAciertos, totalErrores;
                float tiempoTotal;
                PuzzleLogsManager.Instance.ObtenerEstadisticas(
                    out totalIntentos,
                    out totalAciertos,
                    out totalErrores,
                    out tiempoTotal
                );

                // Calcular precisión
                float accuracy = totalIntentos > 0 ? (float)totalAciertos / totalIntentos * 100f : 0f;

                SceneTracker.Instance.SetPuzzleResults(tiempoTotal, accuracy);
            }

            StartCoroutine(DelayAndLoadScene());
        }
        else
        {
            // resetear sombreros incorrectos
            ResetIncorrectHats();
        }
    }

    // ─────────────────────────────────────────────
    // Cuenta cuántos sombreros correctos están en
    // el stand correcto.
    // ─────────────────────────────────────────────
    private int GetCorrectHatsCount()
    {
        int count = 0;

        foreach (HatStandTrigger stand in hatStands)
        {
            if (stand == null) continue;

            Collider standCollider = stand.GetComponent<Collider>();
            if (standCollider == null) continue;

            // Overlap area of the stand collider to see which hats are inside
            Collider[] collidersInTrigger = Physics.OverlapBox(
                standCollider.bounds.center,
                standCollider.bounds.extents,
                standCollider.transform.rotation
            );

            foreach (Collider col in collidersInTrigger)
            {
                if (col.CompareTag("Hat") && col.name == stand.correctHatName)
                {
                    count++;
                    break; // don't double count this stand
                }
            }
        }

        return count;
    }

    // ─────────────────────────────────────────────
    // Cualquier sombrero que NO esté en su stand
    // correcto vuelve a su posición inicial.
    // ─────────────────────────────────────────────
    private void ResetIncorrectHats()
    {
        foreach (GameObject hat in hats)
        {
            if (hat == null) continue;

            bool isCorrectlyPlaced = false;

            // check if this hat sits in its matching stand
            foreach (HatStandTrigger stand in hatStands)
            {
                if (stand == null) continue;

                if (hat.name == stand.correctHatName)
                {
                    Collider standCollider = stand.GetComponent<Collider>();
                    if (standCollider == null) break;

                    Collider[] collidersInTrigger = Physics.OverlapBox(
                        standCollider.bounds.center,
                        standCollider.bounds.extents,
                        standCollider.transform.rotation
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

            // If not correctly placed, snap back to initial transform
            if (!isCorrectlyPlaced)
            {
                if (initialHatPositions.ContainsKey(hat) && initialHatRotations.ContainsKey(hat))
                {
                    Rigidbody rb = hat.GetComponent<Rigidbody>();
                    if (rb != null)
                    {
                        rb.velocity = Vector3.zero;
                        rb.angularVelocity = Vector3.zero;
                    }

                    hat.transform.position = initialHatPositions[hat];
                    hat.transform.rotation = initialHatRotations[hat];

                    Debug.Log($"🔄 Sombrero '{hat.name}' reseteado a su posición inicial");
                }
            }
        }
    }

    // ─────────────────────────────────────────────
    // Cambio de escena cuando puzzle termina.
    // ─────────────────────────────────────────────
    private IEnumerator DelayAndLoadScene()
    {
        yield return new WaitForSeconds(sceneChangeDelay);

        if (SceneTracker.Instance != null)
        {
            SceneTracker.Instance.PreviousScene = SceneManager.GetActiveScene().name;
        }

        SceneManager.LoadScene("GameOverScene");
    }
}
