using UnityEngine;
using UnityEngine.AI;
using System.Text;
using System.Diagnostics;

public class RouteInstructionGenerator : MonoBehaviour
{
    public NavigationPathManager pathManager;

    private Vector3[] pathCorners;

    void Start()
    {
        if (pathManager == null)
            pathManager = FindObjectOfType<NavigationPathManager>();

        pathCorners = pathManager.GetPathCorners();

        string fullInstruction = GenerateFullInstructions();

        UnityEngine.Debug.Log("FULL ROUTE:\n" + fullInstruction);

        // Send this to Python (gTTS)

        GenerateAudio(fullInstruction);
    }

    string GenerateFullInstructions()
    {
        if (pathCorners == null || pathCorners.Length < 2)
            return "No valid path.";

        StringBuilder sentence = new StringBuilder();

        for (int i = 0; i < pathCorners.Length - 1; i++)
        {
            Vector3 current = pathCorners[i];
            Vector3 next = pathCorners[i + 1];

            // Distance between points
            float distance = Vector3.Distance(current, next);
            if (distance < 0.5f)
            {
                continue;
            }
            // Direction calculation
            string direction = GetTurnDirection(i);

            // FIRST SEGMENT → no turn yet
            if (i == 0)
            {
                sentence.Append($"Walk forward for {Mathf.Round(distance)} meters. ");
            }
            else
            {
                sentence.Append($"Then turn {direction} and walk for {Mathf.Round(distance)} meters. ");
            }
        }

        sentence.Append("You will reach your destination.");

        return sentence.ToString();
    }

    string GetTurnDirection(int index)
    {
        if (index == 0 || index >= pathCorners.Length - 1)
            return "forward";

        Vector3 prev = pathCorners[index - 1];
        Vector3 current = pathCorners[index];
        Vector3 next = pathCorners[index + 1];

        Vector3 dir1 = (current - prev).normalized;
        Vector3 dir2 = (next - current).normalized;

        float angle = Vector3.SignedAngle(dir1, dir2, Vector3.up);

        if (angle > 30f) return "right";
        if (angle < -30f) return "left";

        return "forward";
    }

    void GenerateAudio(string text)
{
    text = text.Replace("\"", "");

    string pythonFile = Application.dataPath + "/generate_tts.py";

    UnityEngine.Debug.Log("Running Python: " + pythonFile);

    ProcessStartInfo start = new ProcessStartInfo();

    start.FileName = "/Library/Frameworks/Python.framework/Versions/3.14/bin/python3";
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