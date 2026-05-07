using System.Collections;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.AI;

// This class represents ONE point in a JSON-friendly format (SIMPLIFIED for analytics system)
[System.Serializable]
public class PathPoint
{
    public float x;
    public float z;

    public PathPoint(Vector3 v)
    {
        x = v.x;
        z = v.z;
    }
}

/// <summary>
/// Exports the NavMesh shortest path from start to goal as JSON. Goal defaults to an active object tagged MazeExit
/// (<see cref="NavigationPathManager.TryResolveMazeExitGoal"/>), matching <see cref="NavigationPathManager"/> snapping.
/// </summary>
public class PathExporter : MonoBehaviour
{
    const float NavMeshSampleMaxDistance = 4f;

    [Header("References")]
    public Transform startPoint;

    [Tooltip("Optional manual goal when pathManager is null and MazeExit resolution is unavailable. Ignored when pathManager provides goalPoint.")]
    public Transform goalPoint;

    [Tooltip("When pathManager is null: optional root to limit MazeExit search (same as NavigationPathManager.limitSearchToSubtree).")]
    public Transform limitSearchToSubtree;

    [Tooltip("If set, waits until goalPoint is assigned (session resolves MazeExit asynchronously), then exports.")]
    public NavigationPathManager pathManager;

    [Header("Save Settings")]
    public string saveFolder = "Assets/Log/4-NavigationSceneLogs/";

    [Tooltip("Max seconds to wait for pathManager.goalPoint before retrying TryAssignGoalFromMazeExit / giving up.")]
    public float waitForGoalTimeoutSeconds = 60f;

    NavMeshPath path;

    void Start()
    {
        path = new NavMeshPath();

        if (pathManager != null)
            StartCoroutine(ExportWhenPathReady());
        else
            TryComputeAndExport();
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
                    "PathExporter: pathManager.goalPoint was not set within " + waitForGoalTimeoutSeconds +
                    " s — export skipped. Ensure an active MazeExit, assign optional goalPoint fallback, or increase waitForGoalTimeoutSeconds.");
                yield break;
            }
        }

        yield return null;
        yield return null;
        TryComputeAndExport();
    }

    [ContextMenu("Export Now")]
    public void ExportNow()
    {
        if (path == null)
            path = new NavMeshPath();
        TryComputeAndExport();
    }

    void TryComputeAndExport()
    {
        Debug.Log("Starting path export...");

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
                "PathExporter: start and goal required — set startPoint, assign pathManager and wait for MazeExit, set optional goalPoint, or ensure an active MazeExit.");
            return;
        }

        if (!TrySampleNavMesh(startT.position, out Vector3 startSnapped))
        {
            Debug.LogError(
                "PathExporter: Start is not on (or near) the NavMesh. Move start onto the walkable floor or re-bake NavMesh.");
            return;
        }

        if (!TrySampleNavMesh(goalT.position, out Vector3 goalSnapped))
        {
            Debug.LogError(
                "PathExporter: Goal is not on (or near) the NavMesh. Lower the exit / goal onto the floor or re-bake NavMesh.");
            return;
        }

        bool calculated = NavMesh.CalculatePath(startSnapped, goalSnapped, NavMesh.AllAreas, path);
        if (!calculated || path.status != NavMeshPathStatus.PathComplete)
        {
            Debug.LogError(
                "PathExporter: NavMesh path calculation failed (status: " + path.status +
                "). Start and goal should lie on connected walkable mesh.");
            return;
        }

        Debug.Log("Path corners count: " + path.corners.Length);
        ExportPathToJson();
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

    void ExportPathToJson()
    {
        if (path == null || path.corners == null || path.corners.Length == 0)
        {
            Debug.LogError("PathExporter: No path data to export.");
            return;
        }

        string timestamp = System.DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");

        var json = new StringBuilder();
        json.Append("[\n");

        for (int i = 0; i < path.corners.Length; i++)
        {
            Vector3 p = path.corners[i];
            var point = new PathPoint(p);
            json.Append($"  {{ \"x\": {point.x:F3}, \"z\": {point.z:F3} }}");
            if (i < path.corners.Length - 1)
                json.Append(",");
            json.Append("\n");
            Debug.Log("Corner " + i + ": " + p);
        }

        json.Append("]");

        if (!Directory.Exists(saveFolder))
        {
            Directory.CreateDirectory(saveFolder);
            Debug.Log("Created folder: " + saveFolder);
        }

        string filePath = Path.Combine(saveFolder, "idealPath_" + timestamp + ".json");
        File.WriteAllText(filePath, json.ToString());
        Debug.Log("Path exported to: " + filePath);
    }
}
