using System.Collections;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Exports the baked NavMesh as 2D (x/z) vertices and triangle indices, plus start and end
/// points, for analytics. Output matches the walkable surface used by NavMesh.CalculatePath.
/// If the maze is generated at runtime, assign MazeManager so export runs after generation;
/// the NavMesh must still reflect the scene (rebuild if you change geometry at runtime).
/// </summary>
public class AllLabyrinthPathsExporter : MonoBehaviour
{
    [Header("References")]
    [Tooltip("JSON key: start_point")]
    public Transform startPoint;

    [Tooltip("JSON key: end_point")]
    public Transform goalPoint;

    [Tooltip("If set, waits until MazeManager's generator has finished before exporting.")]
    public MazeManager mazeManager;

    [Header("Save Settings")]
    public string saveFolder = "Assets/Logs/4-NavigationSceneLogs/";

    void Start()
    {
        if (mazeManager != null)
            StartCoroutine(ExportWhenMazeReady());
        else
            Export();
    }

    [ContextMenu("Export Now")]
    public void Export()
    {
        if (startPoint == null || goalPoint == null)
        {
            Debug.LogError("AllLabyrinthPathsExporter: startPoint and goalPoint must be assigned.");
            return;
        }

        NavMeshTriangulation tri = NavMesh.CalculateTriangulation();
        Vector3[] verts = tri.vertices;
        int[] indices = tri.indices;

        if (verts == null || verts.Length == 0)
        {
            Debug.LogError("AllLabyrinthPathsExporter: NavMesh triangulation has no vertices. Is the NavMesh baked and valid?");
            return;
        }

        string timestamp = System.DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        string json = BuildJson(verts, indices, startPoint.position, goalPoint.position);

        if (!Directory.Exists(saveFolder))
        {
            Directory.CreateDirectory(saveFolder);
            Debug.Log("Created folder: " + saveFolder);
        }

        string filePath = Path.Combine(saveFolder, "labyrinthMap_" + timestamp + ".json");
        File.WriteAllText(filePath, json);
        Debug.Log("Labyrinth map exported to: " + filePath);
    }

    IEnumerator ExportWhenMazeReady()
    {
        if (mazeManager != null && mazeManager.loader != null && mazeManager.loader.maze != null)
        {
            while (mazeManager.loader.maze.mazeGenerator != null &&
                   !mazeManager.loader.maze.mazeGenerator.Finish)
            {
                yield return null;
            }
        }

        yield return null;
        Export();
    }

    static string BuildJson(Vector3[] vertices, int[] indices, Vector3 start, Vector3 end)
    {
        var sb = new StringBuilder();
        sb.Append("{\n");
        sb.Append("  \"vertices\": [\n");
        for (int i = 0; i < vertices.Length; i++)
        {
            Vector3 v = vertices[i];
            sb.Append($"    {{ \"x\": {v.x:F3}, \"z\": {v.z:F3} }}");
            if (i < vertices.Length - 1)
                sb.Append(",");
            sb.Append("\n");
        }
        sb.Append("  ],\n");
        sb.Append("  \"indices\": [\n    ");
        for (int i = 0; i < indices.Length; i++)
        {
            sb.Append(indices[i]);
            if (i < indices.Length - 1)
                sb.Append(", ");
            if ((i + 1) % 18 == 0 && i < indices.Length - 1)
                sb.Append("\n    ");
        }
        sb.Append("\n  ],\n");
        sb.Append($"  \"start_point\": {{ \"x\": {start.x:F3}, \"z\": {start.z:F3} }},\n");
        sb.Append($"  \"end_point\": {{ \"x\": {end.x:F3}, \"z\": {end.z:F3} }}\n");
        sb.Append("}");
        return sb.ToString();
    }
}
