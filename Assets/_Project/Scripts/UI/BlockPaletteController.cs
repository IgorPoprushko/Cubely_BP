using System;
using UnityEngine;

namespace Cubely.Blocks.Showcase
{
    /// <summary>Creates the palette's cards once and supplies their shared spawning configuration.</summary>
    [DisallowMultipleComponent, RequireComponent(typeof(BlockPalettePages))]
    public class BlockPaletteController : MonoBehaviour
    {
        [Tooltip("Inactive card prefab: it is configured before its first OnEnable.")]
        [SerializeField] private BlockPaletteCard cardPrefab;
        [SerializeField] private RectTransform content;
        [SerializeField] private CodeBlockNode blockPrefab;
        [SerializeField] private CodeBlockSlot slotPrefab;
        [SerializeField] private BlockDefinition[] definitions = Array.Empty<BlockDefinition>();

        private void Awake()
        {
            if (cardPrefab == null || content == null || blockPrefab == null)
            {
                Debug.LogError("Assign the card prefab, content and block prefab to the palette.", this);
                return;
            }
            if (cardPrefab.gameObject.activeSelf)
            {
                Debug.LogError("Keep the card prefab inactive so it can be configured before activation.", this);
                return;
            }
            foreach (var definition in definitions)
            {
                if (definition == null)
                {
                    Debug.LogError("The palette definition list contains an empty entry.", this);
                    return;
                }
            }

            var cards = new BlockPaletteCard[definitions.Length];
            for (int i = 0; i < definitions.Length; i++)
            {
                cards[i] = Instantiate(cardPrefab, content);
                cards[i].name = definitions[i].displayName + " Card";
                cards[i].Configure(definitions[i], blockPrefab, slotPrefab);
            }
            GetComponent<BlockPalettePages>().Initialize(cards, content);
        }
    }
}
