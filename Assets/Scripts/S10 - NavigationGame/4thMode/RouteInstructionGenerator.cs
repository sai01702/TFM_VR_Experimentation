using UnityEngine;
using UnityEngine.AI;
using System.Text;
using System.Diagnostics;
using Debug = UnityEngine.Debug;

public class RouteInstructionGenerator : MonoBehaviour
{
    public NavigationPathManager pathManager;

    private Vector3[] pathCorners;

    [Header("Tuning")]
    public float turnThreshold = 45f;       // Angle to consider a real turn
    public float minSegmentDistance = 0.5f; // Ignore tiny segments

    void Start()
    {
        if (pathManager == null)
            pathManager = FindObjectOfType<NavigationPathManager>();

        if (pathManager == null)
        {
            UnityEngine.Debug.LogError("PathManager not found!");
            return;
        }

        pathCorners = pathManager.GetPathCorners();

        if (pathCorners == null || pathCorners.Length < 2)
        {
            UnityEngine.Debug.LogError("Invalid path.");
            return;
        }

        DebugDrawPath();

        string fullInstruction = GenerateFullInstructions();

        UnityEngine.Debug.Log("FULL ROUTE:\n" + fullInstruction);

        GenerateAudio(fullInstruction);
    }

    string GenerateFullInstructions()
    {
        StringBuilder sentence = new StringBuilder();

        float accumulatedDistance = 0f;

        sentence.Append("Start by walking forward. ");

        for (int i = 1; i < pathCorners.Length - 1; i++)
        {
            Vector3 prev = pathCorners[i - 1];
            Vector3 current = pathCorners[i];
            Vector3 next = pathCorners[i + 1];

            float segmentDistance = Vector3.Distance(prev, current);

            if (segmentDistance < minSegmentDistance)
                continue;

            accumulatedDistance += segmentDistance;

            string turn = GetTurnDirection(prev, current, next);

            // If a real turn happens → emit instruction
            if (turn != "forward")
            {
                sentence.Append(
                    $"Walk forward for {Mathf.RoundToInt(accumulatedDistance)} meters, then turn {turn}. "
                );

                accumulatedDistance = 0f;
            }
        }

        // Final segment to goal
        if (accumulatedDistance > 0f)
        {
            sentence.Append(
                $"Continue straight for {Mathf.RoundToInt(accumulatedDistance)} meters. "
            );
        }

        sentence.Append("You will reach your destination.");

        return sentence.ToString();
    }

    string GetTurnDirection(Vector3 prev, Vector3 current, Vector3 next)
    {
        Vector3 dir1 = (current - prev).normalized;
        Vector3 dir2 = (next - current).normalized;

        float angle = Vector3.SignedAngle(dir1, dir2, Vector3.up);

        // IMPORTANT: Unity angle sign
        if (angle > turnThreshold) return "left";
        if (angle < -turnThreshold) return "right";

        return "forward";
    }

    void DebugDrawPath()
    {
        if (pathCorners == null) return;

        for (int i = 0; i < pathCorners.Length - 1; i++)
        {
            Debug.DrawLine(pathCorners[i], pathCorners[i + 1], Color.green, 10f);
        }

        foreach (var point in pathCorners)
        {
            Debug.DrawLine(point, point + Vector3.up * 2f, Color.red, 10f);
        }
    }

    void GenerateAudio(string text)
    {
        text = text.Replace("\"", "");

        string pythonFile = Application.dataPath + "/generate_tts.py";

        UnityEngine.Debug.Log("Running Python: " + pythonFile);

        ProcessStartInfo start = new ProcessStartInfo();

#if UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
        start.FileName = "/Library/Frameworks/Python.framework/Versions/3.14/bin/python3";
#elif UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
        start.FileName = "python";
#else
        UnityEngine.Debug.LogError("Unsupported platform for TTS.");
        return;
#endif

        start.Arguments = $"\"{pythonFile}\" \"{text}\"";

        start.UseShellExecute = false;
        start.RedirectStandardOutput = true;
        start.RedirectStandardError = true;
        start.CreateNoWindow = false;

        Process process = Process.Start(start);

        string output = process.StandardOutput.ReadToEnd();
        string error = process.StandardError.ReadToEnd();

        process.WaitForExit();

        UnityEngine.Debug.Log("PYTHON OUTPUT:\n" + output);

        if (!string.IsNullOrEmpty(error))
            UnityEngine.Debug.LogError("PYTHON ERROR:\n" + error);
    }
}