using System;

namespace Cubely.Labyrinths
{
    public enum CellSurface { Empty, Path, Finish, Hole }

    /// <summary>Teacher-authored cell data. Surface and occupant are independent; default is Empty/no block.</summary>
    [Serializable]
    public struct LabyrinthCell
    {
        public CellSurface Surface;
        public int BlockId;

        public LabyrinthCell(CellSurface surface, int blockId = 0)
        {
            Surface = surface;
            BlockId = blockId;
        }
    }
}
