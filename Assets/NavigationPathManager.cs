using UnityEngine;
using UnityEngine.AI;

public class NavigationPathManager : MonoBehaviour
{
    public Transform startPoint;
    public Transform goalPoint;

    private NavMeshPath path;

    void Awake()
    {
        path = new NavMeshPath();
    }

    public Vector3[] GetPathCorners()
    {
        if (startPoint == null || goalPoint == null)
        {
            Debug.LogError("Start or Goal is NULL!");
            return null;
        }

        bool success = NavMesh.CalculatePath(
            startPoint.position,
            goalPoint.position,
            NavMesh.AllAreas,
            path
        );

        if (!success)
        {
            Debug.LogError("NavMesh path calculation FAILED!");
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