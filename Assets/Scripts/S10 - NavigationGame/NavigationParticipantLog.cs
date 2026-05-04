using System;
using UnityEngine;

/// <summary>
/// ParticipantSession lines for navigation, mirroring <see cref="ObjetivosManager"/> shooter headers.
/// </summary>
public static class NavigationParticipantLog
{
    public static void LogSessionStart(int guideModeIndex, int mazeIndex)
    {
        if (ParticipantSession.Instance == null)
            return;

        string platform = "Unknown";
        if (GameSettings.Instance != null)
            platform = GameSettings.Instance.CurrentMode == GameMode.VR ? "VR" : "Desktop";

        ParticipantSession.Instance.AppendLog("");
        ParticipantSession.Instance.AppendLog("=== Navigation Game Log ===");
        ParticipantSession.Instance.AppendLog($"Mode: {platform}");
        ParticipantSession.Instance.AppendLog($"Started: {DateTime.Now:G}");
        ParticipantSession.Instance.AppendLog(
            $"Lobby: Guide = {NavigationLobbyPrefs.GetGuideModeDisplayName(guideModeIndex)}, {NavigationLobbyPrefs.GetMazeDisplayName(mazeIndex)}");
        ParticipantSession.Instance.AppendLog("");
    }

    public static void LogSessionEnd(float guidedSeconds, float unguidedSeconds, int guideModeIndex, int mazeIndex)
    {
        if (ParticipantSession.Instance == null)
            return;

        ParticipantSession.Instance.AppendLog("");
        ParticipantSession.Instance.AppendLog("=== Navigation Game Ended ===");
        ParticipantSession.Instance.AppendLog($"End time: {DateTime.Now:G}");
        ParticipantSession.Instance.AppendLog($"Guided round: {guidedSeconds:F3}s");
        ParticipantSession.Instance.AppendLog($"Unguided round: {unguidedSeconds:F3}s");
        ParticipantSession.Instance.AppendLog(
            $"Lobby: Guide = {NavigationLobbyPrefs.GetGuideModeDisplayName(guideModeIndex)}, {NavigationLobbyPrefs.GetMazeDisplayName(mazeIndex)}");
        ParticipantSession.Instance.AppendLog("");
    }
}
