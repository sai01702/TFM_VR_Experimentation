/*
 * agent that react with each other by the MazeContainer
 */

using UnityEngine;
using Random = System.Random;

namespace MazeGen
{
    public class PathCreator
    {
        private Vector2Int _lastPos;
        private Vector2Int _pos;
        private MazeContainer _container;
        private AgentMazeGenerator _generator;
        private int _orientation; //-2 = no possible direction, -1 = not oriented,0 = x+,1 = y-,2 = x-,3 = y+
        private double _splitProba;
        private int _splited; // -1 = not split, 1 = split right, 2 = split left
        private bool _forceDelete; // enable to force the path to delete by replacing last path with endPath

        /// <summary>
        /// Orientation enum of agents
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
        /// Relative orientation enum of the agent.
        /// </summary>
        public enum RELATIVE_ORIENTATION
        {
            left = -1,
            forward = 0,
            right = 1,
            backward = 2,
        }
        
        /// <summary>
        /// Create new path creator.
        /// </summary>
        /// <param name="generator">Generator in which is the path creator.</param>
        /// <param name="container">Container in which is the path creator.</param>
        /// <param name="pos">Coordinates where the path creator is.</param>
        /// <param name="splitProba">Probability to create a new path creator.</param>
        public PathCreator(AgentMazeGenerator generator, MazeContainer container, Vector2Int pos, double splitProba)
        {
            _generator = generator;
            _container = container;
            _pos = pos;
            _lastPos = Vector2Int.one * -1;
            _orientation = GetRandomDirection();
            _splitProba = splitProba;

            _container.SetPos(pos.x,pos.y,new MazePart(MazePart.PrimordialPart.Reserved));
        }

        /// <summary>
        /// Create new path creator.
        /// </summary>
        /// <param name="generator">Generator in which is the path creator.</param>
        /// <param name="container">Container in which is the path creator.</param>
        /// <param name="pos">Coordinates where the path creator is.</param>
        /// <param name="splitProba">Probability to create a new path creator.</param>
        /// <param name="orientation"> orientation of the path creator</param>
        public PathCreator(AgentMazeGenerator generator, MazeContainer container, Vector2Int pos, double splitProba, int orientation)
        {
            _generator = generator;
            _container = container;
            _pos = pos;
            _lastPos = Vector2Int.one * -1;
            _orientation = orientation;
            _splitProba = splitProba;

            _container.SetPos(pos.x,pos.y,new MazePart(MazePart.PrimordialPart.Reserved));
        }

        /// <summary>
        /// Trigger force delete process
        /// </summary>
        public void ForceDelete()
        {
            _forceDelete = true;
        }

        /// <summary>
        /// Make next movement
        /// </summary>
        /// <returns>return false if agent is stuck</returns>
        public bool Next()
        {
            bool returnValue = RandomNextMovement();
            _splited = -1;
            if (returnValue && _generator.GetNextDouble() <= _splitProba)
            {
                _generator.AddPathCreator(Split());
            }
            return returnValue;
        }
        
        private PathCreator Split() //split can break walls
        {
            bool canRight = false;
            bool canLeft = false;
            int possibilities = 0;
        
            if (_container.GetEmplacement(GetTurnRightPosition()) == (int) MazePart.PrimordialPart.Empty || _container.GetEmplacement(GetTurnRightPosition()) == (int) MazePart.PrimordialPart.Wall)
            {
                canRight = true;
                possibilities++;
            }
            if (_container.GetEmplacement(GetTurnLeftPosition()) == (int) MazePart.PrimordialPart.Empty || _container.GetEmplacement(GetTurnLeftPosition()) == (int) MazePart.PrimordialPart.Wall)
            {
                canLeft = true;
                possibilities++;
            }
        
            //chose a random direction between possibilities
            if(possibilities > 0)
            {
                int dir = _generator.GetNextInt(0, possibilities);
            
                if (canRight)
                {
                    if (dir == 0)
                    {
                        _splited = 1;
                        int splitPathDirection = TurnOrientationRight(_orientation);
                        
                        Vector2Int forwardPos = GetNextPositionInOrientation(_pos, splitPathDirection);
                        Vector2Int doubleForwardPos = GetNextPositionInOrientation(forwardPos, splitPathDirection);
                        MazePart doubleForward = _container.GetPart(doubleForwardPos);
                        if (doubleForward != null)
                        {
                            if (doubleForward.Part != MazePart.PrimordialPart.Empty &&
                                doubleForward.Part != MazePart.PrimordialPart.Reserved &&
                                doubleForward.Part != MazePart.PrimordialPart.Wall)
                            {
                                _container.SetPos(forwardPos.x, forwardPos.y, new MazePart(MazePart.PrimordialPart.StraightPath, splitPathDirection));
                                _container.SetPos(doubleForwardPos.x, doubleForwardPos.y, new MazePart(MazePart.PrimordialPart.UnknownSplitPath, (int) ORIENTATION.notOriented));
                                return null;
                            }
                        }

                        return new PathCreator(_generator, _container, GetTurnRightPosition(), _splitProba, splitPathDirection);
                    }
                    dir--;
                }
                if (canLeft)
                {
                    if (dir == 0)
                    {
                        _splited = 2;
                        int splitPathDirection = TurnOrientationLeft(_orientation);
                        
                        Vector2Int forwardPos = GetNextPositionInOrientation(_pos, splitPathDirection);
                        Vector2Int doubleForwardPos = GetNextPositionInOrientation(forwardPos, splitPathDirection);
                        MazePart doubleForward = _container.GetPart(doubleForwardPos);
                        if (doubleForward != null)
                        {
                            if (doubleForward.Part != MazePart.PrimordialPart.Empty &&
                                doubleForward.Part != MazePart.PrimordialPart.Reserved &&
                                doubleForward.Part != MazePart.PrimordialPart.Wall)
                            {
                                _container.SetPos(forwardPos.x, forwardPos.y, new MazePart(MazePart.PrimordialPart.StraightPath, splitPathDirection));
                                _container.SetPos(doubleForwardPos.x, doubleForwardPos.y, new MazePart(MazePart.PrimordialPart.UnknownSplitPath, (int) ORIENTATION.notOriented));
                                return null;
                            }
                        }
                        
                        return new PathCreator(_generator, _container, GetTurnLeftPosition(), _splitProba, TurnOrientationLeft(_orientation));
                    }
                }
            }

            return null;
        }

        private bool RandomNextMovement()
        {
            if (_forceDelete)
            {
                //Debug.LogError("force deleted at : "+_lastPos);
                HandleForceDelete();
                if (_splited != -1)
                {
                    //Debug.LogError("ça va être chiant"+_pos);
                }
                return false;
            }
            
            int nextDirection = GetNextDirection();
            if (nextDirection == (int) ORIENTATION.impossible)//finish the path if no possible direction
            {
                //Debug.LogError("blocked at : "+_pos);
                switch (_splited)
                {
                    case 1:
                        _container.SetPos(_pos.x,_pos.y,new MazePart(MazePart.PrimordialPart.CornerRightPath,TurnOrientationRight(_orientation)));
                        return false;
                    case 2:
                        _container.SetPos(_pos.x,_pos.y,new MazePart(MazePart.PrimordialPart.CornerLeftPath,TurnOrientationLeft(_orientation)));
                        return false;
                }

                _container.SetPos(_pos.x,_pos.y,new MazePart(MazePart.PrimordialPart.PathEnd, _orientation));
                return false;
            }
        
            Move(nextDirection);
            return true;
        }

        private void HandleForceDelete()
        {
            if(_splited == 1 || _splited == 2) 
                return;
            
            _container.SetPos(_pos.x, _pos.y, new MazePart(MazePart.PrimordialPart.Wall));
            MazePart previousPart = _container.GetPart(_lastPos);
            if(previousPart == null) return; //when new PathCreator created by split is stuck by progression of another PathCreator and lastPos have not been assign by code logic
            switch (previousPart.Part)
            {
                case MazePart.PrimordialPart.CornerLeftPath:
                    _container.SetPos(_lastPos.x,_lastPos.y,new MazePart(MazePart.PrimordialPart.PathEnd, TurnOrientationRight(previousPart.direction)));
                    break;
                case MazePart.PrimordialPart.CornerRightPath:
                    _container.SetPos(_lastPos.x,_lastPos.y,new MazePart(MazePart.PrimordialPart.PathEnd, TurnOrientationLeft(previousPart.direction)));
                    break;
                case MazePart.PrimordialPart.StraightPath:
                    _container.SetPos(_lastPos.x,_lastPos.y,new MazePart(MazePart.PrimordialPart.PathEnd, previousPart.direction));
                    break;
                default:
                    _container.SetPos(_lastPos.x,_lastPos.y,new MazePart(MazePart.PrimordialPart.ToCompute, previousPart.direction));
                    break;
            }
        }

        private int GetNextDirection()
        {
            bool canForward = false;
            bool canRight = false;
            bool canLeft = false;
            int possibilities = 0;
        
            if (CanMoveForward())
            {
                canForward = true;
                possibilities++;
            }
            if (CanMoveRight())
            {
                canRight = true;
                possibilities++;
            }
            if (CanMoveLeft())
            {
                canLeft = true;
                possibilities++;
            }
        
            //chose a random direction between possibilities
            if(possibilities > 0)
            {
                int dir = _generator.GetNextInt(0, possibilities);

                if (canForward)
                {
                    if (dir == 0) return _orientation;
                    dir--;
                }
                if (canRight)
                {
                    if (dir == 0) return TurnOrientationRight(_orientation);
                    dir--;
                }
                if (canLeft)
                {
                    if (dir == 0) return TurnOrientationLeft(_orientation);
                }
            }

            return (int) ORIENTATION.impossible;
        }

        private bool CanMoveForward()
        {
            return _container.GetEmplacement(GetForwardPosition()) == (int) MazePart.PrimordialPart.Empty;
        }

        private bool CanMoveRight()
        {
            return _container.GetEmplacement(GetTurnRightPosition()) == (int) MazePart.PrimordialPart.Empty;
        }

        private bool CanMoveLeft()
        {
            return _container.GetEmplacement(GetTurnLeftPosition()) == (int) MazePart.PrimordialPart.Empty;
        }

        private bool CanMove()
        {
            return CanMoveForward() || CanMoveRight() || CanMoveLeft();
        }

        private int GetRandomDirection()
        {
            bool canXPlus = false;
            bool canXMinus = false;
            bool canYPlus = false;
            bool canYMinus = false;
            int possibilities = 0;
        
            if (_container.GetEmplacement(GetXPlus()) == (int) MazePart.PrimordialPart.Empty)
            {
                canXPlus = true;
                possibilities++;
            }
            if (_container.GetEmplacement(GetXMinus()) == (int) MazePart.PrimordialPart.Empty)
            {
                canXMinus = true;
                possibilities++;
            }
            if (_container.GetEmplacement(GetYPlus()) == (int) MazePart.PrimordialPart.Empty)
            {
                canYPlus = true;
                possibilities++;
            }
            if (_container.GetEmplacement(GetYMinus()) == (int) MazePart.PrimordialPart.Empty)
            {
                canYMinus = true;
                possibilities++;
            }

            //chose a random direction between possibilities
            if(possibilities > 0)//test
            {
                int dir = _generator.GetNextInt(0, possibilities);
                if (canXPlus)
                {
                    if (dir == 0) return (int) ORIENTATION.xPlus;
                    dir--;
                }
                if (canXMinus)
                {
                    if (dir == 0) return (int) ORIENTATION.xMinus;
                    dir--;
                }
                if (canYPlus)
                {
                    if (dir == 0) return (int) ORIENTATION.yPlus;
                    dir--;
                }
                if (canYMinus)
                {
                    if (dir == 0) return (int) ORIENTATION.yMinus;
                }
            }

            return (int) ORIENTATION.impossible;
        }

        private void Move(int orientation)
        {
            switch (CompareOrientation(_orientation,orientation))
            {
                case RELATIVE_ORIENTATION.forward:
                    MoveForward();
                    break;
                case RELATIVE_ORIENTATION.right:
                    MoveRight();
                    break;
                case RELATIVE_ORIENTATION.left:
                    MoveLeft();
                    break;
                default:
                    Debug.LogError("argh !"); //can't move
                    break;
            }

            _orientation = orientation;
        }

        private void MoveForward()
        {
            Vector2Int rightPos = GetTurnRightPosition();
            Vector2Int leftPos = GetTurnLeftPosition();
            switch (_splited)
            {
                case 1:
                    _container.SetPos(_pos.x,_pos.y,new MazePart(MazePart.PrimordialPart.UnknownSplitPath,TurnOrientationRight(_orientation)));
                    _container.MakeWall(leftPos, _generator);
                    break;
                case 2:
                    _container.SetPos(_pos.x,_pos.y,new MazePart(MazePart.PrimordialPart.UnknownSplitPath,TurnOrientationLeft(_orientation)));
                    _container.MakeWall(rightPos, _generator);
                    break;
                default:
                    _container.SetPos(_pos.x,_pos.y,new MazePart(MazePart.PrimordialPart.StraightPath,_orientation));
                    _container.MakeWall(rightPos, _generator);
                    _container.MakeWall(leftPos, _generator);
                    break;
            }
            Vector2Int nextPost = GetForwardPosition();
            _container.SetPos(nextPost.x,nextPost.y,new MazePart(MazePart.PrimordialPart.Reserved));
            _lastPos = _pos;
            _pos = nextPost;
        }

        private void MoveRight()
        {
            Vector2Int forwardPos = GetForwardPosition();
            switch (_splited)
            {
                case 1:
                    Debug.LogError("Should not happen !");
                    break;
                case 2:
                    _container.SetPos(_pos.x,_pos.y,new MazePart(MazePart.PrimordialPart.TPath,TurnOrientationBack(_orientation)));
                    _container.MakeWall(forwardPos, _generator);
                    
                    break;
                default:
                    _container.SetPos(_pos.x,_pos.y,new MazePart(MazePart.PrimordialPart.CornerRightPath,TurnOrientationRight(_orientation)));
                    _container.MakeWall(forwardPos, _generator);
                    Vector2Int leftPos = GetTurnLeftPosition();
                    _container.MakeWall(leftPos, _generator);
                    break;
            }
            Vector2Int nextPost = GetTurnRightPosition();
            _container.SetPos(nextPost.x,nextPost.y,new MazePart(MazePart.PrimordialPart.Reserved));
            _lastPos = _pos;
            _pos = nextPost;
        }

        private void MoveLeft()
        {
            Vector2Int forwardPos = GetForwardPosition();
            switch (_splited)
            {
                case 1:
                    _container.SetPos(_pos.x,_pos.y,new MazePart(MazePart.PrimordialPart.TPath,TurnOrientationBack(_orientation))); //considers the path as a T path (may change if a new path connect to this part)
                    _container.MakeWall(forwardPos, _generator);
                    break;
                case 2:
                    Debug.LogError("Should not happen !");
                    break;
                default:
                    _container.SetPos(_pos.x,_pos.y,new MazePart(MazePart.PrimordialPart.CornerLeftPath,TurnOrientationLeft(_orientation)));
                    _container.MakeWall(forwardPos, _generator);
                    Vector2Int rightPos = GetTurnRightPosition();
                    _container.MakeWall(rightPos, _generator);
                    break;
            }
            Vector2Int nextPost = GetTurnLeftPosition();
            _container.SetPos(nextPost.x,nextPost.y,new MazePart(MazePart.PrimordialPart.Reserved));
            _lastPos = _pos;
            _pos = nextPost;
        }

        private Vector2Int GetForwardPosition()
        {
            return GetNextPositionInOrientation(_orientation);
        }

        private Vector2Int GetTurnRightPosition()
        {
            return GetNextPositionInOrientation(TurnOrientationRight(_orientation));
        }

        private Vector2Int GetTurnLeftPosition()
        {
            return GetNextPositionInOrientation(TurnOrientationLeft(_orientation));
        }

        private Vector2Int GetNextPositionInOrientation(int orientation)
        {
            switch (orientation)
            {
                case (int) ORIENTATION.xPlus:
                    return GetXPlus();
                case (int) ORIENTATION.yMinus:
                    return GetYMinus();
                case (int) ORIENTATION.xMinus:
                    return GetXMinus();
                case (int) ORIENTATION.yPlus:
                    return GetYPlus();
                default:
                    Debug.LogWarning("'GetNextPositionInOrientation' called with an impossible direction");
                    return new Vector2Int(-1, -1);
            }
        }

        private Vector2Int GetNextPositionInOrientation(Vector2Int pos, int orientation)
        {
            switch (orientation)
            {
                case (int) ORIENTATION.xPlus:
                    return MazeContainer.GetXPlusCoord(pos);
                case (int) ORIENTATION.yMinus:
                    return MazeContainer.GetYMinusCoord(pos);
                case (int) ORIENTATION.xMinus:
                    return MazeContainer.GetXMinusCoord(pos);
                case (int) ORIENTATION.yPlus:
                    return MazeContainer.GetYPlusCoord(pos);
                default:
                    Debug.LogWarning("'GetNextPositionInOrientation' called with an impossible direction");
                    return new Vector2Int(-1, -1);
            }
        }

        /// <summary>
        /// Turn an orientation clockwise.
        /// </summary>
        /// <param name="orientation">Original orientation.</param>
        /// <returns>New rotation.</returns>
        public static int TurnOrientationRight(int orientation)
        {
            if (orientation == (int) ORIENTATION.impossible || orientation == (int) ORIENTATION.notOriented)
            {
                return orientation;
            }
            if (orientation == 3)
            {
                return 0;
            }

            orientation++;
            return orientation;
        }

        /// <summary>
        /// Turn an orientation counterclockwise.
        /// </summary>
        /// <param name="orientation">Original orientation.</param>
        /// <returns>New orientation.</returns>
        public static int TurnOrientationLeft(int orientation)
        {
            if (orientation == (int) ORIENTATION.impossible || orientation == (int) ORIENTATION.notOriented)
            {
                return orientation;
            }
            if (orientation == 0)
            {
                return 3;
            }

            orientation--;
            return orientation;
        }

        /// <summary>
        /// Make U-turn to the rotation
        /// </summary>
        /// <param name="orientation">Original orientation.</param>
        /// <returns>New orientation.</returns>
        public static int TurnOrientationBack(int orientation)
        {
            if (orientation == (int) ORIENTATION.impossible || orientation == (int) ORIENTATION.notOriented)
            {
                return orientation;
            }

            if (orientation == (int) ORIENTATION.xPlus || orientation == (int) ORIENTATION.yMinus)
            {
                orientation += 2;
            }
            else if(orientation == (int) ORIENTATION.xMinus || orientation == (int) ORIENTATION.yPlus)
            {
                orientation -= 2;
            }
            
            return orientation;
        }

        /// <summary>
        /// Compare two orientation to give a relative orientation.
        /// </summary>
        /// <param name="original">Base orientation.</param>
        /// <param name="next">Other orientation.</param>
        /// <returns>Return relative orientation between the original one and the other one.</returns>
        public static RELATIVE_ORIENTATION CompareOrientation(int original, int next)//compare to know relative movement(forward,right,left,backward)
        {

            int delta = next - original;
            if (delta == -3)
            {
                return RELATIVE_ORIENTATION.right;
            }
            if (delta == 1)
            {
                return RELATIVE_ORIENTATION.right;
            }
            if (delta == 3)
            {
                return RELATIVE_ORIENTATION.left;
            }
            if (delta == -1)
            {
                return RELATIVE_ORIENTATION.left;
            }

            if (delta == 0)
            {
                return RELATIVE_ORIENTATION.forward;
            }

            if (delta == 2 || delta == -2)
            {
                Debug.LogWarning("backward result should never happen");
            }
            Debug.LogWarning("this result should never happen");
            return RELATIVE_ORIENTATION.backward;
        }

        private Vector2Int GetXPlus()
        {
            return _pos + new Vector2Int(1, 0);
        }

        private Vector2Int GetXMinus()
        {
            return _pos + new Vector2Int(-1, 0);
        }

        private Vector2Int GetYPlus()
        {
            return _pos + new Vector2Int(0, 1);
        }

        private Vector2Int GetYMinus()
        {
            return _pos + new Vector2Int(0, -1);
        }

        /// <summary>
        /// Get coordinates of path creator.
        /// </summary>
        /// <returns>Return coordinates.</returns>
        public Vector2Int GetPos()
        {
            return _pos;
        }

        /// <summary>
        /// Get orientation of path creator.
        /// </summary>
        /// <returns>Return orientation.</returns>
        public int GetOrientation()
        {
            return _orientation;
        }
    }
}