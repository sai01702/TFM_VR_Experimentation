using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

public class NavigationPathManager : MonoBehaviour
{
    public Transform startPoint;

    [Tooltip("Optional manual goal. Ignored at runtime when Assign Goal From MazeExit Tag is enabled.")]
    public Transform goalPoint;

    [Tooltip("If enabled, goalPoint is set in Awake from the first active-in-hierarchy object tagged MazeExit.")]
    public bool assignGoalFromMazeExitTag = true;

    [Tooltip("If set, only search under this transform (includes all nested children). If empty, search every loaded scene depth-first.")]
    public Transform limitSearchToSubtree;

    const string MazeExitTag = "MazeExit";

    private NavMeshPath path;

    void Awake()
    {
        path = new NavMeshPath();
    }

    void Start()
    {
        if (NavigationSessionController.SessionOwnsPathSetup)
            return;
        if (assignGoalFromMazeExitTag)
            TryAssignGoalFromMazeExit();
    }

    /// <summary>
    /// Sets <see cref="goalPoint"/> from active objects tagged <c>MazeExit</c>.
    /// If several exist (common in one maze), prefers an exit that has a complete NavMesh path from <see cref="startPoint"/> when start is set.
    /// </summary>
    public void TryAssignGoalFromMazeExit()
    {
        goalPoint = null;
        TryResolveMazeExitGoal(startPoint, limitSearchToSubtree, out Transform resolved);
        goalPoint = resolved;
    }

    /// <summary>
    /// Resolves a goal transform from active hierarchy objects tagged <c>MazeExit</c>, using the same rules as <see cref="TryAssignGoalFromMazeExit"/>.
    /// </summary>
    /// <param name="startPoint">Used when multiple exits exist to prefer one with a complete NavMesh path; may be null (first candidate wins).</param>
    /// <param name="limitSearchToSubtree">If set, only search under this transform; if null, search all loaded scenes.</param>
    /// <param name="goal">The chosen exit transform, or null if none found.</param>
    /// <returns>True if <paramref name="goal"/> was assigned.</returns>
    public static bool TryResolveMazeExitGoal(Transform startPoint, Transform limitSearchToSubtree, out Transform goal)
    {
        goal = null;

        var candidates = new List<Transform>(4);
        foreach (Transform t in EnumerateTransformsForSubtree(limitSearchToSubtree))
        {
            if (t == null)
                continue;
            if (!t.gameObject.activeInHierarchy)
                continue;
            if (!t.CompareTag(MazeExitTag))
                continue;
            candidates.Add(t);
        }

        string searchDesc = limitSearchToSubtree != null ? "'" + limitSearchToSubtree.name + "'" : "all loaded scenes";

        if (candidates.Count == 0)
        {
            Debug.LogWarning(
                $"NavigationPathManager: No active GameObject with tag '{MazeExitTag}' found under {searchDesc}. " +
                "Ensure the tag exists, the exit object is active, and the collider/transform you tagged is on that GameObject.");
            return false;
        }

        if (candidates.Count > 1)
        {
            Debug.Log(
                $"NavigationPathManager: {candidates.Count} '{MazeExitTag}' object(s) under search root — choosing one with a valid NavMesh path (or first as fallback).");

            if (startPoint != null)
            {
                foreach (var t in candidates)
                {
                    if (TryNavMeshPathComplete(startPoint.position, t.position))
                    {
                        goal = t;
                        Debug.Log($"NavigationPathManager: goalPoint assigned to '{t.name}' (path OK), path: {BuildHierarchyPath(t)}.");
                        return true;
                    }
                }

                Debug.LogWarning(
                    "NavigationPathManager: No MazeExit yielded a complete NavMesh path from StartPoint — using first exit. Re-bake NavMesh, move StartPoint onto the walkable mesh, or remove duplicate MazeExit tags.");
            }
        }

        goal = candidates[0];
        Debug.Log($"NavigationPathManager: goalPoint assigned to active '{goal.name}' ({MazeExitTag}), path: {BuildHierarchyPath(goal)}.");
        return true;
    }

    static bool TryNavMeshPathComplete(Vector3 fromWorld, Vector3 toWorld)
    {
        if (!TrySampleNavMesh(fromWorld, out Vector3 fromSnapped))
            return false;
        if (!TrySampleNavMesh(toWorld, out Vector3 toSnapped))
            return false;

        var testPath = new NavMeshPath();
        if (!NavMesh.CalculatePath(fromSnapped, toSnapped, NavMesh.AllAreas, testPath))
            return false;
        return testPath.status == NavMeshPathStatus.PathComplete;
    }

    static bool TrySampleNavMesh(Vector3 world, out Vector3 snapped)
    {
        snapped = world;
        const float maxDist = 4f;
        if (NavMesh.SamplePosition(world, out NavMeshHit hit, maxDist, NavMesh.AllAreas))
        {
            snapped = hit.position;
            return true;
        }

        return false;
    }

    IEnumerable<Transform> EnumerateTransformsForSearch()
    {
        foreach (Transform t in EnumerateTransformsForSubtree(limitSearchToSubtree))
            yield return t;
    }

    static IEnumerable<Transform> EnumerateTransformsForSubtree(Transform limitSearchToSubtree)
    {
        if (limitSearchToSubtree != null)
        {
            foreach (Transform t in limitSearchToSubtree.GetComponentsInChildren<Transform>(true))
                yield return t;
            yield break;
        }

        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene scene = SceneManager.GetSceneAt(i);
            if (!scene.IsValid() || !scene.isLoaded)
                continue;

            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root == null)
                    continue;
                foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
                    yield return t;
            }
        }
    }

    static string BuildHierarchyPath(Transform t)
    {
        if (t == null)
            return "";
        var stack = new Stack<string>();
        for (Transform x = t; x != null; x = x.parent)
            stack.Push(x.name);
        return string.Join("/", stack);
    }

    public Vector3[] GetPathCorners()
    {
        if (startPoint == null || goalPoint == null)
        {
            Debug.LogError("Start or Goal is NULL!");
            return null;
        }

        if (!TrySampleNavMesh(startPoint.position, out Vector3 startSnapped))
        {
            Debug.LogError(
                "NavMesh path calculation FAILED: StartPoint is not on (or near) the NavMesh. " +
                "Select the blue walkable area in the NavMesh gizmo, re-bake Maze1, or move StartPoint onto the floor.");
            return null;
        }

        if (!TrySampleNavMesh(goalPoint.position, out Vector3 goalSnapped))
        {
            Debug.LogError(
                "NavMesh path calculation FAILED: MazeExit / goal is not on (or near) the NavMesh. " +
                "Lower the exit transform to the floor, re-bake, or remove a duplicate MazeExit that sits in a wall.");
            return null;
        }

        bool success = NavMesh.CalculatePath(
            startSnapped,
            goalSnapped,
            NavMesh.AllAreas,
            path
        );

        if (!success || path.status != NavMeshPathStatus.PathComplete)
        {
            Debug.LogError(
                $"NavMesh path calculation FAILED (status: {path.status}). " +
                "Start and goal snap to the mesh, but there is no connected walk. Check NavMesh holes, agent radius vs corridors, and that only one maze surface is baked for this test.");
            return null;
        }

        // 🔴 DEBUG VISUALIZATION (Scene view only)
        foreach (var p in path.corners)
        {
            Debug.DrawLine(p, p + Vector3.up * 2, Color.red, 5f);
            Debug.Log(NavMesh.SamplePosition(startPoint.position, out NavMeshHit hit1, 1.0f, NavMesh.AllAreas));
            Debug.Log(NavMesh.SamplePosition(goalPoint.position, out NavMeshHit hit2, 1.0f, NavMesh.AllAreas));
        }

        Debug.Log("Path corners count: " + path.corners.Length);

        return path.corners;
    }
}