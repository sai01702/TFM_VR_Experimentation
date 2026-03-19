using UnityEngine;

public class MazeExit : MonoBehaviour
{
    void Start()
    {
        // Register this exit in the manager
        MazeManager.Instance.RegisterExit(this.transform);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            NavigationGameManager.Instance.PlayerReachedGoal();
        }
    }
}