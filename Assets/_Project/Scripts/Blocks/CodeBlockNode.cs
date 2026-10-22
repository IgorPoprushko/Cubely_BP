using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace Cubely.Blocks.Showcase
{
    /// <summary>
    /// One movable block: its definition, appearance and outgoing slot layout.
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

        private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
        private static readonly int Color = Shader.PropertyToID("_Color");

        public BlockDefinition Definition => definition;
        public IReadOnlyList<CodeBlockSlot> Slots => slots;

        private void OnEnable()
        {
            ApplyDefinition();
        }

        /// <summary>Applies a factory-supplied recipe. All slot objects must already exist.</summary>
        public void Configure(BlockDefinition blockDefinition, CodeBlockSlot[] outgoingSlots)
        {
            definition = blockDefinition;
            slots = outgoingSlots;
            ApplyDefinition();
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
