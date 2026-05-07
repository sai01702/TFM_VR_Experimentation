using System.Collections;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Exports the baked NavMesh as 2D (x/z) vertices and triangle indices, plus start and end
/// points, for analytics. Triangulation is the full baked NavMesh (not filtered to one maze subtree).
/// Start/end in JSON are NavMesh-snapped like <see cref="NavigationPathManager"/> uses for
/// <see cref="NavMesh.CalculatePath"/>. End is resolved from active objects tagged MazeExit via
/// <see cref="NavigationPathManager.TryResolveMazeExitGoal"/> when no manual goal is set; optionally assign
/// <see cref="pathManager"/> so export waits until the session has assigned <see cref="NavigationPathManager.goalPoint"/>.
/// </summary>
public class AllLabyrinthPathsExporter : MonoBehaviour
{
    const float NavMeshSampleMaxDistance = 4f;

    [Header("References")]
    [Tooltip("Used when pathManager is null. When pathManager is set, pathManager.startPoint is used instead.")]
    public Transform startPoint;

    [Tooltip("Optional manual goal when pathManager is null and MazeExit resolution is unavailable. Ignored when pathManager provides goalPoint.")]
    public Transform goalPoint;

    [Tooltip("When pathManager is null: optional root to limit MazeExit search (same as NavigationPathManager.limitSearchToSubtree). Leave empty to search all loaded scenes.")]
    public Transform limitSearchToSubtree;

    [Tooltip("If set, waits until goalPoint is assigned (e.g. NavigationSessionController + TryAssignGoalFromMazeExit), then exports.")]
    public NavigationPathManager pathManager;

    [Header("Save Settings")]
    public string saveFolder = "Assets/Logs/4-NavigationSceneLogs/";

    [Tooltip("Max seconds to wait for pathManager.goalPoint before giving up (NavigationScene session assigns it asynchronously).")]
    public float waitForGoalTimeoutSeconds = 60f;

    void Start()
    {
        if (pathManager != null)
            StartCoroutine(ExportWhenPathReady());
        else
            Export();
    }

    IEnumerator ExportWhenPathReady()
    {
        float t = 0f;
        while (pathManager != null && pathManager.goalPoint == null && t < waitForGoalTimeoutSeconds)
        {
            t += Time.deltaTime;
            yield return null;
        }

        if (pathManager != null && pathManager.goalPoint == null)
        {
            pathManager.TryAssignGoalFromMazeExit();
            if (pathManager.goalPoint == null)
            {
                Debug.LogWarning(
                    "AllLabyrinthPathsExporter: pathManager.goalPoint was not set within " + waitForGoalTimeoutSeconds +
                    " s — export skipped. Ensure an active MazeExit under the session maze, assign optional goalPoint fallback, or increase waitForGoalTimeoutSeconds.");
                yield break;
            }
        }

        yield return null;
        yield return null;
        Export();
    }

    [ContextMenu("Export Now")]
    public void Export()
    {
        Transform startT = pathManager != null ? pathManager.startPoint : startPoint;
        Transform goalT = null;

        if (pathManager != null)
        {
            if (pathManager.goalPoint == null)
                pathManager.TryAssignGoalFromMazeExit();
            goalT = pathManager.goalPoint != null ? pathManager.goalPoint : goalPoint;
        }
        else
        {
            if (goalPoint != null)
                goalT = goalPoint;
            else if (NavigationPathManager.TryResolveMazeExitGoal(startT, limitSearchToSubtree, out Transform resolved))
                goalT = resolved;
        }

        if (startT == null || goalT == null)
        {
            Debug.LogError(
                "AllLabyrinthPathsExporter: start and goal required — set startPoint, assign pathManager and wait for MazeExit, set optional goalPoint, or ensure an active MazeExit (see NavigationPathManager.TryResolveMazeExitGoal).");
            return;
        }

        if (!TrySampleNavMesh(startT.position, out Vector3 startSnapped))
        {
            Debug.LogError(
                "AllLabyrinthPathsExporter: Start is not on (or near) the NavMesh. Move start onto the walkable floor or re-bake NavMesh.");
            return;
        }

        if (!TrySampleNavMesh(goalT.position, out Vector3 goalSnapped))
        {
            Debug.LogError(
                "AllLabyrinthPathsExporter: Goal is not on (or near) the NavMesh. Lower the exit / goal onto the floor or re-bake NavMesh.");
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
        string json = BuildJson(verts, indices, startSnapped, goalSnapped);

        if (!Directory.Exists(saveFolder))
        {
            Directory.CreateDirectory(saveFolder);
            Debug.Log("Created folder: " + saveFolder);
        }

        string filePath = Path.Combine(saveFolder, "labyrinthMap_" + timestamp + ".json");
        File.WriteAllText(filePath, json);
        Debug.Log("Labyrinth map exported to: " + filePath);
    }

    static bool TrySampleNavMesh(Vector3 world, out Vector3 snapped)
    {
        snapped = world;
        if (NavMesh.SamplePosition(world, out NavMeshHit hit, NavMeshSampleMaxDistance, NavMesh.AllAreas))
        {
            snapped = hit.position;
            return true;
        }

        return false;
    }

    static string BuildJson(Vector3[] vertices, int[] indices, Vector3 start, Vector3 end)
    {
        var sb = new StringBuilder();
        var inv = CultureInfo.InvariantCulture;
        sb.Append("{\n");
        sb.Append("  \"vertices\": [\n");
        for (int i = 0; i < vertices.Length; i++)
        {
            Vector3 v = vertices[i];
            sb.Append("    { \"x\": ");
            sb.Append(v.x.ToString("G9", inv));
            sb.Append(", \"z\": ");
            sb.Append(v.z.ToString("G9", inv));
            sb.Append(" }");
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
        sb.Append("  \"start_point\": { \"x\": ");
        sb.Append(start.x.ToString("G9", inv));
        sb.Append(", \"z\": ");
        sb.Append(start.z.ToString("G9", inv));
        sb.Append(" },\n");
        sb.Append("  \"end_point\": { \"x\": ");
        sb.Append(end.x.ToString("G9", inv));
        sb.Append(", \"z\": ");
        sb.Append(end.z.ToString("G9", inv));
        sb.Append(" }\n");
        sb.Append("}");
        return sb.ToString();
    }
}
