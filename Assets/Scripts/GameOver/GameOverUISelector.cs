using UnityEngine;

public class GameOverUISelector : MonoBehaviour
{
    public GameObject shootGameUI;
    public GameObject obstacleCourseUI;
    public GameObject puzzleUI;
    [Tooltip("Optional panel for NavigationScene results (assign in GameOverScene).")]
    public GameObject navigationUI;

    void Start()
    {
        string prevScene = SceneTracker.Instance != null ? SceneTracker.Instance.PreviousScene : "";

        if (prevScene == "ShooterScene" && shootGameUI != null)
            shootGameUI.SetActive(true);
        else if (prevScene == "ObstacleCourseScene" && obstacleCourseUI != null)
            obstacleCourseUI.SetActive(true);
        else if (prevScene == "PuzzleScene" && puzzleUI != null)
            puzzleUI.SetActive(true);
        else if (prevScene == "NavigationScene" && navigationUI != null)
            navigationUI.SetActive(true);
    }
}
