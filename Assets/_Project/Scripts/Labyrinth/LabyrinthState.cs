using System;
using System.Collections.Generic;
using Vector3 = System.Numerics.Vector3;

namespace Cubely.Labyrinths
{
    public enum GridDirection { North, East, South, West }

    /// <summary>Plain-data coordinate. X maps to local X; Y maps to local Z.</summary>
    public readonly struct GridCell : IEquatable<GridCell>
    {
        public int X { get; }
        public int Y { get; }

        public GridCell(int x, int y)
        {
            X = x;
            Y = y;
        }

        public bool Equals(GridCell other) => X == other.X && Y == other.Y;
        public override bool Equals(object obj) => obj is GridCell other && Equals(other);
        public override int GetHashCode() => unchecked((X * 397) ^ Y);
        public override string ToString() => $"({X}, {Y})";
    }

    /// <summary>
    /// Independent logical world, with no Unity references or animation.
    /// One execution owns and mutates one instance; this is not shared mutable state.
    /// Facing controls movement; the model's Euler offset is presentation only.
    /// </summary>
    public sealed class LabyrinthState
    {
        private readonly LabyrinthCell[] cells;

        public int Width { get; }
        public int Height { get; }
        public GridCell Position { get; private set; }
        public GridDirection Direction { get; private set; }
        public Vector3 ModelRotationOffset { get; private set; }
        /// <summary>Read-only row-major view; use the coordinate indexer to edit a cell.</summary>
        public IReadOnlyList<LabyrinthCell> Cells { get; }
        public bool HasReachedFinish => cells[Position.Y * Width + Position.X].Surface == CellSurface.Finish;

        public LabyrinthCell this[GridCell position]
        {
            get
            {
                if (!IsInside(position)) throw new ArgumentOutOfRangeException(nameof(position));
                return cells[position.Y * Width + position.X];
            }
            set
            {
                if (!IsInside(position)) throw new ArgumentOutOfRangeException(nameof(position));
                if (value.Surface < CellSurface.Empty || value.Surface > CellSurface.Hole || value.BlockId < 0)
                    throw new ArgumentException("Cells need a valid surface and a non-negative block ID.", nameof(value));
                cells[position.Y * Width + position.X] = value;
            }
        }

        /// <summary>Copies supplied cell data. Unspecified trailing cells remain Empty with BlockId zero.</summary>
        public LabyrinthState(int width, int height, GridCell start, GridDirection direction,
            IEnumerable<LabyrinthCell> layout = null, Vector3 modelRotationOffset = default)
        {
            cells = new LabyrinthCell[CellCount(width, height)];
            if (direction < GridDirection.North || direction > GridDirection.West)
                throw new ArgumentOutOfRangeException(nameof(direction));

            Width = width;
            Height = height;
            if (layout != null)
            {
                int index = 0;
                foreach (LabyrinthCell cell in layout)
                {
                    if (index == cells.Length)
                        throw new ArgumentException("Layout has more cells than the grid.", nameof(layout));
                    if (cell.Surface < CellSurface.Empty || cell.Surface > CellSurface.Hole || cell.BlockId < 0)
                        throw new ArgumentException("Cells need a valid surface and a non-negative block ID.", nameof(layout));
                    cells[index++] = cell;
                }
            }

            if (!IsInside(start))
                throw new ArgumentException("Position must be inside the grid.", nameof(start));
            Position = start;
            Direction = direction;
            SetModelRotationOffset(modelRotationOffset);
            Cells = Array.AsReadOnly(cells);
        }

        public bool IsInside(GridCell cell) => cell.X >= 0 && cell.Y >= 0 && cell.X < Width && cell.Y < Height;
        public bool IsBlocked(GridCell cell) => IsInside(cell) && cells[cell.Y * Width + cell.X].BlockId != 0;

        /// <summary>Copies the current pose and grid into an independently mutable world.</summary>
        public LabyrinthState Copy() => new LabyrinthState(Width, Height, Position, Direction, cells, ModelRotationOffset);

        /// <summary>Euler degrees, independent of grid facing. Does not change movement direction.</summary>
        public void SetModelRotationOffset(Vector3 eulerDegrees)
        {
            if (!IsFinite(eulerDegrees.X) || !IsFinite(eulerDegrees.Y) || !IsFinite(eulerDegrees.Z))
                throw new ArgumentOutOfRangeException(nameof(eulerDegrees), "Rotation angles must be finite.");
            ModelRotationOffset = eulerDegrees;
        }

        /// <summary>Adds a delta to the Euler offset components; this is not a world-space quaternion rotation.</summary>
        public void RotateModel(Vector3 eulerDelta) => SetModelRotationOffset(ModelRotationOffset + eulerDelta);

        /// <summary>Moves by facing. Only boundaries and nonzero block IDs block movement; surface rules are task-specific.</summary>
        public bool TryMoveForward()
        {
            int x = Position.X;
            int y = Position.Y;
            switch (Direction)
            {
                case GridDirection.North: y++; break;
                case GridDirection.East: x++; break;
                case GridDirection.South: y--; break;
                case GridDirection.West: x--; break;
            }

            var destination = new GridCell(x, y);
            if (!IsInside(destination) || IsBlocked(destination))
                return false;

            Position = destination;
            return true;
        }

        public void TurnRight() => Direction = (GridDirection)(((int)Direction + 1) % 4);
        public void TurnLeft() => Direction = (GridDirection)(((int)Direction + 3) % 4);

        private static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);

        private static int CellCount(int width, int height)
        {
            if (width <= 0 || height <= 0 || (long)width * height > int.MaxValue)
                throw new ArgumentOutOfRangeException(nameof(width), "Grid dimensions must be positive and fit in an array.");
            return width * height;
        }

    }
}
