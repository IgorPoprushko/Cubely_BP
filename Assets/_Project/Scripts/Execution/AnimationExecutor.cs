using System;
using System.Collections;
using Cubely.Labyrinths;
using UnityEngine;

namespace Cubely.Execution
{
    /// <summary>Owns main-thread playback, cancellation and recovery to the last completed pose.</summary>
    [DisallowMultipleComponent]
    public class AnimationExecutor : MonoBehaviour
    {
        [SerializeField] private Labyrinth labyrinth;
        [SerializeField, Min(0f)] private float moveDuration = 0.4f;
        [SerializeField, Min(0f)] private float turnDuration = 0.25f;
        [SerializeField, Min(0f)] private float blockedPauseDuration = 0.2f;
        [Tooltip("Character pivot height above the grid, measured along the labyrinth's local Y axis.")]
        [SerializeField] private float characterHeightOffset;

        private ExecutionSession activeSession;
        private Coroutine playback;
        private Transform playbackCharacter;
        private Vector3 completedPosition;
        private Quaternion completedRotation;

        public bool IsPlaying => activeSession != null;
        public Labyrinth Labyrinth => labyrinth;
        public Exception LastError { get; private set; }
        public bool IsPlayingSession(ExecutionSession session) => session != null && activeSession == session;

        /// <summary>Starts playback on this component and takes ownership of session cleanup on success.</summary>
        public bool PlaySession(ExecutionSession session)
        {
            if (session == null || !isActiveAndEnabled || labyrinth == null || labyrinth.Character == null)
            {
                Debug.LogWarning("Assign an enabled AnimationExecutor with a Labyrinth and Character.", this);
                return false;
            }
            if (IsPlaying)
            {
                Debug.LogWarning("Wait for the current playback to finish or stop it first.", this);
                return false;
            }
            if (!IsValidDuration(moveDuration) || !IsValidDuration(turnDuration) || !IsValidDuration(blockedPauseDuration))
            {
                Debug.LogError("Animation durations must be finite and non-negative.", this);
                return false;
            }

            LastError = null;
            playbackCharacter = labyrinth.Character;
            completedPosition = WorldPosition(session.InitialPosition);
            completedRotation = WorldRotation(session.InitialDirection);
            playbackCharacter.SetPositionAndRotation(completedPosition, completedRotation);
            activeSession = session;
            Coroutine started = StartCoroutine(Consume(session));
            // A zero-command program can finish during StartCoroutine itself.
            if (activeSession == session)
                playback = started;
            return true;
        }

        /// <summary>Stops only the supplied session; another runner's playback is never interrupted.</summary>
        public void StopSession(ExecutionSession session)
        {
            if (!IsPlayingSession(session))
                return;

            session.Cancel();
            if (playback != null)
                StopCoroutine(playback);
            FinishSession(session);
        }

        private IEnumerator Consume(ExecutionSession session)
        {
            try
            {
                while (!session.IsDrained)
                {
                    if (session.IsCancellationRequested)
                        yield break;

                    AnimationCommand command;
                    bool available;
                    try
                    {
                        CheckPresentation();
                        available = session.TryTake(out command);
                    }
                    catch (Exception error)
                    {
                        ReportFailure(error);
                        yield break;
                    }

                    if (!available)
                    {
                        yield return null;
                        continue;
                    }

                    // Step the iterator ourselves so animation exceptions reach the
                    // session owner instead of escaping a nested Unity coroutine.
                    IEnumerator animation = Animate(command, session);
                    try
                    {
                        while (true)
                        {
                            bool more;
                            try
                            {
                                more = animation.MoveNext();
                            }
                            catch (Exception error)
                            {
                                ReportFailure(error);
                                yield break;
                            }
                            if (!more)
                                break;
                            yield return animation.Current;
                        }
                    }
                    finally
                    {
                        (animation as IDisposable)?.Dispose();
                    }
                }

                // Preserve the valid queued prefix before reporting a producer fault.
                if (session.LogicTask.IsFaulted)
                    ReportFailure(session.LogicTask.Exception.GetBaseException());
            }
            finally
            {
                FinishSession(session);
            }
        }

        private IEnumerator Animate(AnimationCommand command, ExecutionSession session)
        {
            if (command.Type != AnimationCommandType.Move && command.Type != AnimationCommandType.Turn &&
                command.Type != AnimationCommandType.BlockedMove)
                throw new InvalidOperationException("Unknown animation command type.");

            CheckPresentation();
            Vector3 startPosition = WorldPosition(command.StartCell);
            Vector3 endPosition = WorldPosition(command.EndCell);
            Quaternion startRotation = WorldRotation(command.StartDirection);
            Quaternion endRotation = WorldRotation(command.EndDirection);
            playbackCharacter.SetPositionAndRotation(startPosition, startRotation);

            float duration = command.Type == AnimationCommandType.Move ? moveDuration :
                command.Type == AnimationCommandType.Turn ? turnDuration : blockedPauseDuration;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                if (session.IsCancellationRequested)
                    yield break;
                yield return null;
                if (session.IsCancellationRequested)
                    yield break;

                CheckPresentation();
                elapsed += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsed / duration);
                playbackCharacter.SetPositionAndRotation(Vector3.Lerp(startPosition, endPosition, progress),
                    Quaternion.Slerp(startRotation, endRotation, progress));
            }

            if (session.IsCancellationRequested)
                yield break;

            CheckPresentation();
            playbackCharacter.SetPositionAndRotation(endPosition, endRotation);
            completedPosition = endPosition;
            completedRotation = endRotation;
        }

        private Vector3 WorldPosition(GridCell cell) => labyrinth.CellToWorld(cell) +
            labyrinth.transform.TransformVector(Vector3.up * characterHeightOffset);

        private Quaternion WorldRotation(GridDirection direction) =>
            labyrinth.transform.rotation * Quaternion.Euler(0f, (int)direction * 90f, 0f);

        private static bool IsValidDuration(float duration) =>
            !float.IsNaN(duration) && !float.IsInfinity(duration) && duration >= 0f;

        private void CheckPresentation()
        {
            if (!isActiveAndEnabled || labyrinth == null || playbackCharacter == null ||
                labyrinth.Character != playbackCharacter)
                throw new InvalidOperationException("The playback labyrinth or character was disabled, removed or replaced.");
        }

        private void ReportFailure(Exception error)
        {
            LastError = error;
            Debug.LogException(error, this);
        }

        private void FinishSession(ExecutionSession session)
        {
            if (activeSession != session)
                return;

            // Restore on cancellation/error; on success this is already the final pose.
            if (playbackCharacter != null)
                playbackCharacter.SetPositionAndRotation(completedPosition, completedRotation);
            activeSession = null;
            playback = null;
            playbackCharacter = null;
            session.Dispose(); // Cancels producer and defers disposal until it exits.
        }

        private void OnDisable()
        {
            if (activeSession != null)
                StopSession(activeSession);
        }
    }
}
