using System;
using System.Collections.Generic;
using UnityEngine;

namespace Cubely.Labyrinths
{
    [Serializable]
    public struct LabyrinthCellPlacement
    {
        public Vector2Int position;
        public LabyrinthCell cell;
    }

    /// <summary>Shared layout recipe. Unlisted coordinates are Empty with no block.</summary>
    [CreateAssetMenu(menuName = "Cubely/Labyrinth Definition")]
    public class LabyrinthDefinition : ScriptableObject
    {
        [SerializeField] private Vector2Int size = new Vector2Int(5, 5);
        [SerializeField] private Vector2Int startCell;
        [SerializeField] private GridDirection startDirection;
        [SerializeField] private Vector3 modelRotationOffset;
        [Tooltip("Specify each coordinate at most once. Surface and BlockId are independent; 0 means no block.")]
        [SerializeField] private LabyrinthCellPlacement[] layout = Array.Empty<LabyrinthCellPlacement>();

        /// <summary>Validate and copy this recipe into an independent, Unity-free logical world.</summary>
        public LabyrinthState CreateState()
        {
            if (size.x <= 0 || size.y <= 0 || (long)size.x * size.y > int.MaxValue)
                throw new ArgumentException("Labyrinth dimensions must be positive and fit in an array.");
            var cells = new LabyrinthCell[size.x * size.y];
            var occupied = new HashSet<GridCell>();
            foreach (var placement in layout ?? Array.Empty<LabyrinthCellPlacement>())
            {
                var position = new GridCell(placement.position.x, placement.position.y);
                if (position.X < 0 || position.Y < 0 || position.X >= size.x || position.Y >= size.y)
                    throw new ArgumentException($"Cell {position} is outside the labyrinth.");
                if (!occupied.Add(position))
                    throw new ArgumentException($"Cell {position} is listed more than once.");
                cells[position.Y * size.x + position.X] = placement.cell;
            }
            var state = new LabyrinthState(size.x, size.y, new GridCell(startCell.x, startCell.y), startDirection,
                cells, new System.Numerics.Vector3(modelRotationOffset.x, modelRotationOffset.y, modelRotationOffset.z));
            if (state.IsBlocked(state.Position))
                throw new ArgumentException("The authored start cell must not contain a block.");
            return state;
        }
    }
}
