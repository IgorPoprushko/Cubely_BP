using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Filtering;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace Cubely.Blocks.Showcase
{
    /// <summary>
    /// One outgoing socket. Attach beside XRSocketInteractor and assign its marker.
    /// For a temporary Start, put this on a standalone socket and enable
    /// Is Entry Slot. Leave that option off on slots belonging to movable blocks.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(XRSocketInteractor))]
    public class CodeBlockSlot : MonoBehaviour, IXRSelectFilter, IXRHoverFilter
    {
        [SerializeField] private string slotId = "Next";
        [SerializeField] private GameObject marker;
        [Tooltip("Showcase only: a standalone entry socket is always connected to Start.")]
        [SerializeField] private bool isEntrySlot;
        [SerializeField] private InteractionLayerMask freeLayer = 2;
        [SerializeField] private InteractionLayerMask occupiedLayer = 4;

        private XRSocketInteractor socket;
        private bool connectedToStart;

        private XRSocketInteractor Socket => socket != null
            ? socket : socket = GetComponent<XRSocketInteractor>();

        public string Id => slotId;
        public bool IsConnectedToStart => isEntrySlot || connectedToStart;
        public bool canProcess => isActiveAndEnabled;

        // XRI owns the selection; there is no second cached child or parent reference.
        public CodeBlockNode ConnectedBlock => Socket.hasSelection
            ? Socket.interactablesSelected[0].transform.GetComponentInParent<CodeBlockNode>()
            : null;

        private void Awake()
        {
            Socket.selectEntered.AddListener(OnSelected);
            Socket.selectExited.AddListener(OnReleased);
            Socket.selectFilters.Add(this);
            Socket.hoverFilters.Add(this);
        }

        private void OnEnable()
        {
            CodeBlockNode child = ConnectedBlock;
            if (child != null)
                child.SetConnectionState(true, IsConnectedToStart);
            Refresh();
        }

        private void OnDestroy()
        {
            if (socket == null)
                return;
            socket.selectEntered.RemoveListener(OnSelected);
            socket.selectExited.RemoveListener(OnReleased);
            socket.selectFilters.Remove(this);
            socket.hoverFilters.Remove(this);
        }

        // Keep listeners until destruction: disabling the socket/GameObject makes
        // XRI send a selection-exit notification, which must still reach the child.
        // No disconnection is invented solely because this component is disabled.

        public void Configure(BlockSlotDefinition slotDefinition)
        {
            slotId = slotDefinition.name;
            gameObject.name = slotId;
            transform.localPosition = slotDefinition.localPosition;
            transform.localRotation = Quaternion.Euler(slotDefinition.localEulerAngles);
        }

        public void SetConnectedToStart(bool connected)
        {
            connectedToStart = connected;
            Refresh();
        }

        public bool Process(IXRSelectInteractor interactor, IXRSelectInteractable interactable)
        {
            // Only showcase blocks are accepted; XRI handles socket occupancy and
            // socketActive handles availability. No cycle or parent checks.
            return interactable.transform.GetComponentInParent<CodeBlockNode>() != null;
        }

        public bool Process(IXRHoverInteractor interactor, IXRHoverInteractable interactable)
        {
            // Reject non-blocks before hover socket snapping can move them.
            return interactable.transform.GetComponentInParent<CodeBlockNode>() != null;
        }

        private void OnSelected(SelectEnterEventArgs args)
        {
            var child = args.interactableObject.transform.GetComponentInParent<CodeBlockNode>();
            if (child != null)
                child.SetConnectionState(true, IsConnectedToStart);
            Refresh();
        }

        private void OnReleased(SelectExitEventArgs args)
        {
            var child = args.interactableObject.transform.GetComponentInParent<CodeBlockNode>();
            if (child != null)
                child.SetConnectionState(false, false);
            Refresh();
        }

        private void Refresh()
        {
            bool occupied = Socket.hasSelection;
            Socket.socketActive = IsConnectedToStart || occupied;
            Socket.interactionLayers = occupied ? occupiedLayer : freeLayer;
            if (marker != null)
                marker.SetActive(IsConnectedToStart && !occupied);
        }
    }
}
