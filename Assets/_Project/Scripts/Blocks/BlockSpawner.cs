using UnityEngine;

namespace Cubely.Blocks.Showcase
{
    /// <summary>Creates ordinary runtime blocks, configured before they become active.</summary>
    public static class BlockSpawner
    {
        public static CodeBlockNode Spawn(CodeBlockNode blockPrefab, CodeBlockSlot slotPrefab,
            BlockDefinition definition, Vector3 position, Quaternion rotation)
        {
            var recipe = definition != null
                ? definition.slots ?? System.Array.Empty<BlockSlotDefinition>()
                : System.Array.Empty<BlockSlotDefinition>();

            if (blockPrefab == null || definition == null || (recipe.Length > 0 && slotPrefab == null))
            {
                Debug.LogError("Assign a block prefab, definition and a slot prefab for recipes with slots.");
                return null;
            }
            if (blockPrefab.GetComponentInChildren<CodeBlockSlot>(true) != null)
            {
                Debug.LogError("Use an ordinary base block prefab without slots.", blockPrefab);
                return null;
            }
            foreach (var slot in recipe)
            {
                if (slot == null)
                {
                    Debug.LogError("The block definition contains an empty slot entry.", definition);
                    return null;
                }
            }

            // An inactive parent prevents prefab OnEnable callbacks during assembly.
            var staging = new GameObject("Block assembly");
            staging.SetActive(false);
            try
            {
                var block = Object.Instantiate(blockPrefab, position, rotation, staging.transform);
                block.gameObject.SetActive(false);
                var slots = new CodeBlockSlot[recipe.Length];
                for (int i = 0; i < slots.Length; i++)
                {
                    slots[i] = Object.Instantiate(slotPrefab, block.transform);
                    slots[i].gameObject.SetActive(true);
                }
                block.Configure(definition, slots);
                block.transform.SetParent(null, true);
                block.gameObject.SetActive(true);
                return block;
            }
            finally
            {
                Object.Destroy(staging);
            }
        }
    }
}
