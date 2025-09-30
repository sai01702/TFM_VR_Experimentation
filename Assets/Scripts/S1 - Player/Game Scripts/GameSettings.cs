using UnityEngine;

public class GameSettings : MonoBehaviour
{
    public static GameSettings Instance { get; private set; }
    public GameMode CurrentMode { get; private set; } = GameMode.Desktop;
    const string Key = "game_mode";

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (PlayerPrefs.HasKey(Key))
            CurrentMode = (GameMode)PlayerPrefs.GetInt(Key, (int)GameMode.Desktop);
    }

    public void SetMode(GameMode mode, bool save = true)
    {
        CurrentMode = mode;
        if (save) PlayerPrefs.SetInt(Key, (int)mode);
    }

    public bool HasSavedMode() => PlayerPrefs.HasKey(Key);
}