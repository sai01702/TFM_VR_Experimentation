using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(NetworkObject))]
public class NetworkPlayerController : NetworkBehaviour
{
    public float moveSpeed = 5f;

    void Update()
    {
        // Only allow the owner to control this object
        if (!IsOwner) return;

        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");

        Vector3 move = new Vector3(h, 0f, v);
        transform.Translate(move * moveSpeed * Time.deltaTime);
    }

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            Debug.Log("I own this player! (Client ID: " + OwnerClientId + ")");
        }
    }
}