using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Filtering;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace Cubely.Blocks.Showcase
{
    /// <summary>
    /// One socket accepting blocks held and then released in its hover area.
    /// Used for both ordinary outgoing slots and standalone Start sockets.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(XRSocketInteractor))]
    public class CodeBlockSlot : MonoBehaviour, IXRSelectFilter, IXRHoverFilter
    {
        [SerializeField] private string slotId = "Next";
        [SerializeField] private GameObject marker;

        private XRSocketInteractor socket;
        private readonly HashSet<IXRSelectInteractable> eligibleBlocks = new HashSet<IXRSelectInteractable>();

        private XRSocketInteractor Socket => socket != null
            ? socket : socket = GetComponent<XRSocketInteractor>();

        public string Id => slotId;
        public bool canProcess => isActiveAndEnabled;

        // XRI owns the selection; there is no second cached child or parent reference.
        public CodeBlockNode ConnectedBlock => Socket.hasSelection
            ? Socket.interactablesSelected[0].transform.GetComponentInParent<CodeBlockNode>()
            : null;

        private void Awake()
        {
            // Hover detects placement candidates without moving them out of the hand.
            Socket.hoverExited.AddListener(OnHoverExited);
            Socket.selectEntered.AddListener(OnSelected);
            Socket.selectExited.AddListener(OnReleased);
            Socket.selectFilters.Add(this);
            Socket.hoverFilters.Add(this);
        }

        private void OnEnable()
        {
            RefreshVisual();
        }

        private void OnDisable()
        {
            eligibleBlocks.Clear();
        }

        public void Configure(BlockSlotDefinition slotDefinition)
        {
            slotId = slotDefinition.name;
            gameObject.name = slotId;
            transform.localPosition = slotDefinition.localPosition;
            transform.localRotation = Quaternion.Euler(slotDefinition.localEulerAngles);
        }

        public bool Process(IXRSelectInteractor interactor, IXRSelectInteractable interactable)
        {
            // An existing connection must survive removal from the pending set.
            if (Socket.IsSelecting(interactable))
                return true;

            if (interactable.transform.GetComponentInParent<CodeBlockNode>() == null)
                return false;

            return eligibleBlocks.Contains(interactable) && !interactable.isSelected;
        }

        public bool Process(IXRHoverInteractor interactor, IXRHoverInteractable interactable)
        {
            if (interactable is IXRSelectInteractable selected && Socket.IsSelecting(selected))
                return true;

            if (interactable.transform.GetComponentInParent<CodeBlockNode>() == null)
                return false;

            // Evaluation repeats while hovering, so grabbing a block already in
            // the detection area also makes it eligible. No grab-event subscriptions.
            if (interactable is IXRSelectInteractable selectable && IsHeldByPlayer(selectable))
                eligibleBlocks.Add(selectable);

            return true;
        }

        private static bool IsHeldByPlayer(IXRSelectInteractable interactable)
        {
            Debug.Log("Start IsHeldByPlayer");
            foreach (IXRSelectInteractor interactor in interactable.interactorsSelecting)
            {
                Debug.Log($"For interactor: {interactor}");
                // Includes direct, ray and near/far player input; excludes sockets.
                if (interactor is XRBaseInputInteractor)
                    return true;
            }
            return false;
        }

        private void OnHoverExited(HoverExitEventArgs args)
        {
            if (args.interactableObject is IXRSelectInteractable selectable)
                eligibleBlocks.Remove(selectable);
        }

        private void OnSelected(SelectEnterEventArgs args)
        {
            eligibleBlocks.Remove(args.interactableObject);
            RefreshVisual();
        }

        private void OnReleased(SelectExitEventArgs args)
        {
            RefreshVisual();
        }

        private void RefreshVisual()
        {
            if (marker != null)
                marker.SetActive(!Socket.hasSelection);
        }
    }
}
