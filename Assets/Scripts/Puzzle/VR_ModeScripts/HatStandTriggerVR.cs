using UnityEngine;

public class HatStandTriggerVR : MonoBehaviour
{
    [Header("Correct hat name for this stand")]
    public string correctHatName;

    private static int correctHatsPlaced = 0;

    private bool hatAlreadyPlaced = false;

    private void OnTriggerEnter(Collider other)
    {
        // Validate that the stand is correctly configured
        if (string.IsNullOrEmpty(correctHatName))
        {
            Debug.LogWarning($"HatStandTrigger on '{gameObject.name}' doesn't have correctHatName configured. Ignoring trigger.");
            return;
        }

        if (!hatAlreadyPlaced && other.CompareTag("Hat"))
        {
            if (other.name == correctHatName)
            {
                hatAlreadyPlaced = true;
                correctHatsPlaced++;
                Debug.Log($"Correct hat '{other.name}' on stand for '{correctHatName}'. Total correct: {correctHatsPlaced}");

                // Log in logs
                if (PuzzleLogsManager.Instance != null)
                {
                    PuzzleLogsManager.Instance.RegistrarColocacionCorrecta(other.name, correctHatName);
                }
            }
            else
            {
                Debug.LogWarning($"Incorrect hat '{other.name}' on stand for '{correctHatName}' (GameObject: {gameObject.name})");

                // Log only if it's not the correct hat for THIS specific stand
                if (PuzzleLogsManager.Instance != null)
                {
                    PuzzleLogsManager.Instance.RegistrarColocacionIncorrecta(other.name, correctHatName);
                }
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        // Validate that the stand is correctly configured
        if (string.IsNullOrEmpty(correctHatName))
        {
            return;
        }

        if (hatAlreadyPlaced && other.CompareTag("Hat") && other.name == correctHatName)
        {
            hatAlreadyPlaced = false;
            correctHatsPlaced--;
            Debug.Log($"Hat '{other.name}' removed from stand for '{correctHatName}'. Total correct: {correctHatsPlaced}");

            // Log in logs
            if (PuzzleLogsManager.Instance != null)
            {
                PuzzleLogsManager.Instance.RegistrarRemoverSombrero(other.name, correctHatName);
            }
        }
    }


}
