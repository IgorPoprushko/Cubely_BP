using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cubely.Labyrinths;
using Vector3 = System.Numerics.Vector3;

namespace Cubely.Execution
{
    /// <summary>Worker-owned synchronous logic. Publishes commands without accessing Unity objects.</summary>
    public sealed class ProgramContext
    {
        private readonly LabyrinthState state;
        private readonly Action<AnimationCommand> publish;
        private readonly CancellationToken cancellationToken;
        private readonly HashSet<int> supportedBlocks;

        public GridCell Position => state.Position;
        public GridDirection Direction => state.Direction;
        public Vector3 ModelRotationOffset => state.ModelRotationOffset;
        public bool HasReachedFinish => state.HasReachedFinish;
        public int Width => state.Width;
        public int Height => state.Height;
        public IReadOnlyCollection<int> AvailableBlockIds { get; }

        /// <summary>Ordinary C# cell edits automatically publish an immutable presentation command.</summary>
        public LabyrinthCell this[GridCell position]
        {
            get => state[position];
            set
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (value.BlockId != 0 && !supportedBlocks.Contains(value.BlockId))
                    throw new ArgumentException($"Block ID {value.BlockId} has no prefab mapping.", nameof(value));
                var previous = state[position];
                state[position] = value;
                if (previous.Surface != value.Surface || previous.BlockId != value.BlockId)
                    publish(AnimationCommand.ChangeCell(position, value));
            }
        }

        public ProgramContext(LabyrinthState initialState, Action<AnimationCommand> publish,
            CancellationToken cancellationToken = default, IEnumerable<int> availableBlockIds = null)
        {
            if (initialState == null)
                throw new ArgumentNullException(nameof(initialState));
            this.publish = publish ?? throw new ArgumentNullException(nameof(publish));
            this.cancellationToken = cancellationToken;

            state = initialState.Copy();
            supportedBlocks = new HashSet<int>(availableBlockIds ??
                initialState.Cells.Where(cell => cell.BlockId != 0).Select(cell => cell.BlockId));
            AvailableBlockIds = Array.AsReadOnly(supportedBlocks.OrderBy(id => id).ToArray());
        }

        /// <summary>Publishes a move or blocked attempt. A blocked attempt returns false and does not stop the program.</summary>
        public bool MoveForward()
        {
            cancellationToken.ThrowIfCancellationRequested();
            GridCell previous = state.Position;
            bool moved = state.TryMoveForward();
            publish(moved
                ? AnimationCommand.Move(previous, state.Position, state.Direction, state.ModelRotationOffset)
                : AnimationCommand.BlockedMove(previous, state.Direction, state.ModelRotationOffset));
            return moved;
        }

        public void TurnRight()
        {
            cancellationToken.ThrowIfCancellationRequested();
            GridDirection previous = state.Direction;
            state.TurnRight();
            publish(AnimationCommand.Turn(state.Position, previous, state.Direction, state.ModelRotationOffset));
        }

        public void TurnLeft()
        {
            cancellationToken.ThrowIfCancellationRequested();
            GridDirection previous = state.Direction;
            state.TurnLeft();
            publish(AnimationCommand.Turn(state.Position, previous, state.Direction, state.ModelRotationOffset));
        }

        public void SetModelRotationOffset(Vector3 eulerDegrees)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Vector3 previous = state.ModelRotationOffset;
            state.SetModelRotationOffset(eulerDegrees);
            publish(AnimationCommand.RotateModel(state.Position, state.Direction, previous, state.ModelRotationOffset));
        }

        /// <summary>Adds an Euler-component offset without changing the logical facing.</summary>
        public void RotateModel(Vector3 eulerDelta) => SetModelRotationOffset(state.ModelRotationOffset + eulerDelta);
    }
}
