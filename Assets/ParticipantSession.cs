using System;
using System.IO;
using UnityEngine;

public class ParticipantSession : MonoBehaviour
{
    public static ParticipantSession Instance { get; private set; }

    public string ParticipantId { get; private set; }

    [Header("Log File Settings")]
    [Tooltip("Optional override. If empty, Application.persistentDataPath is used.")]
    public string customLogDirectory;

    public string LogFilePath => _logFilePath;

    string _logFilePath;
    bool _logInitialized;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        string dataFolder = System.DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        string dailyPath = Path.Combine(Application.persistentDataPath, "Logs", dataFolder);

        if (!Directory.Exists(dailyPath))
            {
            Directory.CreateDirectory(dailyPath);
            Debug.Log($"[ParticipantSession] Created log directory at: {dailyPath}");
        }

    }

    // Called once on the login scene when the player presses Save
    public void SetParticipant(string id)
    {
        ParticipantId = string.IsNullOrWhiteSpace(id) ? "Unknown" : id.Trim();
        InitLogFileIfNeeded();
    }

    void InitLogFileIfNeeded()
    {
        if (_logInitialized) return;

        // --- choose directory ---
        string dir = string.IsNullOrWhiteSpace(customLogDirectory)
            ? Application.persistentDataPath
            : customLogDirectory;

        if (!Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        // --- build file name: Participant(id)-yyyyMMdd-HHmmss-log.txt ---
        string timestamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");
        string fileName = $"Participant({ParticipantId})-{timestamp}-log.txt";

        _logFilePath = Path.Combine(dir, fileName);

        // --- header ---
        File.WriteAllText(_logFilePath,
            $"Participant: {ParticipantId}\n" +
            $"Session start: {DateTime.Now:G}\n" +
            "=========================================\n\n");

        _logInitialized = true;

        Debug.Log($"[ParticipantSession] Log file created at: {_logFilePath}");
    }

    public void AppendLog(string line)
    {
        InitLogFileIfNeeded();
        File.AppendAllText(_logFilePath, line + "\n");
        Debug.Log($"[ParticipantSession] {line}");
    }
}
