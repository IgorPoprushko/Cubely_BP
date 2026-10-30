using System;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Cubely.Labyrinths
{
    /// <summary>Builds prefab visuals and a character from this Labyrinth's assigned definition.</summary>
    [DisallowMultipleComponent, RequireComponent(typeof(Labyrinth))]
    public class LabyrinthCreator : MonoBehaviour
    {
        [Serializable]
        private struct BlockPrefab
        {
            [Min(1)] public int id;
            public GameObject prefab;
        }

        [Header("Prefabs (unit-cell dimensions, pivot at ground level)")]
        [SerializeField] private GameObject pathPrefab;
        [SerializeField] private GameObject finishPrefab;
        [SerializeField] private GameObject holePrefab;
        [SerializeField] private BlockPrefab[] blockPrefabs = Array.Empty<BlockPrefab>();
        [SerializeField] private GameObject characterPrefab;
        [Tooltip("Only this generated root is replaced. Must be a direct child of the Labyrinth.")]
        [SerializeField] private Transform generatedContent;

        private sealed class CellVisual
        {
            public LabyrinthCell Cell;
            public GameObject Surface;
            public GameObject Block;
        }

        private Dictionary<GridCell, CellVisual> cellVisuals;
        private Dictionary<int, GameObject> builtBlocks;
        private Dictionary<CellSurface, GameObject> builtSurfaces;
        private readonly HashSet<GridCell> changedCells = new HashSet<GridCell>();
        private LabyrinthState builtState;
        private float builtCellSize;

        /// <summary>Builds on activation or as an editor preview. Runtime rebuilding cancels current playback first.</summary>
        public void Build()
        {
            var labyrinth = GetComponent<Labyrinth>();
            var state = labyrinth.ReadDefinition();
            if (characterPrefab == null)
                throw new InvalidOperationException("Assign a character prefab.");
            if (generatedContent != null && generatedContent.parent != transform)
                throw new InvalidOperationException("Generated Content must be a direct child of the Labyrinth.");
            var blocks = new Dictionary<int, GameObject>();
            foreach (var entry in blockPrefabs ?? Array.Empty<BlockPrefab>())
            {
                if (entry.id <= 0 || entry.prefab == null || !blocks.TryAdd(entry.id, entry.prefab))
                    throw new InvalidOperationException("Block mappings require unique positive IDs and assigned prefabs.");
            }
            foreach (var cell in state.Cells)
            {
                if (cell.Surface != CellSurface.Empty && SurfacePrefab(cell.Surface) == null)
                    throw new InvalidOperationException($"Assign a prefab for surface {cell.Surface}.");
                if (cell.BlockId != 0 && !blocks.ContainsKey(cell.BlockId))
                    throw new InvalidOperationException($"Assign a prefab for block ID {cell.BlockId}.");
            }
            if (Application.isPlaying)
            {
                if (!labyrinth.isActiveAndEnabled)
                    throw new InvalidOperationException("Enable the labyrinth before rebuilding it.");
                labyrinth.Invalidate();
            }
#if UNITY_EDITOR
            bool useUndo = !Application.isPlaying;
            int undoGroup = -1;
            if (useUndo)
            {
                Undo.IncrementCurrentGroup();
                undoGroup = Undo.GetCurrentGroup();
                Undo.SetCurrentGroupName("Build Labyrinth");
            }
#endif
            var root = new GameObject("Generated Labyrinth");
            root.SetActive(false);
            root.transform.SetParent(transform, false);
            try
            {
                var visuals = new Dictionary<GridCell, CellVisual>();
                for (int y = 0; y < state.Height; y++)
                for (int x = 0; x < state.Width; x++)
                {
                    var cell = state.Cells[y * state.Width + x];
                    var position = new Vector3(x * labyrinth.CellSize, 0f, y * labyrinth.CellSize);
                    var surface = SurfacePrefab(cell.Surface);
                    var surfaceObject = surface != null
                        ? Spawn(surface, root.transform, position, labyrinth.CellSize, $"{cell.Surface} ({x}, {y})") : null;
                    var blockObject = cell.BlockId != 0
                        ? Spawn(blocks[cell.BlockId], root.transform, position, labyrinth.CellSize, $"Block {cell.BlockId} ({x}, {y})") : null;
                    if (surfaceObject != null || blockObject != null)
                        visuals.Add(new GridCell(x, y), new CellVisual { Cell = cell, Surface = surfaceObject, Block = blockObject });
                }
                var character = Spawn(characterPrefab, root.transform,
                    new Vector3(state.Position.X * labyrinth.CellSize, 0f, state.Position.Y * labyrinth.CellSize),
                    labyrinth.CellSize, "Character");
                var basis = characterPrefab.transform.localRotation;
                var offset = state.ModelRotationOffset;
                character.transform.localRotation = Quaternion.Euler(0f, (int)state.Direction * 90f, 0f) *
                    basis * Quaternion.Euler(offset.X, offset.Y, offset.Z);
#if UNITY_EDITOR
                if (useUndo)
                {
                    PrefabUtility.RecordPrefabInstancePropertyModifications(character.transform);
                    Undo.RegisterCreatedObjectUndo(root, "Build Labyrinth");
                    Undo.RecordObject(this, "Build Labyrinth");
                    Undo.RecordObject(labyrinth, "Build Labyrinth");
                }
#endif
                if (generatedContent != null)
                {
#if UNITY_EDITOR
                    if (useUndo) Undo.DestroyObjectImmediate(generatedContent.gameObject);
                    else
#endif
                    {
                        generatedContent.gameObject.SetActive(false);
                        Destroy(generatedContent.gameObject);
                    }
                }
                generatedContent = root.transform;
                cellVisuals = visuals;
                builtState = state;
                builtCellSize = labyrinth.CellSize;
                builtBlocks = blocks;
                builtSurfaces = new Dictionary<CellSurface, GameObject>
                {
                    [CellSurface.Path] = pathPrefab, [CellSurface.Finish] = finishPrefab, [CellSurface.Hole] = holePrefab,
                };
                changedCells.Clear();
                var blockIds = new int[blocks.Count];
                blocks.Keys.CopyTo(blockIds, 0);
                labyrinth.CompleteBuild(state, character.transform, basis, blockIds);
                root.SetActive(true);
#if UNITY_EDITOR
                if (useUndo)
                {
                    EditorUtility.SetDirty(this);
                    EditorUtility.SetDirty(labyrinth);
                    Undo.CollapseUndoOperations(undoGroup);
                }
#endif
            }
            catch
            {
#if UNITY_EDITOR
                if (useUndo)
                {
                    Undo.RevertAllDownToGroup(undoGroup);
                    if (root != null) DestroyImmediate(root);
                }
                else
#endif
                    Destroy(root);
                throw;
            }
        }

        /// <summary>Main-thread presentation only. Logical state is owned and edited by the worker.</summary>
        internal void ApplyCellChange(GridCell position, LabyrinthCell cell)
        {
            UpdateCellVisual(position, cell);
            changedCells.Add(position);
        }

        internal void ResetCellVisuals()
        {
            foreach (var position in changedCells)
                UpdateCellVisual(position, builtState[position]);
            changedCells.Clear();
        }

        private void UpdateCellVisual(GridCell position, LabyrinthCell cell)
        {
            if (builtState == null || generatedContent == null || !builtState.IsInside(position))
                throw new InvalidOperationException("Build the labyrinth before applying cell changes.");
            GameObject surfacePrefab = null;
            GameObject blockPrefab = null;
            if (cell.Surface != CellSurface.Empty &&
                (!builtSurfaces.TryGetValue(cell.Surface, out surfacePrefab) || surfacePrefab == null))
                throw new InvalidOperationException($"No built prefab mapping for surface {cell.Surface}.");
            if (cell.BlockId != 0 && (!builtBlocks.TryGetValue(cell.BlockId, out blockPrefab) || blockPrefab == null))
                throw new InvalidOperationException($"No built prefab mapping for block {cell.BlockId}.");

            cellVisuals.TryGetValue(position, out var previous);
            var oldCell = previous != null ? previous.Cell : default;
            bool surfaceChanged = oldCell.Surface != cell.Surface;
            bool blockChanged = oldCell.BlockId != cell.BlockId;
            var localPosition = new Vector3(position.X * builtCellSize, 0f, position.Y * builtCellSize);
            GameObject surface = previous?.Surface;
            GameObject block = previous?.Block;
            GameObject newSurface = null;
            GameObject newBlock = null;
            try
            {
                if (surfaceChanged && surfacePrefab != null)
                    newSurface = Spawn(surfacePrefab, generatedContent, localPosition, builtCellSize, $"{cell.Surface} {position}");
                if (blockChanged && blockPrefab != null)
                    newBlock = Spawn(blockPrefab, generatedContent, localPosition, builtCellSize, $"Block {cell.BlockId} {position}");
            }
            catch
            {
                RemoveVisual(newSurface);
                RemoveVisual(newBlock);
                throw;
            }
            if (surfaceChanged) { RemoveVisual(surface); surface = newSurface; }
            if (blockChanged) { RemoveVisual(block); block = newBlock; }
            if (surface == null && block == null)
                cellVisuals.Remove(position);
            else
                cellVisuals[position] = new CellVisual { Cell = cell, Surface = surface, Block = block };
        }

        private static void RemoveVisual(GameObject visual)
        {
            if (visual == null) return;
            visual.SetActive(false);
            Destroy(visual);
        }

        private GameObject SurfacePrefab(CellSurface surface) => surface switch
        {
            CellSurface.Path => pathPrefab,
            CellSurface.Finish => finishPrefab,
            CellSurface.Hole => holePrefab,
            _ => null,
        };

        private static GameObject Spawn(GameObject prefab, Transform parent, Vector3 position, float cellSize, string objectName)
        {
            GameObject instance;
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                if (!PrefabUtility.IsPartOfPrefabAsset(prefab))
                    throw new InvalidOperationException("Assign prefab assets from the Project window.");
                instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            }
            else
#endif
                instance = Instantiate(prefab, parent);
            instance.name = objectName;
            instance.transform.localPosition = position;
            instance.transform.localScale = prefab.transform.localScale * cellSize;
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                PrefabUtility.RecordPrefabInstancePropertyModifications(instance);
                PrefabUtility.RecordPrefabInstancePropertyModifications(instance.transform);
            }
#endif
            return instance;
        }

#if UNITY_EDITOR
        [CustomEditor(typeof(LabyrinthCreator))]
        private class CreatorInspector : Editor
        {
            public override void OnInspectorGUI()
            {
                DrawDefaultInspector();
                using (new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode))
                {
                    if (!GUILayout.Button("Build Labyrinth")) return;
                    try { ((LabyrinthCreator)target).Build(); }
                    catch (Exception exception) { Debug.LogException(exception, target); }
                }
            }
        }
#endif
    }
}
