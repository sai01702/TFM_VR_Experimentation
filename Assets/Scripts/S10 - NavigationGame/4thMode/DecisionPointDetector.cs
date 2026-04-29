using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class DecisionPointDetector : MonoBehaviour
{
    public Transform startPoint;
    public Transform goalPoint;

    [Header("Sampling")]
    public float pathSampleStep = 1.2f;
    public float sampleDistance = 1.5f;
    public float sampleRadius = 1.2f;

    [Header("Detection")]
    public float minDecisionDistance = 2f;
    public float duplicateAngleThreshold = 20f;

    public List<DecisionPoint> decisionPoints = new List<DecisionPoint>();

    private List<Vector3> pathSamples = new List<Vector3>();

    void Start()
    {
        GenerateDecisionPoints();
    }

    // ================================
    // MAIN
    // ================================
    public void GenerateDecisionPoints()
    {
        decisionPoints.Clear();

        NavMeshPath path = new NavMeshPath();

        if (!NavMesh.CalculatePath(startPoint.position, goalPoint.position, NavMesh.AllAreas, path))
        {
            Debug.LogError("Path calculation failed!");
            return;
        }

        if (path.corners.Length < 2)
        {
            Debug.LogWarning("Path too short.");
            return;
        }

        pathSamples = GetPathSamples(path.corners);

        Vector3 lastDecision = Vector3.positiveInfinity;

        for (int i = 0; i < pathSamples.Count - 1; i++)
        {
            Vector3 current = pathSamples[i];
            Vector3 forward = (pathSamples[i + 1] - current).normalized;

            // 🔥 PATH'TEN SAĞ/SOL OFFSET
            Vector3 right = Vector3.Cross(Vector3.up, forward);
            Vector3 left = -right;

            List<Vector3> offsets = new List<Vector3>()
            {
                Vector3.zero,
                right * 0.8f,
                left * 0.8f
            };

            foreach (var offset in offsets)
            {
                Vector3 samplePoint = current + offset;

                List<Vector3> options = SampleDirections(samplePoint);

                options = RemoveDuplicateDirections(options);

                if (options.Count >= 3)
                {
                    if (Vector3.Distance(lastDecision, samplePoint) < minDecisionDistance)
                        continue;

                    DecisionPoint dp = new DecisionPoint();
                    dp.position = samplePoint;
                    dp.options = options;
                    dp.correctOption = GetCorrectDirection(forward, options);

                    decisionPoints.Add(dp);
                    lastDecision = samplePoint;

                    break; // aynı noktayı 2 kere ekleme
                }
            }
        }

        Debug.Log("Decision Points Found: " + decisionPoints.Count);
    }

    // ================================
    // PATH SAMPLING
    // ================================
    List<Vector3> GetPathSamples(Vector3[] corners)
    {
        List<Vector3> samples = new List<Vector3>();

        for (int i = 0; i < corners.Length - 1; i++)
        {
            Vector3 start = corners[i];
            Vector3 end = corners[i + 1];

            float dist = Vector3.Distance(start, end);
            int count = Mathf.CeilToInt(dist / pathSampleStep);

            for (int j = 0; j <= count; j++)
            {
                Vector3 point = Vector3.Lerp(start, end, j / (float)count);
                samples.Add(point);
            }
        }

        return samples;
    }

    // ================================
    // DIRECTION SAMPLING (IMPROVED)
    // ================================
    List<Vector3> SampleDirections(Vector3 current)
    {
        List<Vector3> validDirs = new List<Vector3>();

        Vector3[] baseDirs =
        {
            Vector3.forward,
            Vector3.back,
            Vector3.left,
            Vector3.right
        };

        foreach (var dir in baseDirs)
        {
            int successCount = 0;

            // 🔥 MULTI STEP TEST
            for (int i = 1; i <= 3; i++)
            {
                Vector3 testPoint = current + dir * (sampleDistance * i * 0.6f);

                if (IsWalkable(current, testPoint))
                {
                    successCount++;
                }
            }

            if (successCount >= 2)
            {
                validDirs.Add(dir.normalized);
            }
        }

        return validDirs;
    }

    // ================================
    // WALKABLE CHECK
    // ================================
    bool IsWalkable(Vector3 from, Vector3 to)
    {
        NavMeshHit hit;

        if (!NavMesh.SamplePosition(to, out hit, sampleRadius, NavMesh.AllAreas))
            return false;

        if (NavMesh.Raycast(from, to, out hit, NavMesh.AllAreas))
            return false;

        return true;
    }

    // ================================
    // REMOVE DUPLICATES
    // ================================
    List<Vector3> RemoveDuplicateDirections(List<Vector3> dirs)
    {
        List<Vector3> result = new List<Vector3>();

        foreach (var dir in dirs)
        {
            bool duplicate = false;

            foreach (var existing in result)
            {
                if (Vector3.Angle(dir, existing) < duplicateAngleThreshold)
                {
                    duplicate = true;
                    break;
                }
            }

            if (!duplicate)
                result.Add(dir);
        }

        return result;
    }

    // ================================
    // CORRECT PATH DIRECTION
    // ================================
    Vector3 GetCorrectDirection(Vector3 forward, List<Vector3> options)
    {
        float bestDot = -1f;
        Vector3 best = Vector3.zero;

        foreach (var dir in options)
        {
            float dot = Vector3.Dot(forward, dir);

            if (dot > bestDot)
            {
                bestDot = dot;
                best = dir;
            }
        }

        return best;
    }

}

// ================================
// DATA CLASS
// ================================
[System.Serializable]
public class DecisionPoint
{
    public Vector3 position;
    public List<Vector3> options;
    public Vector3 correctOption;
}
