using Unity.Netcode;
using UnityEngine;

public class DisableNonOwnerCamera : NetworkBehaviour
{
    Camera cam;

    void Start()
    {
        cam = GetComponent<Camera>();
        if (cam == null)
        {
            cam = GetComponentInChildren<Camera>(true);
        }

        if (cam == null)
        {
            Debug.LogWarning($"No Camera found on {name} or its children for {nameof(DisableNonOwnerCamera)}.");
            return;
        }

        if (!IsOwner)
            cam.enabled = false;
    }
}
