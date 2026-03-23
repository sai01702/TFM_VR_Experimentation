using UnityEngine;

namespace MazeGen
{
    [CreateAssetMenu(fileName = "MazeSettings", menuName = "MazeGen/Maze Config/Settings")]
    public class MazeSettings : ScriptableObject
    {
        [Tooltip("The seed influencing the maze generation to create unique maze")] public int seed;
        [Tooltip("The size on the X axis of the maze")] public int sizeX;
        [Tooltip("The size on the Y axis of the maze")] public int sizeY;
        [Tooltip("The probability to create a new path each time a path is created")] public float splitProbability;
    }
}
