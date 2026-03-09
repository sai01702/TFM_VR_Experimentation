using Unity.Netcode;
using UnityEngine;

public class NetworkPlayerController : NetworkBehaviour
{
    public float moveSpeed = 5f;

    void Update()
    {
        if (!IsOwner) return;

        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");

        Vector3 move = new Vector3(h, 0, v);

        transform.Translate(move * moveSpeed * Time.deltaTime);
    }

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            Debug.Log("I control this player");
        }
    }
}