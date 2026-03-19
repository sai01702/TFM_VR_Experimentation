using System.Collections.Generic;
using UnityEngine;
using MazeGen;

    [CreateAssetMenu(fileName = "ObstacleProcessor", menuName = "MazeGen/DemoScript/Maze Config/Generator/Processor/ObstacleProcessor")]
    public class ObstacleProcessor : MazeProcessor
    {
        public Obstacle[] obstacles;
        
        private List<Obstacle> straight;
        private List<Obstacle> cornerRight;
        private List<Obstacle> cornerLeft;
        private List<Obstacle> TPath;
        private List<Obstacle> XPath;
        
        public void SortAndRegister(MazeRegistry registry)
        {
            foreach (Obstacle obstacle in obstacles)
            {
                switch (obstacle.primordialShape)
                {
                    case MazePart.PrimordialPart.StraightPath:
                        straight.Add(obstacle);
                        obstacle.typeID = registry.RegisterType((int) MazePart.PrimordialPart.StraightPath, "obstacle", obstacle.gameObject);
                        break;
                    case MazePart.PrimordialPart.CornerRightPath:
                        cornerRight.Add(obstacle);
                        obstacle.typeID = registry.RegisterType((int) MazePart.PrimordialPart.CornerRightPath, "obstacle", obstacle.gameObject);
                        break;
                    case MazePart.PrimordialPart.CornerLeftPath:
                        cornerLeft.Add(obstacle);
                        obstacle.typeID = registry.RegisterType((int) MazePart.PrimordialPart.CornerLeftPath, "obstacle", obstacle.gameObject);
                        break;
                    case MazePart.PrimordialPart.TPath:
                        TPath.Add(obstacle);
                        obstacle.typeID = registry.RegisterType((int) MazePart.PrimordialPart.TPath, "obstacle", obstacle.gameObject);
                        break;
                    case MazePart.PrimordialPart.XPath:
                        XPath.Add(obstacle);
                        obstacle.typeID = registry.RegisterType((int) MazePart.PrimordialPart.XPath, "obstacle", obstacle.gameObject);
                        break;
                }
            }
        }
        
        public override void Process(AgentMazeGenerator generator, MazeContainer mazeContainer)
        {
            straight = new List<Obstacle>();
            cornerRight = new List<Obstacle>();
            cornerLeft = new List<Obstacle>();
            TPath = new List<Obstacle>();
            XPath = new List<Obstacle>();
            
            SortAndRegister(generator.GetRegex());
            
            mazeContainer.ForEachEmplacements((x, y, emplacement) =>
            {
                if (emplacement.Part != MazePart.PrimordialPart.Empty &&
                    emplacement.Part != MazePart.PrimordialPart.Wall)
                {
                    if (emplacement.Part == MazePart.PrimordialPart.StraightPath)
                    {
                        foreach (Obstacle obstacle in straight)
                        {
                            if (generator.GetNextDouble() <= obstacle.prob)
                            {
                                emplacement.type = obstacle.typeID;
                                return emplacement;
                            }
                        }
                    }
                    if (emplacement.Part == MazePart.PrimordialPart.CornerRightPath)
                    {
                        foreach (Obstacle obstacle in cornerRight)
                        {
                            if (generator.GetNextDouble() <= obstacle.prob)
                            {
                                emplacement.type = obstacle.typeID;
                                return emplacement;
                            }
                        }
                    }
                    if (emplacement.Part == MazePart.PrimordialPart.CornerLeftPath)
                    {
                        foreach (Obstacle obstacle in cornerLeft)
                        {
                            if (generator.GetNextDouble() <= obstacle.prob)
                            {
                                emplacement.type = obstacle.typeID;
                                return emplacement;
                            }
                        }
                    }
                    if (emplacement.Part == MazePart.PrimordialPart.TPath)
                    {
                        foreach (Obstacle obstacle in TPath)
                        {
                            if (generator.GetNextDouble() <= obstacle.prob)
                            {
                                emplacement.type = obstacle.typeID;
                                return emplacement;
                            }
                        }
                    }if (emplacement.Part == MazePart.PrimordialPart.XPath)
                    {
                        foreach (Obstacle obstacle in XPath)
                        {
                            if (generator.GetNextDouble() <= obstacle.prob)
                            {
                                emplacement.type = obstacle.typeID;
                                return emplacement;
                            }
                        }
                    }
                    
                }

                return emplacement;
            });
        }
        
    }