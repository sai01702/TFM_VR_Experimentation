using UnityEngine;
using Unity.Netcode;
using UnityEngine.InputSystem;

public class ClientEnvironmentInputBlocker : MonoBehaviour
{
    void Start()
    {
        // Only run on connected clients (spectators), not on host
        if (NetworkManager.Singleton != null &&
            NetworkManager.Singleton.IsClient &&
            !NetworkManager.Singleton.IsHost)
        {
            BlockEnvironmentInteraction();
        }
    }

    void BlockEnvironmentInteraction()
    {
        Debug.Log("Spectator mode active: Blocking environment interaction.");

        // Disable all PlayerInput components (movement, gameplay actions)
        var inputs = FindObjectsOfType<PlayerInput>(true);

        foreach (var input in inputs)
        {
            input.enabled = false;
        }

        // Disable XR ray interactors or other interactors if present
        var behaviours = FindObjectsOfType<MonoBehaviour>(true);

        foreach (var b in behaviours)
        {
            string name = b.GetType().Name;

            if (name.Contains("Interactor") || name.Contains("Grab") || name.Contains("Teleport"))
            {
                b.enabled = false;
            }
        }

        // Lock gameplay cursor interactions
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
}