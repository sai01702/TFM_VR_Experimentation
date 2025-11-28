using UnityEngine;

public class GameOverLogger : MonoBehaviour
{
    void Start()
    {
        if (ParticipantSession.Instance == null || SceneTracker.Instance == null)
            return;

        string prev = SceneTracker.Instance.PreviousScene;
        string line = $"[GameOver] From scene '{prev}' - ";

        switch (prev)
        {
            case "ShooterScene":
                line += $"Score = {SceneTracker.Instance.shooterScore}";
                break;

            case "ObstacleCourseScene":
                line += $"Time = {SceneTracker.Instance.obstacleCourseTime:F1}s, " +
                        $"Success = {SceneTracker.Instance.obstacleCourseSuccessRate:F1}%";
                break;

            case "PuzzleScene":
                line += $"Time = {SceneTracker.Instance.puzzleTime:F1}s, " +
                        $"Accuracy = {SceneTracker.Instance.puzzleAccuracy:F1}%";
                break;

            default:
                line += "Unknown scene.";
                break;
        }

        ParticipantSession.Instance.AppendLog(line);
        ParticipantSession.Instance.AppendLog("Session finished.\n");
    }
}
