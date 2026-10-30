using Cubely.Labyrinths;
using Vector3 = System.Numerics.Vector3;

namespace Cubely.Execution
{
    public enum AnimationCommandType { Move, Turn, BlockedMove, ModelRotation, CellChanged }

    /// <summary>Action poses or a changed cell's value snapshot. Contains no Unity objects.</summary>
    public readonly struct AnimationCommand
    {
        public AnimationCommandType Type { get; }
        public GridCell StartCell { get; }
        public GridCell EndCell { get; }
        public GridDirection StartDirection { get; }
        public GridDirection EndDirection { get; }
        public Vector3 StartModelRotationOffset { get; }
        public Vector3 EndModelRotationOffset { get; }
        public LabyrinthCell CellValue { get; }

        private AnimationCommand(AnimationCommandType type, GridCell startCell, GridCell endCell,
            GridDirection startDirection, GridDirection endDirection,
            Vector3 startModelRotationOffset, Vector3 endModelRotationOffset, LabyrinthCell cellValue = default)
        {
            Type = type;
            StartCell = startCell;
            EndCell = endCell;
            StartDirection = startDirection;
            EndDirection = endDirection;
            StartModelRotationOffset = startModelRotationOffset;
            EndModelRotationOffset = endModelRotationOffset;
            CellValue = cellValue;
        }

        public static AnimationCommand Move(GridCell from, GridCell to, GridDirection direction, Vector3 modelOffset = default) =>
            new AnimationCommand(AnimationCommandType.Move, from, to, direction, direction, modelOffset, modelOffset);

        public static AnimationCommand Turn(GridCell cell, GridDirection from, GridDirection to, Vector3 modelOffset = default) =>
            new AnimationCommand(AnimationCommandType.Turn, cell, cell, from, to, modelOffset, modelOffset);

        public static AnimationCommand BlockedMove(GridCell cell, GridDirection direction, Vector3 modelOffset = default) =>
            new AnimationCommand(AnimationCommandType.BlockedMove, cell, cell, direction, direction, modelOffset, modelOffset);

        public static AnimationCommand RotateModel(GridCell cell, GridDirection facing, Vector3 from, Vector3 to) =>
            new AnimationCommand(AnimationCommandType.ModelRotation, cell, cell, facing, facing, from, to);

        public static AnimationCommand ChangeCell(GridCell position, LabyrinthCell cell) =>
            new AnimationCommand(AnimationCommandType.CellChanged, position, position, default, default, default, default, cell);
    }
}
