using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(LineRenderer))]
public class FloorPathRenderer : MonoBehaviour
{
    public NavigationPathManager pathManager;
    public int smoothness = 10;
    public float heightOffset = 1.05f;

    private LineRenderer line;

    void Start()
    {
        line = GetComponent<LineRenderer>();

        Vector3[] corners = pathManager.GetPathCorners();

        if (corners == null || corners.Length < 2)
            return;

        List<Vector3> smoothPath = SmoothPath(corners, smoothness);

        line.positionCount = smoothPath.Count;
        line.SetPositions(smoothPath.ToArray());
    }

    List<Vector3> SmoothPath(Vector3[] points, int subdivisions)
    {
        List<Vector3> result = new List<Vector3>();

        for (int i = 0; i < points.Length - 1; i++)
        {
            for (int j = 0; j < subdivisions; j++)
            {
                float t = j / (float)subdivisions;
                Vector3 p = Vector3.Lerp(points[i], points[i + 1], t);
                p.y = heightOffset; // lift slightly above ground
                result.Add(p);
            }
        }

        result.Add(points[points.Length - 1]);

        return result;
    }
}