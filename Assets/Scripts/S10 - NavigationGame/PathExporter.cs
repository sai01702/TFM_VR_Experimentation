using UnityEngine;
using UnityEngine.AI;
using System.IO; // Needed to write files
using System.Text; // Needed to manually build JSON

// This class represents ONE point in a JSON-friendly format (SIMPLIFIED for analytics system)
[System.Serializable]
public class PathPoint
{
    public float x; // X position (Unity X axis)
    public float z; // Z position (Unity Z axis)

    // Constructor: converts Unity Vector3 → our JSON format
    public PathPoint(Vector3 v)
    {
        x = v.x;
        z = v.z;
    }
}

// Main script
public class PathExporter : MonoBehaviour
{
    [Header("References")]
    public Transform startPoint; // Where path starts
    public Transform goalPoint;  // Where path ends

    [Header("Save Settings")]
    public string saveFolder = "Assets/Log/4-NavigationSceneLogs/"; // CHANGE THIS IN INSPECTOR

    private NavMeshPath path; // Unity path container

    void Start()
    {
        Debug.Log("Starting path export...");

        // Create a new empty path object
        path = new NavMeshPath();

        // Calculate path from start → goal using NavMesh
        NavMesh.CalculatePath(
            startPoint.position, // starting position
            goalPoint.position,  // destination
            NavMesh.AllAreas,    // use all walkable areas
            path                 // store result here
        );

        // Print how many corners we got
        Debug.Log("Path corners count: " + path.corners.Length);

        // Export path to JSON file
        ExportPathToJson();
    }

    void ExportPathToJson()
    {
        // Safety check → if no path exists
        if (path == null || path.corners.Length == 0)
        {
            Debug.LogError("No path data to export!");
            return;
        }

        // Create timestamp - generated per export)
        string timestamp = System.DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");

        // Build JSON manually to get PURE ARRAY format (no wrapper)
        StringBuilder json = new StringBuilder();
        json.Append("[\n");

        // Loop through each corner
        for (int i = 0; i < path.corners.Length; i++)
        {
            Vector3 p = path.corners[i];

            // Convert Unity Vector3 → simplified JSON format
            PathPoint point = new PathPoint(p);

            // Write JSON line manually
            json.Append($"  {{ \"x\": {point.x:F3}, \"z\": {point.z:F3} }}");

            // Add comma except for last element
            if (i < path.corners.Length - 1)
                json.Append(",");

            json.Append("\n");

            // Debug each point
            Debug.Log("Corner " + i + ": " + p);
        }

        json.Append("]");

        // Ensure folder exists
        if (!Directory.Exists(saveFolder))
        {
            Directory.CreateDirectory(saveFolder);
            Debug.Log("📁 Created folder: " + saveFolder);
        }

        // Define file path (NOW CORRECTLY BUILT)
        string filePath = Path.Combine(saveFolder, "idealPath_" + timestamp + ".json");

        // Write JSON string into file
        File.WriteAllText(filePath, json.ToString());

        // Confirm export
        Debug.Log("✅ Path exported to: " + filePath);
    }

}