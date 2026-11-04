using System;
using UnityEngine;

namespace Cubely.Labyrinths
{
    /// <summary>Scene configuration and main-thread grid mapping. Does not move the character yet.</summary>
    [DisallowMultipleComponent]
    public class Labyrinth : MonoBehaviour
    {
        [SerializeField] private Transform character;
        [SerializeField] private Vector2Int size = new Vector2Int(5, 5);
        [Tooltip("Distance between cell centres in this object's local space. Cell (0,0) is at its origin.")]
        [SerializeField, Min(0.001f)] private float cellSize = 1f;
        [SerializeField] private Vector2Int startCell;
        [SerializeField] private GridDirection startDirection = GridDirection.North;
        [SerializeField] private Vector2Int finishCell = new Vector2Int(4, 4);
        [Tooltip("Cells occupied by walls/obstacles. These are logical cells, not physics colliders.")]
        [SerializeField] private Vector2Int[] blockedCells = Array.Empty<Vector2Int>();

        public Transform Character => character;

        /// <summary>Creates a fresh, independent snapshot on the main thread for a future execution.</summary>
        public LabyrinthState CreateState()
        {
            if (float.IsNaN(cellSize) || float.IsInfinity(cellSize) || cellSize <= 0f)
                throw new ArgumentOutOfRangeException(nameof(cellSize), "Cell size must be finite and positive.");

            var obstacles = new GridCell[blockedCells == null ? 0 : blockedCells.Length];
            for (int i = 0; i < obstacles.Length; i++)
                obstacles[i] = new GridCell(blockedCells[i].x, blockedCells[i].y);

            return new LabyrinthState(size.x, size.y, new GridCell(startCell.x, startCell.y),
                startDirection, new GridCell(finishCell.x, finishCell.y), obstacles);
        }

        /// <summary>Unity main thread only. North is local +Z; East is local +X.</summary>
        public Vector3 CellToWorld(GridCell cell) =>
            transform.TransformPoint(new Vector3(cell.X * cellSize, 0f, cell.Y * cellSize));

    }
}
