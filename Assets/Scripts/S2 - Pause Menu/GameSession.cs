using UnityEngine;

// Example place to keep run data; expand with your own fields.
public class GameSession : MonoBehaviour
{
    public static GameSession Instance { get; private set; }

    public int score;
    public int coins;
    public string currentMiniGame;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public static void ClearRunData(bool keepMode = true)
    {
        // Reset in-memory session
        if (Instance != null)
        {
            Instance.score = 0;
            Instance.coins = 0;
            Instance.currentMiniGame = string.Empty;
        }

        // If you saved any run data in PlayerPrefs, clear it here:
        PlayerPrefs.DeleteKey("score");
        PlayerPrefs.DeleteKey("coins");
        PlayerPrefs.DeleteKey("currentMiniGame");

        // Keep the mode by default; delete if you want a fresh choice:
        if (!keepMode) PlayerPrefs.DeleteKey("game_mode");
    }
}