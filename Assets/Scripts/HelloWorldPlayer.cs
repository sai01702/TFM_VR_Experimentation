using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Player component for the HelloWorld example.
/// </summary>
public class HelloWorldPlayer : NetworkBehaviour
{
    /// <summary>
    /// Moves the player to a new position.
    /// </summary>
    public void Move()
    {
        if (!IsOwner) return;

        // Example: Move the player a random distance
        var randomX = Random.Range(-10f, 10f);
        var randomZ = Random.Range(-10f, 10f);
        
        transform.position = new Vector3(randomX, transform.position.y, randomZ);
    }
}
