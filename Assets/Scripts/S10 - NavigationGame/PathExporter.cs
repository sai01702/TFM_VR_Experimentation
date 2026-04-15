using UnityEngine;
using UnityEngine.AI;
using System.IO; // Needed to write files

// This class represents ONE point in a JSON-friendly format (UPDATED for analytics system)
[System.Serializable]
public class PathPoint
{
    public float position_x; // X position (Unity X axis)
    public float position_z; // Z position (Unity Z axis)
    public int index;        // Order in path (important for plotting)

    // Constructor: converts Unity Vector3 → our JSON format
    public PathPoint(Vector3 v, int i)
    {
        position_x = v.x;
        position_z = v.z;
        index = i;
    }
}

// Unity cannot directly serialize arrays → we wrap it
[System.Serializable]
public class Wrapper<T>
{
    public T[] items; // This will hold our array

    public Wrapper(T[] items)
    {
        this.items = items;
    }
}

// Main script
public class PathExporter : MonoBehaviour
{
    [Header("References")]
    public Transform startPoint; // Where path starts
    public Transform goalPoint;  // Where path ends

    [Header("Save Settings")]
    public string saveFolder = "Assets/Log/4-NavigationSceneLogs/"; // 👈 CHANGE THIS IN INSPECTOR

    private NavMeshPath path; // Unity path container

    void Start()
    {
        Debug.Log("🔍 Starting path export...");

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
            Debug.LogError("❌ No path data to export!");
            return;
        }

        // Create timestamp (UPDATED - now generated per export)
        string timestamp = System.DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");

        // Create an array to store converted data
        PathPoint[] data = new PathPoint[path.corners.Length];

        // Loop through each corner
        for (int i = 0; i < path.corners.Length; i++)
        {
            // Convert Unity Vector3 → JSON-friendly format
            data[i] = new PathPoint(path.corners[i], i);

            // Debug each point
            Debug.Log("Corner " + i + ": " + path.corners[i]);
        }

        // Convert array into JSON string
        string json = JsonUtility.ToJson(new Wrapper<PathPoint>(data), true);

        // Ensure folder exists
        if (!Directory.Exists(saveFolder))
        {
            Directory.CreateDirectory(saveFolder);
            Debug.Log("📁 Created folder: " + saveFolder);
        }

        // Define file path (NOW CORRECTLY BUILT)
        string filePath = Path.Combine(saveFolder, "pathCorners_" + timestamp + ".json");

        // Write JSON string into file
        File.WriteAllText(filePath, json);

        // Confirm export
        Debug.Log("✅ Path exported to: " + filePath);
    }

    // 🔥 BONUS: Visualize path in Scene view
    void OnDrawGizmos()
    {
        // If no path, do nothing
        if (path == null || path.corners == null) return;

        Gizmos.color = Color.red; // Set color

        // Loop through corners
        for (int i = 0; i < path.corners.Length; i++)
        {
            // Draw a sphere at each corner
            Gizmos.DrawSphere(path.corners[i], 0.2f);

            // Draw a line to next point
            if (i < path.corners.Length - 1)
            {
                Gizmos.DrawLine(path.corners[i], path.corners[i + 1]);
            }
        }
    }
}