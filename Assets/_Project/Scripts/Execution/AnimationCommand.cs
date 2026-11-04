 using Cubely.Labyrinths;

namespace Cubely.Execution
{
    public enum AnimationCommandType { Move, Turn, BlockedMove }

    /// <summary>A validated action's poses. Contains no Unity objects or animation logic.</summary>
    public readonly struct AnimationCommand
    {
        public AnimationCommandType Type { get; }
        public GridCell StartCell { get; }
        public GridCell EndCell { get; }
        public GridDirection StartDirection { get; }
        public GridDirection EndDirection { get; }

        private AnimationCommand(AnimationCommandType type, GridCell startCell, GridCell endCell,
            GridDirection startDirection, GridDirection endDirection)
        {
            Type = type;
            StartCell = startCell;
            EndCell = endCell;
            StartDirection = startDirection;
            EndDirection = endDirection;
        }

        public static AnimationCommand Move(GridCell from, GridCell to, GridDirection direction) =>
            new AnimationCommand(AnimationCommandType.Move, from, to, direction, direction);

        public static AnimationCommand Turn(GridCell cell, GridDirection from, GridDirection to) =>
            new AnimationCommand(AnimationCommandType.Turn, cell, cell, from, to);

        public static AnimationCommand BlockedMove(GridCell cell, GridDirection direction) =>
            new AnimationCommand(AnimationCommandType.BlockedMove, cell, cell, direction, direction);
    }
}
