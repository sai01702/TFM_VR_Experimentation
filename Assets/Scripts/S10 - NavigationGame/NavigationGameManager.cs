using UnityEngine;
using UnityEngine.SceneManagement;

public class NavigationGameManager : MonoBehaviour
{
    public static NavigationGameManager Instance;

    float _roundStartTime;
    bool _timerActive;

    /// <summary>Completion times for the last finished session (seconds), set when each round ends.</summary>
    public float LastRound1Seconds { get; private set; }
    public float LastRound2Seconds { get; private set; }

    void Awake()
    {
        Instance = this;
    }

    /// <summary>Called when loading finishes and when round 2 begins (session-controlled).</summary>
    public void BeginRoundTimer()
    {
        _roundStartTime = Time.time;
        _timerActive = true;
        
        try {
            VRLogger.LoggerService.LogEvent("task_start", "task_start", 1, null);
        } catch (System.Exception) { }
    }

    public void NotifyRoundGoalReached(int roundIndex)
    {
        if (!_timerActive)
            return;

        float elapsed = Time.time - _roundStartTime;
        if (roundIndex == 1)
            LastRound1Seconds = elapsed;
        else if (roundIndex == 2)
            LastRound2Seconds = elapsed;

        Debug.Log($"Navigation round {roundIndex} completion time: {elapsed:F2}s");
        _timerActive = false;
        
        try {
            VRLogger.LoggerService.LogEvent("task_end", "task_end", "success", null);
        } catch (System.Exception) { }
    }

    /// <summary>Direct play in NavigationScene without <see cref="NavigationSessionController"/>.</summary>
    void Start()
    {
        if (NavigationSessionController.Instance != null)
            return;
        BeginRoundTimer();
    }

    public void PlayerReachedGoal()
    {
        if (NavigationSessionController.Instance != null)
            return;

        float time = Time.time - _roundStartTime;
        Debug.Log("Goal reached! Time: " + time);
        
        try {
            VRLogger.LoggerService.LogEvent("task_end", "task_end", "success", null);
        } catch (System.Exception) { }
        
        EndGame();
    }

    public void EndNavigationSession()
    {
        Debug.Log("Navigation session finished (both rounds complete).");

        int guide = -1;
        int maze = -1;
        NavigationLobbyPrefs.TryGet(out guide, out maze);

        NavigationParticipantLog.LogSessionEnd(LastRound1Seconds, LastRound2Seconds, guide, maze);

        if (SceneTracker.Instance != null)
        {
            SceneTracker.Instance.PreviousScene = SceneManager.GetActiveScene().name;
            SceneTracker.Instance.SetNavigationResults(LastRound1Seconds, LastRound2Seconds, guide, maze);
        }

        SceneManager.LoadScene("GameOverScene");
    }

    void EndGame()
    {
        Debug.Log("Navigation finished.");
    }
}
