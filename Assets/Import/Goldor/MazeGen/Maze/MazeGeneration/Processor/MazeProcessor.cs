using UnityEngine;

namespace MazeGen
{
    public abstract class MazeProcessor : ScriptableObject
    {
        public string processorName;
        /// <summary>
        /// Virtual method to start the process.
        /// </summary>
        /// <param name="generator">Current generator starting the process.</param>
        /// <param name="maze">Maze container.</param>
        public abstract void Process(AgentMazeGenerator generator, MazeContainer maze);//introduce the script to change the maze
    }
}
    