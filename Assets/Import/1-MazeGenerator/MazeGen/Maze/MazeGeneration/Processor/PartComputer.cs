/*
 * Repair unhandled generation problems
 */
using UnityEngine;

namespace MazeGen
{
    public class PartComputer
    {
        /// <summary>
        /// Start process to adapt a maze part to the surrounding ones
        /// </summary>
        /// <param name="mazeContainer">Maze container to use</param>
        public static void Process(MazeContainer mazeContainer)
        {
            mazeContainer.ForEachEmplacements((x, y, emplacement) =>
            {
                if (emplacement.Part == MazePart.PrimordialPart.UnknownSplitPath || emplacement.Part == MazePart.PrimordialPart.ToCompute)
                {
                    return GetAppropriatedMazePart(mazeContainer, x, y);
                }
            
                return emplacement;
            });
        }

        private static MazePart GetAppropriatedMazePart(MazeContainer mazeContainer, int x, int y )
        {
            Vector2Int pos = new Vector2Int(x, y);
        
            bool connectXPlus = false;
            bool connectXMinus = false;
            bool connectYPlus = false;
            bool connectYMinus = false;
            int connectionCount = 0;

            if (IsPartConnected(mazeContainer.GetXPlus(pos), (int)PathCreator.ORIENTATION.xPlus))
            {
                connectXPlus = true;
                connectionCount++;
            }
            if (IsPartConnected(mazeContainer.GetXMinus(pos), (int)PathCreator.ORIENTATION.xMinus))
            {
                connectXMinus = true;
                connectionCount++;
            }
            if (IsPartConnected(mazeContainer.GetYPlus(pos), (int)PathCreator.ORIENTATION.yPlus))
            {
                connectYPlus = true; 
                connectionCount++;
            }
            if (IsPartConnected(mazeContainer.GetYMinus(pos), (int)PathCreator.ORIENTATION.yMinus))
            {
                connectYMinus = true;
                connectionCount++;
            }

            switch (connectionCount)
            {
                case 4 :
                    return new MazePart(MazePart.PrimordialPart.XPath);
                case 3 :
                    if(connectXPlus && connectXMinus && connectYPlus)
                        return new MazePart(MazePart.PrimordialPart.TPath, 3);
                    if(connectXMinus && connectYPlus && connectYMinus)
                        return new MazePart(MazePart.PrimordialPart.TPath, 2);
                    if(connectYPlus && connectYMinus && connectXPlus)
                        return new MazePart(MazePart.PrimordialPart.TPath, 0);
                    if(connectYMinus && connectXPlus && connectXMinus)
                        return new MazePart(MazePart.PrimordialPart.TPath, 1);
                    break;
                case 2 :
                    int connectedInPaths = 0;

                    if (IsPartConnectedIn(mazeContainer.GetXPlus(pos), (int) PathCreator.ORIENTATION.xPlus))
                    {
                        if (connectXMinus)
                            return new MazePart(MazePart.PrimordialPart.StraightPath, 2);
                        if (connectYPlus)
                            return new MazePart(MazePart.PrimordialPart.CornerRightPath, 3);
                        if (connectYMinus)
                            return new MazePart(MazePart.PrimordialPart.CornerLeftPath, 1);
                        connectedInPaths++;
                    }
                    if (IsPartConnectedIn(mazeContainer.GetXMinus(pos), (int) PathCreator.ORIENTATION.xMinus))
                    {
                        if (connectXPlus)
                            return new MazePart(MazePart.PrimordialPart.StraightPath, 0);
                        if (connectYPlus)
                            return new MazePart(MazePart.PrimordialPart.CornerLeftPath, 3);
                        if (connectYMinus)
                            return new MazePart(MazePart.PrimordialPart.CornerRightPath, 1);
                    
                        connectedInPaths++;
                    }

                    if (IsPartConnectedIn(mazeContainer.GetYPlus(pos), (int) PathCreator.ORIENTATION.yPlus))
                    {
                        if (connectXPlus)
                            return new MazePart(MazePart.PrimordialPart.CornerRightPath, 2);
                        if (connectXMinus)
                            return new MazePart(MazePart.PrimordialPart.CornerLeftPath, 0);
                        if (connectYMinus)
                            return new MazePart(MazePart.PrimordialPart.StraightPath, 1);
                    
                        connectedInPaths++;
                    }

                    if (IsPartConnectedIn(mazeContainer.GetYMinus(pos), (int) PathCreator.ORIENTATION.yMinus))
                    {
                        if (connectXPlus)
                            return new MazePart(MazePart.PrimordialPart.CornerRightPath, 0);
                        if (connectXMinus)
                            return new MazePart(MazePart.PrimordialPart.CornerLeftPath, 2);
                        if (connectYPlus)
                            return new MazePart(MazePart.PrimordialPart.StraightPath, 3);
                    
                        connectedInPaths++;
                    }

                    if (connectedInPaths > 1)
                    {
                        return new MazePart(MazePart.PrimordialPart.OutOfControl);
                    } 
                    break;
                case 1 :
                    if(connectXPlus)
                        return new MazePart(MazePart.PrimordialPart.PathEnd, 2);
                    if(connectYMinus)
                        return new MazePart(MazePart.PrimordialPart.PathEnd, 1);
                    if(connectXMinus)
                        return new MazePart(MazePart.PrimordialPart.PathEnd, 0);
                    if(connectYPlus)
                        return new MazePart(MazePart.PrimordialPart.PathEnd, 3);
                    break;
                case 0 :
                    return null;
            }
            return new MazePart(MazePart.PrimordialPart.OutOfControl);
        }

        private static bool IsPartConnected(MazePart emplacement, int orientationToPart)//can be null
        {
            if (emplacement != null)
            {
                var part = emplacement.Part;
                var orientation = emplacement.direction;
                if (part == MazePart.PrimordialPart.StraightPath && 
                    (orientation == orientationToPart || PathCreator.TurnOrientationBack(orientation) == orientationToPart))
                {
                    return true;
                }
                if(part == MazePart.PrimordialPart.CornerLeftPath &&
                   (PathCreator.TurnOrientationBack(orientation) == orientationToPart || PathCreator.TurnOrientationRight(orientation) == orientationToPart))
                {
                    return true;
                }
                if(part == MazePart.PrimordialPart.CornerRightPath &&
                   (PathCreator.TurnOrientationBack(orientation) == orientationToPart || PathCreator.TurnOrientationLeft(orientation) == orientationToPart))
                {
                    return true;
                }
                if(part == MazePart.PrimordialPart.TPath &&
                   (orientation != orientationToPart))
                {
                    return true;
                }
                if(part == MazePart.PrimordialPart.XPath || part == MazePart.PrimordialPart.UnknownSplitPath || part == MazePart.PrimordialPart.ToCompute)
                {
                    return true;
                }
                if ((part == MazePart.PrimordialPart.PathEnd || part == MazePart.PrimordialPart.MazeEnd) && orientation == orientationToPart)
                {
                    return true;
                }
                //Debug.Log("relativeOrientation : "+orientationToPart+", orientation : "+orientation+", part : "+part);
            }
        
            return false;
        }

        private static bool IsPartConnectedIn(MazePart emplacement, int orientationToPart)//can be null
        {
            if (emplacement != null)
            {
                var part = emplacement.Part;
                var orientation = emplacement.direction;
                if (part == MazePart.PrimordialPart.StraightPath || part == MazePart.PrimordialPart.CornerLeftPath || part == MazePart.PrimordialPart.CornerRightPath &&
                    (PathCreator.TurnOrientationBack(orientation) == orientationToPart))
                {
                    return true;
                }
            
                //these are complicate
                if(part == MazePart.PrimordialPart.TPath &&
                   (orientation != orientationToPart))
                {
                    return true;
                }
                if(part == MazePart.PrimordialPart.XPath || part == MazePart.PrimordialPart.UnknownSplitPath || part == MazePart.PrimordialPart.ToCompute)
                {
                    return true;
                }
            }
        
            return false;
        }
        public static bool IsPartConnectedOut(MazePart emplacement, int orientationToPart)//can be null
        {
            if (emplacement != null)
            {
                var part = emplacement.Part;
                var orientation = emplacement.direction;
                if (part == MazePart.PrimordialPart.StraightPath || part == MazePart.PrimordialPart.PathEnd || part == MazePart.PrimordialPart.MazeEnd &&
                    orientation == orientationToPart)
                {
                    return true;
                }
                if(part == MazePart.PrimordialPart.CornerLeftPath &&
                   PathCreator.TurnOrientationRight(orientation) == orientationToPart)
                {
                    return true;
                }
                if(part == MazePart.PrimordialPart.CornerRightPath &&
                   PathCreator.TurnOrientationLeft(orientation) == orientationToPart)
                {
                    return true;
                }
            
                //these are complicate
                if(part == MazePart.PrimordialPart.TPath &&
                   (orientation != orientationToPart))
                {
                    return true;
                }
                if(part == MazePart.PrimordialPart.XPath || part == MazePart.PrimordialPart.UnknownSplitPath || part == MazePart.PrimordialPart.ToCompute)
                {
                    return true;
                }
            }
        
            return false;
        }
    }   
}