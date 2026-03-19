using UnityEngine;

public class NavigationGameManager : MonoBehaviour
{
    public static NavigationGameManager Instance;

    private float startTime;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        startTime = Time.time;
    }

    public void PlayerReachedGoal()
    {
        float time = Time.time - startTime;

        Debug.Log("Goal reached!");
        Debug.Log("Completion Time: " + time);

        EndGame();
    }

    void EndGame()
    {
        Debug.Log("Navigation finished.");
    }
}