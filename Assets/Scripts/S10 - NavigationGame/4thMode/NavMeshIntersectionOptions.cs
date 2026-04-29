using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class NavMeshIntersectionOptions : MonoBehaviour
{
    public NavMeshIntersectionDetector detector;

    public float directionMergeAngle = 15f;

    public List<IntersectionWithOptions> results = new List<IntersectionWithOptions>();

    void Start()
    {
        GenerateOptions();
    }

    public void GenerateOptions()
    {
        results.Clear();

        NavMeshTriangulation triangulation = NavMesh.CalculateTriangulation();
        Vector3[] vertices = triangulation.vertices;
        int[] indices = triangulation.indices;

        // 🔹 triangle list
        List<Triangle> triangles = new List<Triangle>();

        for (int i = 0; i < indices.Length; i += 3)
        {
            Triangle t = new Triangle();
            t.a = vertices[indices[i]];
            t.b = vertices[indices[i + 1]];
            t.c = vertices[indices[i + 2]];
            t.center = (t.a + t.b + t.c) / 3f;

            triangles.Add(t);
        }

        foreach (var intersection in detector.intersections)
        {
            List<Vector3> dirs = new List<Vector3>();

            foreach (var tri in triangles)
            {
                // 🔥 intersection’a yakın triangle’ları bul
                if (Vector3.Distance(tri.center, intersection.position) < 2.5f)
                {
                    foreach (var other in triangles)
                    {
                        if (tri == other) continue;

                        if (ShareEdge(tri, other))
                        {
                            Vector3 dir = (other.center - tri.center).normalized;
                            dirs.Add(dir);
                        }
                    }
                }
            }

            // 🔹 duplicate temizle
            dirs = MergeDirections(dirs);

            IntersectionWithOptions res = new IntersectionWithOptions();
            res.position = intersection.position;
            res.options = dirs;

            results.Add(res);
        }

        Debug.Log("Options generated for intersections: " + results.Count);
    }

    // 🔹 EDGE CHECK
    bool ShareEdge(Triangle t1, Triangle t2)
    {
        int shared = 0;

        if (Approximately(t1.a, t2.a) || Approximately(t1.a, t2.b) || Approximately(t1.a, t2.c)) shared++;
        if (Approximately(t1.b, t2.a) || Approximately(t1.b, t2.b) || Approximately(t1.b, t2.c)) shared++;
        if (Approximately(t1.c, t2.a) || Approximately(t1.c, t2.b) || Approximately(t1.c, t2.c)) shared++;

        return shared >= 2;
    }

    bool Approximately(Vector3 a, Vector3 b)
    {
        return Vector3.Distance(a, b) < 0.01f;
    }

    // 🔹 DIRECTION MERGE
    List<Vector3> MergeDirections(List<Vector3> dirs)
    {
        List<Vector3> result = new List<Vector3>();

        foreach (var d in dirs)
        {
            bool merged = false;

            foreach (var r in result)
            {
                if (Vector3.Angle(d, r) < directionMergeAngle)
                {
                    merged = true;
                    break;
                }
            }

            if (!merged)
                result.Add(d);
        }

        return result;
    }

    void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;

        foreach (var r in results)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawSphere(r.position, 0.25f);

            foreach (var dir in r.options)
            {
                Gizmos.color = Color.magenta;
                Gizmos.DrawLine(r.position, r.position + dir * 2f);
            }
        }
    }
}

// =====================

[System.Serializable]
public class IntersectionWithOptions
{
    public Vector3 position;
    public List<Vector3> options;
}

public class NavTriangle
{
    public Vector3 a, b, c;
    public Vector3 center;
}