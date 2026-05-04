using UnityEngine;

/// <summary>
/// PlayerPrefs keys written when the player confirms START on the navigation lobby (UI_TeleportF).
/// </summary>
public static class NavigationLobbyPrefs
{
    public const string GuideModeKey = "nav_lobby_guide_mode";
    public const string MazeIndexKey = "nav_lobby_maze_index";

    /// <summary>Saved when START is pressed; guide mode is 0 Roadline, 1 Agent, 2 Verbal.</summary>
    public static void Save(int guideModeIndex, int mazeIndex)
    {
        PlayerPrefs.SetInt(GuideModeKey, guideModeIndex);
        PlayerPrefs.SetInt(MazeIndexKey, mazeIndex);
        PlayerPrefs.Save();
    }

    public static bool TryGet(out int guideModeIndex, out int mazeIndex)
    {
        guideModeIndex = PlayerPrefs.GetInt(GuideModeKey, -1);
        mazeIndex = PlayerPrefs.GetInt(MazeIndexKey, -1);
        return guideModeIndex >= 0 && mazeIndex >= 0;
    }
}
