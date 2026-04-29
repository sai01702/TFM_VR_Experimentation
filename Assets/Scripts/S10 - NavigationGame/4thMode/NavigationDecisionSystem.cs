using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class NavigationDecisionSystem : MonoBehaviour
{
    public Transform startPoint;
    public Transform goalPoint;

    public NavMeshIntersectionOptions optionsSystem;

    public float pathThreshold = 1.5f;

    private List<DecisionNode> decisionNodes = new List<DecisionNode>();

    IEnumerator Start()
    {
        // diğer scriptlerin çalışmasını bekle
        yield return null;
        yield return null;

        GenerateDecisions();
    }

    void GenerateDecisions()
    {
        Debug.Log("OptionsSystem results count: " + optionsSystem.results.Count);
        decisionNodes.Clear();

        // 1️⃣ PATH AL
        NavMeshPath path = new NavMeshPath();
        NavMesh.CalculatePath(startPoint.position, goalPoint.position, NavMesh.AllAreas, path);

        Vector3[] corners = path.corners;

        if (corners.Length < 2)
        {
            Debug.LogError("Path too short!");
            return;
        }

        // 2️⃣ INTERSECTION'LARI AL
        var allIntersections = optionsSystem.results;
        Debug.Log("All intersections: " + allIntersections.Count);
        Debug.Log("Path corners: " + corners.Length);

        // 3️⃣ PATH ÜZERİNDEKİLERİ FİLTRELE
        List<IntersectionWithOptions> pathIntersections = new List<IntersectionWithOptions>();

        foreach (var i in allIntersections)
        {
            if (IsOnPath(i.position, corners, pathThreshold))
            {
                pathIntersections.Add(i);
            }
            
        }
        Debug.Log("Filtered intersections: " + pathIntersections.Count);

        // 4️⃣ PATH BOYUNCA SIRALA
        pathIntersections.Sort((a, b) =>
        {
            float da = DistanceAlongPath(a.position, corners);
            float db = DistanceAlongPath(b.position, corners);
            return da.CompareTo(db);
        });

        // 5️⃣ HER BİRİ İÇİN DOĞRU YÖN
        for (int i = 0; i < pathIntersections.Count; i++)
        {
            var inter = pathIntersections[i];

            Vector3 nextCorner = GetNextCorner(inter.position, corners);

            Vector3 correct = GetCorrectOption(inter.position, inter.options, nextCorner);

            DecisionNode node = new DecisionNode();
            node.position = inter.position;
            node.options = inter.options;
            node.correctOption = correct;

            decisionNodes.Add(node);
        }

        Debug.Log("Decision Nodes: " + decisionNodes.Count);
        
    }

    // =========================
    // HELPERS
    // =========================

    bool IsOnPath(Vector3 point, Vector3[] corners, float threshold)
    {
        for (int i = 0; i < corners.Length - 1; i++)
        {
            Vector3 a = corners[i];
            Vector3 b = corners[i + 1];

            Vector3 closest = ClosestPointOnSegment(point, a, b);
            float dist = Vector3.Distance(point, closest);

            if (dist < threshold)
                return true;
            
        }
        return false;
    }
    
    Vector3 ClosestPointOnSegment(Vector3 p, Vector3 a, Vector3 b)
    {
        Vector3 ap = p - a;
        Vector3 ab = b - a;

        float t = Vector3.Dot(ap, ab) / ab.sqrMagnitude;
        t = Mathf.Clamp01(t);

        return a + ab * t;
    }

    float DistancePointToSegment(Vector3 p, Vector3 a, Vector3 b)
    {
        Vector3 ap = p - a;
        Vector3 ab = (b - a);

        float t = Vector3.Dot(ap, ab) / ab.sqrMagnitude;
        t = Mathf.Clamp01(t);

        Vector3 closest = a + ab * t;
        return Vector3.Distance(p, closest);
    }

    float DistanceAlongPath(Vector3 point, Vector3[] corners)
    {
        float total = 0f;

        for (int i = 0; i < corners.Length - 1; i++)
        {
            Vector3 a = corners[i];
            Vector3 b = corners[i + 1];

            float segLength = Vector3.Distance(a, b);

            float dist = DistancePointToSegment(point, a, b);

            if (dist < pathThreshold)
            {
                total += Vector3.Distance(a, point);
                break;
            }
            else
            {
                total += segLength;
            }
        }

        return total;
    }

    Vector3 GetNextCorner(Vector3 point, Vector3[] corners)
    {
        for (int i = 0; i < corners.Length - 1; i++)
        {
            if (DistancePointToSegment(point, corners[i], corners[i + 1]) < pathThreshold)
            {
                return corners[i + 1];
            }
        }

        return corners[corners.Length - 1];
    }

    Vector3 GetCorrectOption(Vector3 intersection, List<Vector3> options, Vector3 nextCorner)
    {
        Vector3 toNext = (nextCorner - intersection).normalized;

        float bestDot = -1f;
        Vector3 best = Vector3.zero;

        foreach (var option in options)
        {
            float dot = Vector3.Dot(option, toNext);

            if (dot > bestDot)
            {
                bestDot = dot;
                best = option;
            }
        }

        return best;
    }

    void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;

        Gizmos.color = Color.green;

        foreach (var d in decisionNodes)
        {
            Gizmos.DrawSphere(d.position, 0.3f);

            Gizmos.color = Color.red;
            Gizmos.DrawLine(d.position, d.position + d.correctOption * 2f);
        }
    }
    
}

// =========================

[System.Serializable]
public class DecisionNode
{
    public Vector3 position;
    public List<Vector3> options;
    public Vector3 correctOption;
}