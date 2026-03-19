using System.IO;
using UnityEngine;
using UnityEngine.AI;

public class MazePDFExporter : MonoBehaviour
{
    public int textureSize = 1024;

    [Header("Save Settings")]
    public string saveDirectory = "C:/Users/YourName/Desktop"; // 🔥 YOU CHANGE THIS

    public string fileName = "MazeOutput.png";

    // 🔥 MAIN FUNCTION
    public void GenerateMazeImage(Vector3 start, Vector3 end)
    {
        UnityEngine.Debug.Log("🔥 GenerateMazeImage CALLED");

        if (!Directory.Exists(saveDirectory))
        {
            UnityEngine.Debug.LogError("❌ Directory does not exist: " + saveDirectory);
            return;
        }

        NavMeshPath path = new NavMeshPath();

        if (!NavMesh.CalculatePath(start, end, NavMesh.AllAreas, path))
        {
            UnityEngine.Debug.LogError("❌ No path found!");
            return;
        }

        Texture2D texture = new Texture2D(textureSize, textureSize);

        // White background
        Color[] bg = new Color[textureSize * textureSize];
        for (int i = 0; i < bg.Length; i++)
            bg[i] = Color.white;

        texture.SetPixels(bg);

        // Draw RED path
        for (int i = 1; i < path.corners.Length; i++)
        {
            DrawLine(texture,
                WorldToTex(path.corners[i - 1]),
                WorldToTex(path.corners[i]),
                Color.red);
        }

        texture.Apply();

        // 🔥 BUILD FULL PATH
        string fullPath = Path.Combine(saveDirectory, fileName);

        byte[] png = texture.EncodeToPNG();
        File.WriteAllBytes(fullPath, png);

        UnityEngine.Debug.Log("✅ PNG SAVED AT: " + fullPath);
    }

    // 🔥 TEST BUTTON
    [ContextMenu("Generate Maze Image (TEST)")]
    public void TestGenerate()
    {
        if (MazeManager.Instance == null)
        {
            UnityEngine.Debug.LogError("MazeManager not found!");
            return;
        }

        Transform start = MazeManager.Instance.playerRig;
        Transform end = MazeManager.Instance.GetExit();

        if (start == null || end == null)
        {
            UnityEngine.Debug.LogError("Start or Exit missing!");
            return;
        }

        GenerateMazeImage(start.position, end.position);
    }

    // 🔥 Convert world → texture
    Vector2Int WorldToTex(Vector3 world)
    {
        float scale = 10f;

        int x = Mathf.Clamp((int)(world.x * scale + textureSize / 2), 0, textureSize - 1);
        int y = Mathf.Clamp((int)(world.z * scale + textureSize / 2), 0, textureSize - 1);

        return new Vector2Int(x, y);
    }

    // 🔥 Draw line
    void DrawLine(Texture2D tex, Vector2Int a, Vector2Int b, Color col)
    {
        int x0 = a.x;
        int y0 = a.y;
        int x1 = b.x;
        int y1 = b.y;

        int dx = Mathf.Abs(x1 - x0);
        int dy = Mathf.Abs(y1 - y0);

        int sx = x0 < x1 ? 1 : -1;
        int sy = y0 < y1 ? 1 : -1;

        int err = dx - dy;

        while (true)
        {
            tex.SetPixel(x0, y0, col);

            if (x0 == x1 && y0 == y1) break;

            int e2 = 2 * err;

            if (e2 > -dy)
            {
                err -= dy;
                x0 += sx;
            }

            if (e2 < dx)
            {
                err += dx;
                y0 += sy;
            }
        }
    }
}