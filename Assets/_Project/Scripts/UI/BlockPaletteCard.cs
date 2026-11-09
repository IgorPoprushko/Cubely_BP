using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Filtering;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace Cubely.Blocks.Showcase
{
    /// <summary>Owns a real, socketed display block. A normal hand grab turns it into a usable block.</summary>
    [DisallowMultipleComponent]
    public class BlockPaletteCard : MonoBehaviour, IXRSelectFilter, IXRHoverFilter
    {
        [SerializeField] private Text label;
        [SerializeField] private XRSocketInteractor holder;
        [SerializeField, Range(0.1f, 1f)] private float displayScale = 0.75f;
        [SerializeField, Min(0f)] private float displayDepth = 0.035f;
        [SerializeField, Min(0.01f)] private float refillClearance = 0.09f;

        private CardWindowGraphic window;
        private CodeBlockNode displayBlock;
        private XRGrabInteractable displayGrab;
        private Vector3 normalScale;
        private Transform lastTaken;
        private BlockDefinition definition;
        private CodeBlockNode blockPrefab;
        private CodeBlockSlot slotPrefab;

        public bool canProcess => isActiveAndEnabled;

        /// <summary>Called by the palette while this card is inactive, before display creation begins.</summary>
        public void Configure(BlockDefinition blockDefinition, CodeBlockNode sharedBlockPrefab, CodeBlockSlot sharedSlotPrefab)
        {
            definition = blockDefinition;
            blockPrefab = sharedBlockPrefab;
            slotPrefab = sharedSlotPrefab;
        }

        private void Awake()
        {
            window = GetComponent<CardWindowGraphic>();
            if (holder == null) return;
            holder.selectFilters.Add(this);
            holder.hoverFilters.Add(this);
            holder.showInteractableHoverMeshes = false;
            holder.hoverSocketSnapping = false;
            holder.recycleDelayTime = 0f;
            holder.socketScaleMode = SocketScaleMode.Fixed;
            holder.fixedScale = Vector3.one * displayScale;
        }

        private void OnEnable()
        {
            if (label != null && definition != null)
                label.text = definition.displayName;
            if (holder == null || window == null)
            {
                Debug.LogError("Assign a preview holder and CardWindowGraphic to the block card.", this);
                return;
            }
            StartCoroutine(Refill());
        }

        private IEnumerator Refill()
        {
            // Let child interactors register and the UI layout settle before using the holder.
            yield return null;
            var center = window.HoleCenter;
            float canvasScale = Mathf.Abs(transform.lossyScale.z);
            holder.transform.localPosition = new Vector3(center.x, center.y,
                displayDepth / Mathf.Max(canvasScale, 0.0001f));
            while (lastTaken != null && Vector3.Distance(lastTaken.position, holder.transform.position) < refillClearance)
                yield return null;
            lastTaken = null;
            CreateDisplayBlock();
        }

        private void CreateDisplayBlock()
        {
            if (!isActiveAndEnabled || displayBlock != null || holder.interactionManager == null)
                return;
            displayBlock = BlockSpawner.Spawn(blockPrefab, slotPrefab, definition,
                holder.transform.position, holder.transform.rotation);
            if (displayBlock == null) return;
            displayGrab = displayBlock.GetComponent<XRGrabInteractable>();
            normalScale = displayBlock.transform.localScale;
            foreach (var slot in displayBlock.Slots)
                slot.gameObject.SetActive(false);
            // The display block stays outside the scaled Canvas hierarchy, so grabbing never inherits UI scale.
            displayGrab.retainTransformParent = false;
            displayGrab.selectEntered.AddListener(OnTaken);
            holder.interactionManager.SelectEnter((IXRSelectInteractor)holder, displayGrab);
            if (!holder.IsSelecting(displayGrab))
            {
                Debug.LogWarning("The palette holder could not select its display block.", this);
                DestroyDisplayBlock();
            }
        }

        private void OnTaken(SelectEnterEventArgs args)
        {
            if (!(args.interactorObject is XRBaseInputInteractor)) return;
            var block = displayBlock;
            var grab = displayGrab;
            grab.selectEntered.RemoveListener(OnTaken);
            displayBlock = null;
            displayGrab = null;
            // XRI normally keeps the socket's scale transformer until hover exit. End that hover now
            // so the hand immediately receives normal size rather than the socket shrinking it again.
            if (holder.IsHovering(grab))
                holder.interactionManager.HoverExit((IXRHoverInteractor)holder, grab);
            grab.SetTargetLocalScale(normalScale);
            block.transform.localScale = normalScale;
            foreach (var slot in block.Slots)
                slot.gameObject.SetActive(true);
            lastTaken = block.transform;
            StartCoroutine(Refill());
        }

        public bool Process(IXRSelectInteractor interactor, IXRSelectInteractable interactable) =>
            ReferenceEquals(interactable, displayGrab);

        public bool Process(IXRHoverInteractor interactor, IXRHoverInteractable interactable) =>
            ReferenceEquals(interactable, displayGrab);

        private void DestroyDisplayBlock()
        {
            if (displayGrab != null)
                displayGrab.selectEntered.RemoveListener(OnTaken);
            var block = displayBlock;
            displayBlock = null;
            displayGrab = null;
            if (block == null) return;
            block.gameObject.SetActive(false);
            Destroy(block.gameObject);
        }

        private void OnDisable()
        {
            StopAllCoroutines();
            DestroyDisplayBlock();
        }

        private void OnDestroy()
        {
            if (holder == null) return;
            holder.selectFilters.Remove(this);
            holder.hoverFilters.Remove(this);
        }
    }
}
