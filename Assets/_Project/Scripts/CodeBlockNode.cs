using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace Cubely.Blocks.Showcase
{
    /// <summary>
    /// One movable block: its definition, appearance, grab state and outgoing slots.
    /// Attach to the same root as XRGrabInteractable.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(XRGrabInteractable))]
    public class CodeBlockNode : MonoBehaviour
    {
        [SerializeField] private BlockDefinition definition;
        [SerializeField] private Renderer bodyRenderer;
        [Tooltip("Assign existing child slots. This showcase does not instantiate slot prefabs.")]
        [SerializeField] private CodeBlockSlot[] slots = System.Array.Empty<CodeBlockSlot>();
        [SerializeField] private InteractionLayerMask freeLayer = 2;
        [SerializeField] private InteractionLayerMask attachedLayer = 4;

        private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
        private static readonly int Color = Shader.PropertyToID("_Color");
        private XRGrabInteractable grab;

        public BlockDefinition Definition => definition;
        public IReadOnlyList<CodeBlockSlot> Slots => slots;
        public bool IsAttached { get; private set; }
        public bool IsConnectedToStart { get; private set; }

        private void OnEnable()
        {
            ApplyDefinition();
            RefreshConnections();
        }

        /// <summary>For later factory use. All slot objects must already exist.</summary>
        public void Configure(BlockDefinition blockDefinition, CodeBlockSlot[] outgoingSlots)
        {
            definition = blockDefinition;
            slots = outgoingSlots;
            ApplyDefinition();
            if (Application.isPlaying)
                RefreshConnections();
        }

        /// <summary>Called by the incoming socket when this block is selected or released.</summary>
        public void SetConnectionState(bool attached, bool connectedToStart)
        {
            IsAttached = attached;
            SetConnectedToStart(connectedToStart);
        }

        private void SetConnectedToStart(bool connected)
        {
            IsConnectedToStart = connected;
            RefreshConnections();
        }

        private void RefreshConnections()
        {
            if (grab == null)
                grab = GetComponent<XRGrabInteractable>();
            grab.interactionLayers = IsAttached ? attachedLayer : freeLayer;

            foreach (CodeBlockSlot slot in slots)
            {
                if (slot == null)
                    continue;

                slot.SetConnectedToStart(IsConnectedToStart);
                CodeBlockNode child = slot.ConnectedBlock;
                if (child != null)
                {
                    // Propagate Start availability, not attachment state. Internal
                    // children stay attached when the subtree's root is released.
                    child.SetConnectedToStart(IsConnectedToStart);
                }
            }
        }

        private void ApplyDefinition()
        {
            if (definition == null)
                return;

            gameObject.name = string.IsNullOrWhiteSpace(definition.displayName)
                ? definition.name : definition.displayName;

            if (bodyRenderer != null)
            {
                var properties = new MaterialPropertyBlock();
                bodyRenderer.GetPropertyBlock(properties);
                properties.SetColor(BaseColor, definition.color);
                properties.SetColor(Color, definition.color);
                bodyRenderer.SetPropertyBlock(properties);
            }

            // Match the manually assigned slots to the recipe by array index.
            // Placeholder identity remains the slot's name, not its array index.
            if (definition.slots == null)
                return;

            for (int i = 0; i < slots.Length && i < definition.slots.Length; i++)
            {
                if (slots[i] != null && definition.slots[i] != null)
                    slots[i].Configure(definition.slots[i]);
            }
        }
    }
}
