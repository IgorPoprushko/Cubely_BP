using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

namespace Cubely.Blocks.Showcase
{
    /// <summary>Development helper. Creates a block through its Inspector button only.</summary>
    public class BlockFactory : MonoBehaviour
    {
        [Tooltip("Ordinary block prefab with CodeBlockNode and its body renderer assigned. No child slots.")]
        [SerializeField] private CodeBlockNode blockPrefab;
        [Tooltip("Slot prefab with CodeBlockSlot, a trigger collider and Is Entry Slot disabled.")]
        [SerializeField] private CodeBlockSlot slotPrefab;
        [SerializeField] private BlockDefinition definition;

#if UNITY_EDITOR
        private void CreateBlock()
        {
            BlockSlotDefinition[] recipe = definition != null
                ? definition.slots ?? System.Array.Empty<BlockSlotDefinition>()
                : System.Array.Empty<BlockSlotDefinition>();

            if (blockPrefab == null || definition == null || (recipe.Length > 0 && slotPrefab == null))
            {
                Debug.LogWarning("Assign a block prefab, definition and a slot prefab for recipes with slots.", this);
                return;
            }
            if (!PrefabUtility.IsPartOfPrefabAsset(blockPrefab) ||
                (slotPrefab != null && !PrefabUtility.IsPartOfPrefabAsset(slotPrefab)))
            {
                Debug.LogWarning("Assign prefab assets from the Project window.", this);
                return;
            }
            if (blockPrefab.GetComponentInChildren<CodeBlockSlot>(true) != null)
            {
                Debug.LogWarning("Use a base block prefab without slots. The definition supplies its slots.", this);
                return;
            }
            foreach (BlockSlotDefinition slot in recipe)
            {
                if (slot == null)
                {
                    Debug.LogWarning("The definition contains an empty slot entry.", this);
                    return;
                }
            }

            var blockObject = (GameObject)PrefabUtility.InstantiatePrefab(blockPrefab.gameObject, gameObject.scene);
            blockObject.SetActive(false);
            blockObject.transform.SetPositionAndRotation(transform.position, transform.rotation);

            var slots = new CodeBlockSlot[recipe.Length];
            for (int i = 0; i < slots.Length; i++)
            {
                var slotObject = (GameObject)PrefabUtility.InstantiatePrefab(slotPrefab.gameObject, blockObject.transform);
                slotObject.SetActive(true);
                slots[i] = slotObject.GetComponent<CodeBlockSlot>();
            }

            var block = blockObject.GetComponent<CodeBlockNode>();
            block.Configure(definition, slots);
            blockObject.SetActive(true);

            PrefabUtility.RecordPrefabInstancePropertyModifications(block);
            PrefabUtility.RecordPrefabInstancePropertyModifications(blockObject);
            PrefabUtility.RecordPrefabInstancePropertyModifications(blockObject.transform);
            foreach (CodeBlockSlot slot in slots)
            {
                PrefabUtility.RecordPrefabInstancePropertyModifications(slot);
                PrefabUtility.RecordPrefabInstancePropertyModifications(slot.gameObject);
                PrefabUtility.RecordPrefabInstancePropertyModifications(slot.transform);
            }
            Undo.RegisterCreatedObjectUndo(blockObject, "Create Code Block");
            EditorSceneManager.MarkSceneDirty(blockObject.scene);
            EditorGUIUtility.PingObject(blockObject);
        }

        [CustomEditor(typeof(BlockFactory))]
        private class FactoryInspector : Editor
        {
            public override void OnInspectorGUI()
            {
                DrawDefaultInspector();
                if (GUILayout.Button("Create Block"))
                    ((BlockFactory)target).CreateBlock();
            }
        }
#endif
    }
}
