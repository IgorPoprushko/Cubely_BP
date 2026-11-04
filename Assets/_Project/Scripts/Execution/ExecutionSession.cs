using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Cubely.Labyrinths;

namespace Cubely.Execution
{
    /// <summary>One producer task and its bounded FIFO. The playback owner disposes it after consuming.</summary>
    public sealed class ExecutionSession : IDisposable
    {
        private readonly BlockingCollection<AnimationCommand> commands;
        private readonly CancellationTokenSource cancellation = new CancellationTokenSource();
        private int disposeRequested;

        public Task LogicTask { get; }
        public GridCell InitialPosition { get; }
        public GridDirection InitialDirection { get; }
        public bool IsCancellationRequested => cancellation.IsCancellationRequested;

        /// <summary>Producer has exited and the queue is empty. The consumer may still be animating its last command.</summary>
        public bool IsDrained => LogicTask.IsCompleted && commands.IsCompleted;

        /// <summary>Call on the main thread with a plain-data snapshot. The program executes only on the worker.</summary>
        public ExecutionSession(LabyrinthState initialState, Action<ProgramContext> program, int queueCapacity = 8)
        {
            if (initialState == null)
                throw new ArgumentNullException(nameof(initialState));
            if (program == null)
                throw new ArgumentNullException(nameof(program));
            if (queueCapacity <= 0)
                throw new ArgumentOutOfRangeException(nameof(queueCapacity));

            commands = new BlockingCollection<AnimationCommand>(queueCapacity);
            CancellationToken token = cancellation.Token;
            var context = new ProgramContext(initialState, command => commands.Add(command, token), token);
            InitialPosition = context.Position;
            InitialDirection = context.Direction;

            LogicTask = ProduceAsync(context, program, token);
        }

        private async Task ProduceAsync(ProgramContext context, Action<ProgramContext> program, CancellationToken token)
        {
            try
            {
                await Task.Run(() => program(context), token).ConfigureAwait(false);
            }
            finally
            {
                // Also closes the queue if cancellation prevented Task.Run from starting.
                commands.CompleteAdding();
            }
        }

        /// <summary>Never blocks the caller. Suitable for one poll per Unity frame.</summary>
        public bool TryTake(out AnimationCommand command) => commands.TryTake(out command);

        /// <summary>Cooperative: also releases a producer waiting for queue space.</summary>
        public void Cancel()
        {
            if (Volatile.Read(ref disposeRequested) == 0)
                cancellation.Cancel();
        }

        /// <summary>Call after playback stops. Never waits on the main thread for the producer.</summary>
        public void Dispose()
        {
            if (Interlocked.Exchange(ref disposeRequested, 1) != 0)
                return;

            cancellation.Cancel();
            LogicTask.ContinueWith(completed =>
            {
                // Observe faults even if the owner disappeared before reporting them.
                if (completed.IsFaulted)
                    _ = completed.Exception;
                commands.Dispose();
                cancellation.Dispose();
            }, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
        }
    }
}
