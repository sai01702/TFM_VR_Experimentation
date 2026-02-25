using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRLogger;

public class ObjetivosManager : MonoBehaviour
{
    public static ObjetivosManager Instance;

    [Header("Target Settings")]
    public string tagToFind = "Disparable";
    public float tiempoRespawn = 4f;

    [Header("Log Settings")]
    [Tooltip("Base name for the log file, without extension.")]
    public string logFileBaseName = "reaction-time";

    [Tooltip("Optional ABSOLUTE directory path. If empty, uses Application.persistentDataPath.")]
    public string customLogDirectory;

    private GameObject[] objetivos;
    private GameObject spawneado = null;

    public bool smthSpawned = false;
    public bool fuera = false;
    public float tiempo = 0f;
    public float momentodeSpawn;
    public float momentodeDespawn;
    public bool desdeF = false;
    public int puntos = 0;

    private string logPath;
    private int nlogs = 1;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        objetivos = GameObject.FindGameObjectsWithTag(tagToFind);
        Debug.Log("Objetivos encontrados: " + objetivos.Length);

        PrintAllTaggedObjectNames();
        DisableAllTaggedObjects();

        InitLogFile();   // <- prepare local log file
        InitSessionLog(); // <- also log to ParticipantSession
        
        // VR Logger Integration
        LoggerService.LogEvent("session_start", "level_start");

        Spawn();
        tiempo = Time.time + tiempoRespawn;
    }

    /// <summary>
    /// Write a header to ParticipantSession log for shooter game details
    /// </summary>
    void InitSessionLog()
    {
        if (ParticipantSession.Instance == null) return;

        // Get current game mode
        string gameMode = "Unknown";
        if (GameSettings.Instance != null)
        {
            gameMode = GameSettings.Instance.CurrentMode == GameMode.VR ? "VR" : "Desktop";
        }

        ParticipantSession.Instance.AppendLog("");
        ParticipantSession.Instance.AppendLog("=== Shooter Game - Reaction Time Log ===");
        ParticipantSession.Instance.AppendLog($"Mode: {gameMode}");
        ParticipantSession.Instance.AppendLog($"Started: {DateTime.Now:G}");
        ParticipantSession.Instance.AppendLog("");
    }

    void Update()
    {
        if ((Time.time > tiempo) && !fuera)
        {
            Despawn();
            Spawn();
            tiempo = Time.time + tiempoRespawn;
        }
    }

    // -------------------- SPAWN / DESPAWN --------------------

    public void Spawn()
    {
        if (objetivos == null || objetivos.Length == 0)
        {
            Debug.LogWarning("[ObjetivosManager] No objetivos found to spawn.");
            return;
        }

        int indice = UnityEngine.Random.Range(0, objetivos.Length);
        GameObject obj = objetivos[indice];
        obj.SetActive(true);
        momentodeSpawn = Time.time;

        if (nlogs < 16)
        {
            SafeAppendLog($"Spawned object {nlogs}: {obj.name}");
            nlogs++;
        }

        // VR Logger Integration
        LoggerService.LogEvent("task_start", "spawn_object");

        smthSpawned = true;
        desdeF = false;
        Debug.Log("Objeto activado: " + obj.name);
        spawneado = obj;
    }

    public void Despawn()
    {
        if (spawneado == null) return;

        spawneado.SetActive(false);
        momentodeDespawn = Time.time;
        smthSpawned = false;

        if (fuera)
        {
            float reactionTime = momentodeDespawn - momentodeSpawn;
            SafeAppendLog($"Reaction time: {reactionTime:F3} seconds");
            SafeAppendLog("");   // blank line
            desdeF = true;

            // VR Logger Integration - SUCCESS
            // We log 'task_end' with name 'task_end' (mapped to role task_end) and value 'success' (required by parser)
            LoggerService.LogEvent("task_end", "task_end", "success");

            // We log reaction time here. 'reaction_time' maps to 'performance_measure' which feeds AvgReactionTimeMs
            // We pass the raw time (seconds) - Metrics calculation might need Ms? ExperimentProfile says Ms. 
            // Let's send Ms to be safe or standard Seconds if the system converts it. 
            // Looking at Metrics: AvgReactionTimeMs. Usually unity time is seconds. Let's send seconds and context ms?
            // Actually, let's look at how other metrics are used. Usually floats. 
            // I will log as "reaction_time" with value in seconds. If metric expects Ms, I might need to multiply.
            // Wait, AvgReactionTimeMs Min 100 Max 2000. That looks like Milliseconds.
            LoggerService.LogEvent("performance_measure", "reaction_time", reactionTime * 1000f); 
            
            // Also Context for potential score?
        }
        else
        {
            if (!desdeF)
            {
                SafeAppendLog("Manual despawn");
                SafeAppendLog("");

                // VR Logger Integration - TIMEOUT / FAILURE
                // 'task_timeout' maps to 'task_abandoned' or 'action_fail'. 
                // Plan said: task_timeout -> task_abandoned
                LoggerService.LogEvent("task_abandoned", "task_timeout");
            }
        }

        Debug.Log("Objeto desactivado: " + spawneado.name);
    }

    // -------------------- LOGGING --------------------

    void InitLogFile()
    {
        try
        {
            // choose directory
            string dir = string.IsNullOrWhiteSpace(customLogDirectory)
                ? Application.persistentDataPath      // default safe location
                : customLogDirectory;

            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
                Debug.Log($"[ObjetivosManager] Created log directory: {dir}");
            }

            string baseName = string.IsNullOrWhiteSpace(logFileBaseName)
                ? "reaction-time"
                : logFileBaseName.Trim();

            string timestamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");
            string fileName = $"{baseName}-{timestamp}.txt";

            logPath = Path.Combine(dir, fileName);

            File.WriteAllText(logPath, "Reaction time Log Inmersive\n");
            File.AppendAllText(logPath, $"Created: {DateTime.Now:G}\n");
            File.AppendAllText(logPath, "=====================================\n\n");

            Debug.Log($"[ObjetivosManager] Log file created at: {logPath}");
        }
        catch (Exception ex)
        {
            Debug.LogError("[ObjetivosManager] Error creating log file: " + ex);
            logPath = null;
        }
    }

    void SafeAppendLog(string line)
    {
        // Write to local detailed log file
        if (!string.IsNullOrEmpty(logPath))
        {
            try
            {
                File.AppendAllText(logPath, line + "\n");
            }
            catch (Exception ex)
            {
                Debug.LogError("[ObjetivosManager] Error writing log: " + ex);
            }
        }

        // Also write to ParticipantSession log (main session log)
        if (ParticipantSession.Instance != null && !string.IsNullOrEmpty(line))
        {
            ParticipantSession.Instance.AppendLog(line);
        }
    }

    // -------------------- HELPERS --------------------

    public void PrintAllTaggedObjectNames()
    {
        foreach (GameObject obj in objetivos)
        {
            Debug.Log("Tagged Object: " + obj.name);
        }
    }

    void DisableAllTaggedObjects()
    {
        foreach (GameObject obj in objetivos)
        {
            obj.SetActive(false);
        }
    }

    public void EnableAllTaggedObjects()
    {
        foreach (GameObject obj in objetivos)
        {
            obj.SetActive(true);
        }
    }

    public bool IsAnyObjectActive()
    {
        foreach (GameObject obj in objetivos)
        {
            if (obj.activeSelf)
                return true;
        }
        return false;
    }

    /// <summary>
    /// Called when scene ends - write summary to session log
    /// </summary>
    void OnDestroy()
    {
        if (ParticipantSession.Instance != null)
        {
            ParticipantSession.Instance.AppendLog("");
            ParticipantSession.Instance.AppendLog($"=== Shooter Game Ended ===");
            ParticipantSession.Instance.AppendLog($"End time: {DateTime.Now:G}");
            ParticipantSession.Instance.AppendLog($"Final Score: {puntos}");
            ParticipantSession.Instance.AppendLog($"Total Spawns: {nlogs - 1}");
            ParticipantSession.Instance.AppendLog("");
        }

        // VR Logger Integration
        var context = new Dictionary<string, object>
        {
            { "final_score", puntos },
            { "total_spawns", nlogs - 1 }
        };
        LoggerService.LogEvent("session_end", "level_end", 0, context);
    }
}