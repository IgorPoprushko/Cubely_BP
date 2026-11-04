using System;
using System.Threading;
using Cubely.Labyrinths;

namespace Cubely.Execution
{
    /// <summary>Worker-owned synchronous logic. Publishes commands without accessing Unity objects.</summary>
    public sealed class ProgramContext
    {
        private readonly LabyrinthState state;
        private readonly Action<AnimationCommand> publish;
        private readonly CancellationToken cancellationToken;

        public GridCell Position => state.Position;
        public GridDirection Direction => state.Direction;
        public bool HasReachedFinish => state.HasReachedFinish;

        public ProgramContext(LabyrinthState initialState, Action<AnimationCommand> publish,
            CancellationToken cancellationToken = default)
        {
            if (initialState == null)
                throw new ArgumentNullException(nameof(initialState));
            this.publish = publish ?? throw new ArgumentNullException(nameof(publish));
            this.cancellationToken = cancellationToken;

            state = initialState.Copy();
        }

        /// <summary>Publishes a move or blocked attempt. A blocked attempt returns false and does not stop the program.</summary>
        public bool MoveForward()
        {
            cancellationToken.ThrowIfCancellationRequested();
            GridCell previous = state.Position;
            bool moved = state.TryMoveForward();
            publish(moved
                ? AnimationCommand.Move(previous, state.Position, state.Direction)
                : AnimationCommand.BlockedMove(previous, state.Direction));
            return moved;
        }

        public void TurnRight()
        {
            cancellationToken.ThrowIfCancellationRequested();
            GridDirection previous = state.Direction;
            state.TurnRight();
            publish(AnimationCommand.Turn(state.Position, previous, state.Direction));
        }

        public void TurnLeft()
        {
            cancellationToken.ThrowIfCancellationRequested();
            GridDirection previous = state.Direction;
            state.TurnLeft();
            publish(AnimationCommand.Turn(state.Position, previous, state.Direction));
        }
    }
}
