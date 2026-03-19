using MazeGen;
using System.Collections;
using UnityEngine;

public class MazeManager : MonoBehaviour
{
    public static MazeManager Instance;

    public MazeLoader loader;

    public Transform playerRig;
    public Transform playerSpawnPoint;

    public bool useRandomSeed = true;
    public int fixedSeed = 12345;

    private Transform mazeExit;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        StartCoroutine(GenerateMazeRoutine());
    }

    IEnumerator GenerateMazeRoutine()
    {
        if (loader == null)
        {
            Debug.LogError("MazeLoader not assigned!");
            yield break;
        }

        // 🔥 Set seed (SAFE)
        if (loader.maze != null && loader.maze.mazeSettings != null)
        {
            if (useRandomSeed)
                loader.maze.mazeSettings.seed = Random.Range(0, 100000);
            else
                loader.maze.mazeSettings.seed = fixedSeed;

            Debug.Log("Maze Seed: " + loader.maze.mazeSettings.seed);
        }

        mazeExit = null;

        // 🧹 Clean previous maze
        loader.Destroy();
        yield return null;

        // 🔥 ONLY LOAD (no Create!)
        loader.Load(true, true);

        // 🔥 Wait until generation finishes
        while (loader.maze != null &&
               loader.maze.mazeGenerator != null &&
               !loader.maze.mazeGenerator.Finish)
        {
            yield return null;
        }

        // 🔥 Wait for exit registration
        float timeout = 5f;
        float timer = 0f;

        while (mazeExit == null && timer < timeout)
        {
            timer += Time.deltaTime;
            yield return null;
        }

        if (mazeExit == null)
        {
            Debug.LogError("Maze exit not found!");
        }

        MovePlayerToStart();
    }

    void MovePlayerToStart()
    {
        if (playerRig != null && playerSpawnPoint != null)
        {
            playerRig.position = playerSpawnPoint.position + Vector3.up * 1f;
        }
    }

    public void RegisterExit(Transform exitTransform)
    {
        mazeExit = exitTransform;
        Debug.Log("Exit registered: " + exitTransform.name);
    }

    public Transform GetExit()
    {
        return mazeExit;
    }
}