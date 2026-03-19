namespace MazeGen
{
    public class MazePart
    {
        
        public int part;
        public int type;
        public int direction;
        
        
        public PrimordialPart Part => (PrimordialPart) part;
        
        /// <summary>
        /// Create a new maze part whit type and orientation initialised to 0.
        /// </summary>
        /// <param name="part">Part represented.</param>
        public MazePart(PrimordialPart part)
        {
            this.part = (int) part;
            direction = 0;
            type = 0;
        }
        
        /// <summary>
        /// Create a new maze part white type and orientation initialised to 0.
        /// </summary>
        /// <param name="part">Part represented.</param>
        public MazePart(int part)
        {
            this.part = part;
            direction = 0;
            type = 0;
        }
        
        /// <summary>
        /// Create a new maze part white type initialised to 0.
        /// </summary>
        /// <param name="part">Part represented.</param>
        /// <param name="direction">Orientation of part.</param>
        public MazePart(PrimordialPart part,int direction)
        {
            this.part = (int) part;
            this.direction = direction;
            type = 0;
        }
        
        /// <summary>
        /// Create a new maze part
        /// </summary>
        /// <param name="part">Part represented.</param>
        /// <param name="direction">Orientation of part.</param>
        /// <param name="type">Type of part.</param>
        public MazePart(PrimordialPart part,int direction, int type)
        {
            this.part = (int) part;
            this.direction = direction;
            this.type = type;
        }
    
        /// <summary>
        /// Primordial part used in maze generation
        /// </summary>
        public enum PrimordialPart
        {
            StraightPath = 1,
            CornerRightPath = 2,
            CornerLeftPath = 3,
            TPath = 4,
            XPath = 5,
            PathEnd = 6,
            MazeEnd = 7,
            Empty = 10,
            Wall = 11,
            Reserved = 12,
            UnknownSplitPath = 20,
            ToCompute = 21,
            OutOfControl = 22,
        }
    }
}