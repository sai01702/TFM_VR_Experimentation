using UnityEngine;

namespace MazeGen
{
    [CreateAssetMenu(fileName = "Maze", menuName = "MazeGen/Maze")]
    public class Maze : ScriptableObject
    {
        public MazeGenerator mazeGenerator;
        public MazeSettings mazeSettings;
        private MazeContainer _mazeContainer;
        private MazeRegistry _mazeRegistry;

        /// <summary>
        /// Create a maze with the generator and settings given before
        /// </summary>
        public void Create()
        {
            if (mazeSettings != null)
            {
                mazeGenerator.SetSettings(mazeSettings);
                _mazeContainer = mazeGenerator.Generate();
                return;
            }
            
            Debug.LogWarning("Can't create a new maze, no settings");
        }

        public MazeRegistry GetMazeRegex()
        {
            return _mazeRegistry;
        }

        /// <summary>
        /// Get the maze container and create a warning if it doesn't exist
        /// </summary>
        public MazeContainer GetMaze()
        {
            if (_mazeContainer == null)
            {
                Debug.LogWarning("Maze isn't created can't return it's value");
            }

            return _mazeContainer;
        }
        
        public MazeContainer GetMazeOrNull()
        {
            return _mazeContainer;
        }
    }
}
