/*
 * Contain a maze made of Primordial maze part and control the agent used to create it
 */

using System;
using UnityEngine;

namespace MazeGen
{
    public class MazeContainer
    {
        protected MazePart[,] _maze;
        private int _sizeX;
        private int _sizeY;
        
        public int SizeX => _sizeX;
        public int SizeY => _sizeY;

        public delegate MazePart PathOperation(int x,int y, MazePart actualPart);
        
        /// <summary>
        /// Orientation enum of agents and maze parts
        /// </summary>
        public enum ORIENTATION
        {
            impossible = -2,
            notOriented = -1,
            xPlus = 0,
            yMinus = 1,
            xMinus = 2,
            yPlus = 3,
        }

        /// <summary>
        /// Create a new maze container.
        /// </summary>
        /// <param name="xSize">X size.</param>
        /// <param name="ySize">Y size.</param>
        /// <param name="defaultPrimordialID">Default maze part to be placed (should be the empty one).</param>
        public MazeContainer(int xSize, int ySize, int defaultPrimordialID)
        {
            _sizeX = xSize;
            _sizeY = ySize;
            _maze = new MazePart[_sizeX,_sizeY];
        
            for (int x = 0; x < _sizeX; x++)
            {
                for (int y = 0; y < _sizeY; y++)
                {
                
                    _maze[x, y] = new MazePart(defaultPrimordialID);
                
                }
            }
        }
        
        /// <summary>
        /// Set a maze part at a specific position.
        /// </summary>
        /// <param name="x">X coordinate.</param>
        /// <param name="y">Y coordinate.</param>
        /// <param name="mazePart">Maze part to be placed.</param>
        public virtual void SetPos(int x, int y, MazePart mazePart)
        {
            _maze[x, y] = mazePart;
        }

        /// <summary>
        /// Verify if position is in the maze bounds before setting the value.
        /// </summary>
        public void VerifyAndSetPos(int x, int y, MazePart mazePart)
        {
            if (IsInMaze(x, y))
            {
                _maze[x, y] = mazePart;
            }
        }

        /// <summary>
        /// Verify whether or not coordinates are in the maze bounds.
        /// </summary>
        /// <param name="pos">Coordinates.</param>
        /// <returns>Return true if coordinates are in the maze bounds.</returns>
        public bool IsInMaze(Vector2Int pos)
        {
            return (pos.x < _sizeX && pos.x >= 0) && (pos.y < _sizeY && pos.y >= 0);
        }

        /// <summary>
        /// Verify whether or not coordinates are in the maze bounds.
        /// </summary>
        /// <param name="x">X coordinate.</param>
        /// <param name="y">Y coordinate.</param>
        /// <returns>Return true if coordinates are in the maze bounds.</returns>
        public bool IsInMaze(int x, int y)
        {
            return (x < _sizeX && x >= 0) && (y < _sizeY && y >= 0);
        }

        /// <summary>
        /// Make wall at a specific position and handle specific cases.
        /// </summary>
        /// <param name="pos">Coordinates to place wall.</param>
        /// <param name="generator">Generator used to generate this maze.</param>
        public void MakeWall(Vector2Int pos, AgentMazeGenerator generator)
        {
            if (IsInMaze(pos))
            {
                MazePart emplacement = GetPartNotVerified(pos);
                if (emplacement.Part == MazePart.PrimordialPart.Reserved)
                {
                    GetPathCreatorAtPos(pos, generator)?.ForceDelete();
                    _maze[pos.x, pos.y] = new MazePart(MazePart.PrimordialPart.ToCompute);
                    return;
                }

                if (emplacement.Part == MazePart.PrimordialPart.Empty)
                {
                    _maze[pos.x, pos.y] = new MazePart(MazePart.PrimordialPart.Wall);
                    return;
                }
            }
        }

        private static PathCreator GetPathCreatorAtPos(Vector2Int pos, AgentMazeGenerator generator)
        {
            foreach (PathCreator pathCreator in generator.GetAllPathCreators())
            {
                if (pathCreator.GetPos() == pos)
                {
                    return pathCreator;
                }
            }
            
            return null;
        }

        /// <summary>
        /// Make an operation for each maze part.
        /// </summary>
        /// <param name="operation">Operation to be perform.</param>
        public void ForEachEmplacements(PathOperation operation)
        {
            for (int x = 0; x < _sizeX; x++)
            {
                for (int y = 0; y < _sizeY; y++)
                {
                    MazePart emplacement = operation(x, y, GetPartNotVerified(new Vector2Int(x, y)));
                    SetPos(x, y, emplacement);
                }
            }
        }

        /// <summary>
        /// Get a maze part at a specific position.
        /// </summary>
        /// <param name="pos">Coordinates to pick the maze part.</param>
        /// <returns>Return the maze part as an int.</returns>
        public int GetEmplacement(Vector2Int pos)
        {
            if (IsInMaze(pos))
            {
                return (int) _maze[pos.x, pos.y].Part;
            }

            return 0;
        }

        /// <summary>
        /// Get a maze part at a specific position.
        /// </summary>
        /// <param name="pos">Coordinates to pick the maze part.</param>
        /// <returns>Return the maze part</returns>
        public MazePart GetPart(Vector2Int pos)
        {
            if (IsInMaze(pos))
            {
                return _maze[pos.x, pos.y];
            }

            return null;
        }

        /// <summary>
        /// Get a maze part at a specific position without verifying if it is in the maze(could make out of bound exception).
        /// </summary>
        /// <param name="pos">Coordinates to pick the maze part.</param>
        /// <returns>Return the maze part</returns>
        public MazePart GetPartNotVerified(Vector2Int pos)
        {
            return _maze[pos.x, pos.y];
        }

        /// <summary>
        /// Get coordinates at x+1.
        /// </summary>
        /// <param name="pos">Coordinates</param>
        /// <returns>x+1 coordinates.</returns>
        public static Vector2Int GetXPlusCoord(Vector2Int pos)
        {
            return pos + new Vector2Int(1, 0);
        }

        /// <summary>
        /// Get coordinates at x-1.
        /// </summary>
        /// <param name="pos">Coordinates</param>
        /// <returns>x-1 coordinates.</returns>
        public static  Vector2Int GetXMinusCoord(Vector2Int pos)
        {
            return pos + new Vector2Int(-1, 0);
        }

        /// <summary>
        /// Get coordinates at y+1.
        /// </summary>
        /// <param name="pos">Coordinates</param>
        /// <returns>y+1 coordinates.</returns>
        public static  Vector2Int GetYPlusCoord(Vector2Int pos)
        {
            return pos + new Vector2Int(0, 1);
        }

        /// <summary>
        /// Get coordinates at y-1.
        /// </summary>
        /// <param name="pos">Coordinates</param>
        /// <returns>y-1 coordinates.</returns>
        public static  Vector2Int GetYMinusCoord(Vector2Int pos)
        {
            return pos + new Vector2Int(0, -1);
        }
        
        /// <summary>
        /// Get MazePart at coordinates x+1.
        /// </summary>
        /// <param name="pos">Coordinates</param>
        /// <returns>Maze part at x+1 coordinates.</returns>
        public MazePart GetXPlus(Vector2Int pos)
        {
            Vector2Int coord = GetXPlusCoord(pos);
            return GetPart(coord);
        }

        /// <summary>
        /// Get MazePart at coordinates x-1.
        /// </summary>
        /// <param name="pos">Coordinates</param>
        /// <returns>Maze part at x-1 coordinates.</returns>
        public  MazePart GetXMinus(Vector2Int pos)
        {
            Vector2Int coord = GetXMinusCoord(pos);
            return GetPart(coord);
        }

        /// <summary>
        /// Get MazePart at coordinates y+1.
        /// </summary>
        /// <param name="pos">Coordinates</param>
        /// <returns>Maze part at y+1 coordinates.</returns>
        public  MazePart GetYPlus(Vector2Int pos)
        {
            Vector2Int coord = GetYPlusCoord(pos);
            return GetPart(coord);
        }

        /// <summary>
        /// Get MazePart at coordinates y-1.
        /// </summary>
        /// <param name="pos">Coordinates</param>
        /// <returns>Maze part at y-1 coordinates.</returns>
        public  MazePart GetYMinus(Vector2Int pos)
        {
            Vector2Int coord = GetYMinusCoord(pos);
            return GetPart(coord);
        }
    }   
}