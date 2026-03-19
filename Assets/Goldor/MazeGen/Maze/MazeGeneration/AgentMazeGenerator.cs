using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using Random = System.Random;

namespace MazeGen
{
    [CreateAssetMenu(fileName = "AgentMazeGenerator", menuName = "MazeGen/Maze Config/Generator/Main Generator")]
    public class AgentMazeGenerator : MazeGenerator
    {
        [SerializeField] private MazeProcessor[] processors;
        
        protected List<PathCreator> AllPathCreators;
        protected List<PathCreator> ToDelete;
        protected List<PathCreator> ToAdd;
        protected Random random;

        protected DateTime StartTime;

        /// <summary>
        /// Get path creator currently in the maze.
        /// </summary>
        /// <returns>Return list of path creator</returns>
        public List<PathCreator> GetAllPathCreators()
        {
            return AllPathCreators;
        }

        private void InitVariables()
        {
            MazeContainer = new MazeContainer(MazeSettings.sizeX, MazeSettings.sizeY, (int) MazePart.PrimordialPart.Empty);
            
            MazeRegistry = new MazeRegistry();
            RegisterPrimordialPartIntoRegex();

            AllPathCreators = new List<PathCreator>();
            ToDelete = new List<PathCreator>();
            ToAdd = new List<PathCreator>();
            
            random = new Random(MazeSettings.seed);
        }

        private void RegisterPrimordialPartIntoRegex()
        {
            MazeRegistry.RegisterPrimordial((int)MazePart.PrimordialPart.Empty, "Empty", null);
            MazeRegistry.RegisterPrimordial((int)MazePart.PrimordialPart.Wall, "Wall", null);
            MazeRegistry.RegisterPrimordial((int)MazePart.PrimordialPart.MazeEnd, "MazeEnd", null);
            MazeRegistry.RegisterPrimordial((int)MazePart.PrimordialPart.PathEnd, "PathEnd", null);
            MazeRegistry.RegisterPrimordial((int)MazePart.PrimordialPart.StraightPath, "StraightPath", null);
            MazeRegistry.RegisterPrimordial((int)MazePart.PrimordialPart.CornerRightPath, "CornerRightPath", null);
            MazeRegistry.RegisterPrimordial((int)MazePart.PrimordialPart.CornerLeftPath, "CornerLeftPath", null);
            MazeRegistry.RegisterPrimordial((int)MazePart.PrimordialPart.TPath, "TPath", null);
            MazeRegistry.RegisterPrimordial((int)MazePart.PrimordialPart.XPath, "XPath", null);
        }

        /// <summary>
        /// Generate a maze with the given registry and settings (Async method !)
        /// </summary>
        /// <returns>return the maze container not generated yet (Wait for property "Finish" to be true)</returns>
        public override MazeContainer Generate()
        {
            Generating = true;
            InitVariables();
            AllPathCreators.Add(new PathCreator(this, MazeContainer, new Vector2Int(0,0), MazeSettings.splitProbability));
            
            StartTime = DateTime.Now;
            Debug.Log("Starting Maze generation at : "+StartTime);
            
            Thread thread = new Thread(() =>
            {
                while (AllPathCreators.Count > 0)
                {
                    GenerationStep();
                }
                
                Debug.Log("Basic generation ended in : "+DateTime.Now.Subtract(StartTime));

                FixUnsolvedPart();
                StartProcessors();
                
                Debug.Log("Maze generation finished in : "+DateTime.Now.Subtract(StartTime));
                
                Generating = false;
            });
            
            thread.Start();
            return MazeContainer;
        }

        private void StartProcessors()
        {
            if (processors == null) return;
            for (int i = 0; i < processors.Length; i++)
            {
                StartProcessor(processors[i]);
            }
        }

        private void FixUnsolvedPart()
        {
            PartComputer.Process(MazeContainer);
        }

        private void GenerationStep()
        {
            foreach (var pathCreator in AllPathCreators)
            {
                if (!pathCreator.Next()) //if the path creator is stuck
                {
                    ToDelete.Add(pathCreator);
                }
            }

            //Generate the end of the maze
            if (IsLastGenerationStep())
            {
                Vector2Int end = AllPathCreators[0].GetPos();
                MazeContainer.SetPos(end.x, end.y,
                    new MazePart(MazePart.PrimordialPart.MazeEnd, AllPathCreators[0].GetOrientation()));
            }

            foreach (var pathCreator in ToDelete)
            {
                AllPathCreators.Remove(pathCreator);
            }
            ToDelete.Clear();

            foreach (var pathCreator in ToAdd)
            {
                AllPathCreators.Add(pathCreator);
            }
            ToAdd.Clear();
        }

        private bool IsLastGenerationStep()
        {
            return ToDelete.Count == AllPathCreators.Count && ToAdd.Count == 0;
        }

        private void StartProcessor(MazeProcessor processor)
        {
            DateTime processorStartTime = DateTime.Now;
            Debug.Log($"Starting processor : {processor.processorName}");
            processor.Process(this, MazeContainer);
            Debug.Log($"Processor : {processor.processorName} ended in : {DateTime.Now.Subtract(processorStartTime)}");
        }
        
        /// <summary>
        /// Add a path creator to be used in the next generation step.
        /// </summary>
        /// <param name="pathCreator">Path creator to add</param>
        public void AddPathCreator(PathCreator pathCreator)
        {
            if(pathCreator == null) return;
            ToAdd.Add(pathCreator);
        }

        /// <summary>
        /// Get a number between 0 and 1 using the seed.
        /// </summary>
        /// <returns>Return number generated</returns>
        public double GetNextDouble()
        {
            return random.NextDouble();
        }

        /// <summary>
        /// Get a number between min and max using the seed.
        /// </summary>
        /// <param name="min">Minimum number to be generated</param>
        /// <param name="max">Maximum number to be generated</param>
        /// <returns>Return number generated</returns>
        public int GetNextInt(int min, int max)
        {
            return random.Next(min, max);
        }
    }   
}