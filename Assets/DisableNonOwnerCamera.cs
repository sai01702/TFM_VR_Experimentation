using Unity.Netcode;
using UnityEngine;

public class DisableNonOwnerCamera : NetworkBehaviour
{
    Camera cam;

    void Start()
    {
        cam = GetComponent<Camera>();

        if (!IsOwner)
            cam.enabled = false;
    }
}