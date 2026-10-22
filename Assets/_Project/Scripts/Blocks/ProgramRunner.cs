using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Cubely.Blocks.Showcase
{
    /// <summary>Builds the program attached to one Start when its input action is performed.</summary>
    [DisallowMultipleComponent]
    public class ProgramRunner : MonoBehaviour
    {
        [SerializeField] private InputActionReference buildAction;
        [SerializeField] private CodeBlockSlot startSlot;

        private InputAction subscribedAction;

        private void OnEnable()
        {
            if (buildAction == null || buildAction.action == null)
            {
                Debug.LogWarning("Assign a Build Action to ProgramRunner.", this);
                return;
            }

            // InputActionManager owns enabling/disabling the shared action asset.
            subscribedAction = buildAction.action;
            subscribedAction.performed += OnBuildPerformed;
        }

        private void OnDisable()
        {
            if (subscribedAction == null)
                return;

            subscribedAction.performed -= OnBuildPerformed;
            subscribedAction = null;
        }

        private void OnBuildPerformed(InputAction.CallbackContext context)
        {
            if (startSlot == null)
            {
                Debug.LogWarning("Assign a Start Slot to ProgramRunner.", this);
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
                // SourceBuilder prints the completed source once; execution comes later.
                SourceBuilder.Build(firstBlock);
            }
            catch (InvalidOperationException error)
            {
                Debug.LogError($"Program source build failed: {error.Message}", this);
            }
        }
    }
}
