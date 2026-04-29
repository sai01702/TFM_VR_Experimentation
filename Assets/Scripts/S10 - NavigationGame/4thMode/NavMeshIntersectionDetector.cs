using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class NavMeshIntersectionDetector : MonoBehaviour
{
    public float mergeDistance = 1.0f;

    public List<IntersectionPoint> intersections = new List<IntersectionPoint>();

    void Start()
    {
        DetectIntersections();
    }

    public void DetectIntersections()
    {
        intersections.Clear();

        NavMeshTriangulation triangulation = NavMesh.CalculateTriangulation();

        Vector3[] vertices = triangulation.vertices;
        int[] indices = triangulation.indices;

        // 🔹 Triangle list
        List<Triangle> triangles = new List<Triangle>();

        for (int i = 0; i < indices.Length; i += 3)
        {
            Triangle tri = new Triangle();
            tri.a = vertices[indices[i]];
            tri.b = vertices[indices[i + 1]];
            tri.c = vertices[indices[i + 2]];
            tri.center = (tri.a + tri.b + tri.c) / 3f;

            triangles.Add(tri);
        }

        // 🔹 adjacency hesapla
        Dictionary<int, List<int>> adjacency = new Dictionary<int, List<int>>();

        for (int i = 0; i < triangles.Count; i++)
        {
            adjacency[i] = new List<int>();

            for (int j = 0; j < triangles.Count; j++)
            {
                if (i == j) continue;

                if (ShareEdge(triangles[i], triangles[j]))
                {
                    adjacency[i].Add(j);
                }
            }
        }

        // 🔥 intersection bul
        for (int i = 0; i < triangles.Count; i++)
        {
            int neighborCount = adjacency[i].Count;

            if (neighborCount >= 3)
            {
                IntersectionPoint ip = new IntersectionPoint();
                ip.position = triangles[i].center;
                ip.neighborCount = neighborCount;

                intersections.Add(ip);
            }
        }

        // 🔹 yakın intersection’ları merge et
        intersections = MergeClosePoints(intersections);

        Debug.Log("Intersections Found: " + intersections.Count);
    }

    // 🔹 iki triangle edge paylaşıyor mu?
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

    // 🔹 merge
    List<IntersectionPoint> MergeClosePoints(List<IntersectionPoint> points)
    {
        List<IntersectionPoint> result = new List<IntersectionPoint>();

        foreach (var p in points)
        {
            bool merged = false;

            foreach (var r in result)
            {
                if (Vector3.Distance(p.position, r.position) < mergeDistance)
                {
                    r.position = (r.position + p.position) / 2f;
                    merged = true;
                    break;
                }
            }

            if (!merged)
                result.Add(p);
        }

        return result;
    }

}

// =======================

[System.Serializable]
public class IntersectionPoint
{
    public Vector3 position;
    public int neighborCount;
}

public class Triangle
{
    public Vector3 a, b, c;
    public Vector3 center;
}
