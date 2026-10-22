using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.ARFoundation;

namespace Cubely.XR
{
    /// <summary>Toggles passthrough and the virtual floor with a button action.</summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Camera), typeof(ARCameraManager))]
    public class PassthroughToggle : MonoBehaviour
    {
        [SerializeField] private InputActionReference toggleAction;
        [SerializeField] private GameObject plane;

        private InputAction subscribedAction;
        private Camera targetCamera;
        private ARCameraManager arCameraManager;
        private CameraClearFlags originalClearFlags;
        private Color originalBackgroundColor;

        private void Awake()
        {
            targetCamera = GetComponent<Camera>();
            arCameraManager = GetComponent<ARCameraManager>();
            originalClearFlags = targetCamera.clearFlags;
            originalBackgroundColor = targetCamera.backgroundColor;
        }

        private void OnEnable()
        {
            if (plane == null)
            {
                Debug.LogWarning("Assign the virtual Plane to PassthroughToggle.", this);
                return;
            }

            // The manager's Inspector checkbox determines the initial mode.
            SetPassthrough(arCameraManager.enabled);

            if (toggleAction == null || toggleAction.action == null)
            {
                Debug.LogWarning("Assign a Toggle Action to PassthroughToggle.", this);
                return;
            }

            // InputActionManager owns enabling/disabling the shared action asset.
            subscribedAction = toggleAction.action;
            subscribedAction.performed += OnTogglePerformed;
        }

        private void OnDisable()
        {
            if (subscribedAction == null)
                return;

            subscribedAction.performed -= OnTogglePerformed;
            subscribedAction = null;
        }

        private void OnTogglePerformed(InputAction.CallbackContext context)
        {
            if (plane == null)
                return;

            SetPassthrough(!arCameraManager.enabled);
        }

        private void SetPassthrough(bool enablePassthrough)
        {
            arCameraManager.enabled = enablePassthrough;
            plane.SetActive(!enablePassthrough);
            targetCamera.clearFlags = enablePassthrough ? CameraClearFlags.SolidColor : originalClearFlags;
            targetCamera.backgroundColor = enablePassthrough
                ? new Color(originalBackgroundColor.r, originalBackgroundColor.g, originalBackgroundColor.b, 0f)
                : originalBackgroundColor;
        }
    }
}
