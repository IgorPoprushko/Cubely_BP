using System;
using Cubely.Execution;
using Cubely.Execution.Examples;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Cubely.Blocks.Showcase
{
    /// <summary>Builds one Start's source and manages its temporary example execution.</summary>
    [DisallowMultipleComponent]
    public class ProgramRunner : MonoBehaviour
    {
        [SerializeField] private InputActionReference buildAction;
        [SerializeField] private InputActionReference stopAction;
        [SerializeField] private CodeBlockSlot startSlot;
        [SerializeField] private AnimationExecutor animationExecutor;
        [SerializeField, Min(1)] private int queueCapacity = 8;

        private InputAction subscribedAction;
        private InputAction subscribedStopAction;
        private ExecutionSession session;

        // Keep the worker in scope after stopping playback: don't launch repeated
        // workers if a future program ignores cooperative cancellation.
        public bool IsRunning => session != null &&
            (!session.LogicTask.IsCompleted ||
             (animationExecutor != null && animationExecutor.IsPlayingSession(session)));

        private void OnEnable()
        {
            // InputActionManager owns enabling/disabling the shared action asset.
            if (buildAction != null && buildAction.action != null)
            {
                subscribedAction = buildAction.action;
                subscribedAction.performed += OnBuildPerformed;
            }
            else
            {
                Debug.LogWarning("Assign a Build Action to ProgramRunner, or invoke RunProgram from UI.", this);
            }

            if (stopAction != null && stopAction.action != null)
            {
                if (stopAction.action == subscribedAction)
                {
                    Debug.LogWarning("Build and Stop must use different input actions.", this);
                    return;
                }
                subscribedStopAction = stopAction.action;
                subscribedStopAction.performed += OnStopPerformed;
            }
        }

        private void OnDisable()
        {
            if (subscribedAction != null)
                subscribedAction.performed -= OnBuildPerformed;
            if (subscribedStopAction != null)
                subscribedStopAction.performed -= OnStopPerformed;
            subscribedAction = null;
            subscribedStopAction = null;
            StopProgram();
        }

        private void OnBuildPerformed(InputAction.CallbackContext context) => RunProgram();
        private void OnStopPerformed(InputAction.CallbackContext context) => StopProgram();

        /// <summary>Input/UI entry point. Generated source is previewed; the handwritten example runs for now.</summary>
        public void RunProgram()
        {
            if (!Application.isPlaying || !isActiveAndEnabled)
                return;
                
            if (IsRunning || (animationExecutor != null && animationExecutor.IsPlaying))
            {
                Debug.LogWarning("A program is already running or its worker is still stopping.", this);
                return;
            }
            if (startSlot == null)
            {
                Debug.LogWarning("Assign a Start Slot to ProgramRunner.", this);
                return;
            }
            if (animationExecutor == null || !animationExecutor.isActiveAndEnabled ||
                animationExecutor.Labyrinth == null || animationExecutor.Labyrinth.Character == null)
            {
                Debug.LogWarning("Assign an enabled AnimationExecutor with a Labyrinth and Character.", this);
                return;
            }

            CodeBlockNode firstBlock = startSlot.ConnectedBlock;
            if (firstBlock == null)
            {
                Debug.Log("No block is connected to this program's Start slot.", this);
                return;
            }

            try
            {
                SourceBuilder.Build(firstBlock);
                // All scene access and snapshot creation happen on the main thread.
                var snapshot = animationExecutor.Labyrinth.CreateState();
                session = new ExecutionSession(snapshot, ExampleProgram.Run, queueCapacity);
                Debug.LogWarning("Runtime compilation is not implemented: running the handwritten move/turn/move example, not the printed block source.", this);
                if (!animationExecutor.PlaySession(session))
                    session.Dispose();
            }
            catch (Exception error)
            {
                StopProgram();
                Debug.LogError($"Program start failed: {error.Message}", this);
            }
        }

        /// <summary>Stops this runner's playback immediately; worker cancellation remains cooperative.</summary>
        public void StopProgram()
        {
            if (session == null)
                return;

            session.Cancel();
            if (animationExecutor != null)
                animationExecutor.StopSession(session);
            // Safe and idempotent after the playback consumer has been stopped.
            session.Dispose();
        }
    }
}
