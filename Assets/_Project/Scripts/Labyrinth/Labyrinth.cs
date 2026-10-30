using System;
using System.Collections.Generic;
using UnityEngine;

namespace Cubely.Labyrinths
{
    /// <summary>Scene instance: shared definition, physical grid scale and generated character.</summary>
    [DisallowMultipleComponent]
    public class Labyrinth : MonoBehaviour
    {
        [SerializeField] private LabyrinthDefinition definition;
        [SerializeField] private Transform character;
        [Tooltip("Distance between cell centres in local space. Cell (0,0) is at this object's origin.")]
        [SerializeField, Min(0.001f)] private float cellSize = 1f;
        [SerializeField, HideInInspector] private Quaternion characterBaseRotation = Quaternion.identity;

        private LabyrinthState initialState;
        private float builtCellSize;

        /// <summary>Raised before replacing a built world or disabling it. Playback must stop immediately.</summary>
        public event Action Invalidated;

        public LabyrinthDefinition Definition => definition;
        public Transform Character => character;
        public Quaternion CharacterBaseRotation => characterBaseRotation;
        public float CellSize => cellSize;
        public bool IsReady => isActiveAndEnabled && initialState != null && character != null;
        public IReadOnlyCollection<int> AvailableBlockIds { get; private set; } = Array.Empty<int>();

        /// <summary>Each run starts from the exact layout used to build the current visuals.</summary>
        public LabyrinthState CreateState()
        {
            if (!IsReady)
                throw new InvalidOperationException("Enable and successfully build the labyrinth before running a program.");
            return initialState.Copy();
        }

        internal LabyrinthState ReadDefinition()
        {
            if (float.IsNaN(cellSize) || float.IsInfinity(cellSize) || cellSize <= 0f)
                throw new ArgumentOutOfRangeException(nameof(cellSize), "Cell size must be finite and positive.");
            if (definition == null)
                throw new InvalidOperationException("Assign a Labyrinth Definition before building or running.");
            return definition.CreateState();
        }

        internal void CompleteBuild(LabyrinthState state, Transform value, Quaternion baseRotation, int[] blockIds = null)
        {
            initialState = state;
            builtCellSize = cellSize;
            character = value;
            characterBaseRotation = baseRotation;
            AvailableBlockIds = Array.AsReadOnly(blockIds ?? Array.Empty<int>());
        }

        internal void ApplyCellChange(GridCell position, LabyrinthCell cell) =>
            GetComponent<LabyrinthCreator>().ApplyCellChange(position, cell);

        internal void ResetCellVisuals() => GetComponent<LabyrinthCreator>().ResetCellVisuals();

        internal void Invalidate()
        {
            if (initialState == null)
                return;
            initialState = null;
            Invalidated?.Invoke();
        }

        private void OnEnable()
        {
            if (!Application.isPlaying)
                return;

            Invalidate();
            try
            {
                var creator = GetComponent<LabyrinthCreator>();
                if (creator == null || !creator.enabled)
                    throw new InvalidOperationException("Assign an enabled LabyrinthCreator on the Labyrinth object.");
                creator.Build();
            }
            catch (Exception error)
            {
                Debug.LogException(error, this);
            }
        }

        private void OnDisable()
        {
            if (Application.isPlaying)
                Invalidate();
        }

        /// <summary>Unity main thread only. North is local +Z; East is local +X.</summary>
        public Vector3 CellToWorld(GridCell cell)
        {
            float spacing = initialState != null ? builtCellSize : cellSize;
            return transform.TransformPoint(new Vector3(cell.X * spacing, 0f, cell.Y * spacing));
        }
    }
}
