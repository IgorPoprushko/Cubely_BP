using System.Numerics;
using Cubely.Labyrinths;

namespace Cubely.Execution.Examples
{
    public class ExampleProgram
    {
        public static void Run(ProgramContext context)
        {
            // Tilt the model; movement still follows the logical facing.
            context.SetModelRotationOffset(new Vector3(60f, 0f, 0f));
            context.MoveForward();
            context.MoveForward();

            // Ordinary C#: compute the cell ahead and edit its data, without a RemoveBlock command.
            var (dx, dy) = context.Direction switch
            {
                GridDirection.North => (0, 1), GridDirection.East => (1, 0),
                GridDirection.South => (0, -1), _ => (-1, 0),
            };
            var ahead = new GridCell(context.Position.X + dx, context.Position.Y + dy);
            var cell = context[ahead];
            cell.BlockId = 0;
            context[ahead] = cell;

            context.TurnRight();
            // Add a visual yaw offset without changing the movement direction.
            context.RotateModel(new Vector3(0f, 45f, 0f));
            context.MoveForward();
            context.MoveForward();
            context.SetModelRotationOffset(Vector3.Zero);
            context.TurnLeft();
            context.MoveForward();
            context.MoveForward();
            context.MoveForward();
        }
    }
}
