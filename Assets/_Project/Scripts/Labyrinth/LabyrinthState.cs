using System;
using System.Collections.Generic;

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
    /// Obstacles occupy whole cells and the outer grid boundary is impassable.
    /// </summary>
    public sealed class LabyrinthState
    {
        private readonly HashSet<GridCell> blockedCells;

        public int Width { get; }
        public int Height { get; }
        public GridCell Position { get; private set; }
        public GridDirection Direction { get; private set; }
        public GridCell Finish { get; }
        public bool HasReachedFinish => Position.Equals(Finish);

        public LabyrinthState(int width, int height, GridCell start, GridDirection direction,
            GridCell finish, IEnumerable<GridCell> obstacles)
        {
            if (width <= 0 || height <= 0)
                throw new ArgumentOutOfRangeException(nameof(width), "Grid dimensions must both be positive.");
            if (direction < GridDirection.North || direction > GridDirection.West)
                throw new ArgumentOutOfRangeException(nameof(direction));

            Width = width;
            Height = height;
            blockedCells = obstacles == null ? new HashSet<GridCell>() : new HashSet<GridCell>(obstacles);
            foreach (GridCell cell in blockedCells)
                if (!IsInside(cell))
                    throw new ArgumentException($"Obstacle {cell} is outside the grid.", nameof(obstacles));

            if (!IsInside(start) || IsBlocked(start))
                throw new ArgumentException("Start must be an unblocked cell inside the grid.", nameof(start));
            if (!IsInside(finish) || IsBlocked(finish))
                throw new ArgumentException("Finish must be an unblocked cell inside the grid.", nameof(finish));

            Position = start;
            Direction = direction;
            Finish = finish;
        }

        public bool IsInside(GridCell cell) => cell.X >= 0 && cell.Y >= 0 && cell.X < Width && cell.Y < Height;
        public bool IsBlocked(GridCell cell) => blockedCells.Contains(cell);

        /// <summary>Copies the current pose and grid into an independently mutable world.</summary>
        public LabyrinthState Copy() => new LabyrinthState(Width, Height, Position, Direction, Finish, blockedCells);

        /// <summary>Moves one cell, or returns false without changing state if blocked.</summary>
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
    }
}
