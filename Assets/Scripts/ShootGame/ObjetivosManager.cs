using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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

        InitLogFile();   // <- prepare log
        Spawn();
        tiempo = Time.time + tiempoRespawn;
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
            SafeAppendLog($"Reaction time: {momentodeDespawn - momentodeSpawn:F3} seconds");
            SafeAppendLog("");   // blank line
            desdeF = true;
        }
        else
        {
            if (!desdeF)
            {
                SafeAppendLog("Manual despawn");
                SafeAppendLog("");
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
        if (string.IsNullOrEmpty(logPath)) return;

        try
        {
            File.AppendAllText(logPath, line + "\n");
            // Optional: Debug.Log($"[ObjetivosManager] LOG: {line}");
        }
        catch (Exception ex)
        {
            Debug.LogError("[ObjetivosManager] Error writing log: " + ex);
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
}