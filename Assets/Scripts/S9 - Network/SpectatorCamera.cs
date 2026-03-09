using UnityEngine;

public class SpectatorCamera : MonoBehaviour
{
    public Transform target;
    public Vector3 offset = new Vector3(0, 2, -4);

    void LateUpdate()
    {
        if (target == null) return;

        transform.position = target.position + offset;
        transform.rotation = target.rotation;
    }
}