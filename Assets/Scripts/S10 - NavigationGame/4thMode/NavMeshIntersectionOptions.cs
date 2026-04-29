using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

/*
 * V4 pipeline role: RAW DECISION CANDIDATE PROVIDER.
 *
 * This script now owns the full raw-candidate step that used to be split across
 * two components.
 *
 * It performs both jobs in one NavMesh triangulation pass:
 * 1. Convert Unity's baked NavMesh into triangle data.
 * 2. Build triangle adjacency by checking shared edges.
 * 3. Find triangles with 3+ neighbors. These are rough intersection candidates.
 * 4. Merge nearby candidate positions to reduce duplicate node spam.
 * 5. For each merged candidate, collect outgoing directions from nearby connected
 *    triangles and merge directions that point almost the same way.
 *
 * Output:
 *   results = List<IntersectionWithOptions>
 *
 * NavigationDecisionSystem reads this list, filters it against the correct route,
 * marks the correct option, and creates the debug visuals/guide text.
 */
public class NavMeshIntersectionOptions : MonoBehaviour
{
    [Header("Candidate Detection")]
    public float intersectionMergeDistance = 1.0f;
    public int minimumNeighborCount = 3;

    [Header("Option Detection")]
    public float optionSearchRadius = 2.5f;
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

        List<NavMeshTriangleData> triangles = BuildTriangleList(vertices, indices);
        Dictionary<int, List<int>> adjacency = BuildAdjacency(triangles);
        List<Vector3> candidatePositions = FindIntersectionCandidates(triangles, adjacency);
        candidatePositions = MergeClosePositions(candidatePositions, intersectionMergeDistance);

        foreach (var candidate in candidatePositions)
        {
            List<Vector3> directions = FindOutgoingDirections(candidate, triangles, adjacency);
            directions = MergeDirections(directions);

            results.Add(new IntersectionWithOptions
            {
                position = candidate,
                options = directions
            });
        }

        Debug.Log("Intersection options generated: " + results.Count);
    }

    List<NavMeshTriangleData> BuildTriangleList(Vector3[] vertices, int[] indices)
    {
        List<NavMeshTriangleData> triangles = new List<NavMeshTriangleData>();

        for (int i = 0; i < indices.Length; i += 3)
        {
            NavMeshTriangleData triangle = new NavMeshTriangleData
            {
                a = vertices[indices[i]],
                b = vertices[indices[i + 1]],
                c = vertices[indices[i + 2]]
            };

            triangle.center = (triangle.a + triangle.b + triangle.c) / 3f;
            triangles.Add(triangle);
        }

        return triangles;
    }

    Dictionary<int, List<int>> BuildAdjacency(List<NavMeshTriangleData> triangles)
    {
        Dictionary<int, List<int>> adjacency = new Dictionary<int, List<int>>();

        for (int i = 0; i < triangles.Count; i++)
            adjacency[i] = new List<int>();

        for (int i = 0; i < triangles.Count; i++)
        {
            for (int j = i + 1; j < triangles.Count; j++)
            {
                if (!ShareEdge(triangles[i], triangles[j]))
                    continue;

                adjacency[i].Add(j);
                adjacency[j].Add(i);
            }
        }

        return adjacency;
    }

    List<Vector3> FindIntersectionCandidates(List<NavMeshTriangleData> triangles, Dictionary<int, List<int>> adjacency)
    {
        List<Vector3> candidates = new List<Vector3>();

        for (int i = 0; i < triangles.Count; i++)
        {
            if (adjacency[i].Count >= minimumNeighborCount)
                candidates.Add(triangles[i].center);
        }

        return candidates;
    }

    List<Vector3> FindOutgoingDirections(Vector3 candidate, List<NavMeshTriangleData> triangles, Dictionary<int, List<int>> adjacency)
    {
        List<Vector3> directions = new List<Vector3>();

        for (int i = 0; i < triangles.Count; i++)
        {
            if (Vector3.Distance(triangles[i].center, candidate) > optionSearchRadius)
                continue;

            foreach (int neighborIndex in adjacency[i])
            {
                Vector3 direction = triangles[neighborIndex].center - triangles[i].center;
                direction.y = 0f;

                if (direction.sqrMagnitude <= Mathf.Epsilon)
                    continue;

                directions.Add(direction.normalized);
            }
        }

        return directions;
    }

    List<Vector3> MergeClosePositions(List<Vector3> positions, float mergeDistance)
    {
        List<Vector3> merged = new List<Vector3>();

        foreach (var position in positions)
        {
            bool wasMerged = false;

            for (int i = 0; i < merged.Count; i++)
            {
                if (Vector3.Distance(position, merged[i]) > mergeDistance)
                    continue;

                merged[i] = (merged[i] + position) * 0.5f;
                wasMerged = true;
                break;
            }

            if (!wasMerged)
                merged.Add(position);
        }

        return merged;
    }

    List<Vector3> MergeDirections(List<Vector3> directions)
    {
        List<Vector3> merged = new List<Vector3>();

        foreach (var direction in directions)
        {
            bool alreadyExists = false;

            foreach (var existing in merged)
            {
                if (Vector3.Angle(direction, existing) < directionMergeAngle)
                {
                    alreadyExists = true;
                    break;
                }
            }

            if (!alreadyExists)
                merged.Add(direction);
        }

        return merged;
    }

    bool ShareEdge(NavMeshTriangleData first, NavMeshTriangleData second)
    {
        int shared = 0;

        if (Approximately(first.a, second.a) || Approximately(first.a, second.b) || Approximately(first.a, second.c)) shared++;
        if (Approximately(first.b, second.a) || Approximately(first.b, second.b) || Approximately(first.b, second.c)) shared++;
        if (Approximately(first.c, second.a) || Approximately(first.c, second.b) || Approximately(first.c, second.c)) shared++;

        return shared >= 2;
    }

    bool Approximately(Vector3 first, Vector3 second)
    {
        return Vector3.Distance(first, second) < 0.01f;
    }
}

[System.Serializable]
public class IntersectionWithOptions
{
    public Vector3 position;
    public List<Vector3> options;
}

public class NavMeshTriangleData
{
    public Vector3 a;
    public Vector3 b;
    public Vector3 c;
    public Vector3 center;
}
