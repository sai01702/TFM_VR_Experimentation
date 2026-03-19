using UnityEngine;
using MazeGen;

[CreateAssetMenu(fileName = "TreasureProcessor", menuName = "MazeGen/DemoScript/Maze Config/Generator/Processor/TreasureProcessor")]
public class TreasureProcessor : MazeProcessor
{
    [SerializeField,Range(0,1)] private double prob;
    [SerializeField] private GameObject treasurePrefab;
    
    public override void Process(AgentMazeGenerator generator, MazeContainer maze)
    {
        int treasureTypeId = generator.GetRegex().RegisterType((int)MazePart.PrimordialPart.PathEnd, "treasurePathEnd", treasurePrefab);
        
        maze.ForEachEmplacements(((x, y, part) =>
        {
            if ((MazePart.PrimordialPart)part.part == MazePart.PrimordialPart.PathEnd)
            {
                if (generator.GetNextDouble() <= prob)
                {
                    part.type = treasureTypeId;
                }
            }
            
            return part;
        }));
    }
}
