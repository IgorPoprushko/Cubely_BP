using UnityEngine;

namespace Cubely.Blocks.Showcase
{
    /// <summary>Shows the scene-authored palette at the hand's current position.</summary>
    public class BlockPaletteWindow : MonoBehaviour
    {
        [SerializeField] private Transform leftHandAnchor;
        [SerializeField] private Transform head;

        public void Show()
        {
            if (leftHandAnchor == null || head == null)
            {
                Debug.LogError("Assign the left hand anchor and head to the block palette.", this);
                return;
            }

            var position = leftHandAnchor.position;
            var forward = position - head.position;
            if (forward.sqrMagnitude < 0.0001f)
                forward = head.forward;
            transform.SetPositionAndRotation(position, Quaternion.LookRotation(forward, head.up));
            gameObject.SetActive(true);
        }

        public void Hide() => gameObject.SetActive(false);
    }
}
