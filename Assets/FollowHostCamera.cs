using UnityEngine;
using Unity.Netcode;

public class FollowHostCamera : MonoBehaviour
{
    Transform target;

    void Start()
    {
        Invoke(nameof(FindHostCamera), 1f);
    }

    void FindHostCamera()
    {
        Camera[] cams = FindObjectsOfType<Camera>();

        foreach (var cam in cams)
        {
            var netObj = cam.GetComponentInParent<NetworkObject>();

            if (netObj != null && netObj.IsOwner)
            {
                target = cam.transform;
                break;
            }
        }
    }

    void LateUpdate()
    {
        if (target == null) return;

        transform.position = target.position;
        transform.rotation = target.rotation;
    }
}