using System.Collections;
using Demonixis.ToolboxV2.Inputs;
using Demonixis.ToolboxV2.Utils;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR;

namespace Demonixis.ToolboxV2.XR
{
    public class XRPawn : MonoBehaviour
    {
        private bool _localPlayer;
        private bool _canRecenter;
        
        [SerializeField] private TrackingOriginModeFlags trackingSpaceType;
        [SerializeField] private float headHeight;
        [SerializeField] private Transform trackingSpace;
        [SerializeField] private Transform mainCamera;
        [SerializeField] private bool autoInitialize;
        [SerializeField] private Transform[] controllerOffsets;

        private void Start()
        {
            if (autoInitialize)
                InitializeLocalPlayer();
        }
        
        private void OnDestroy()
        {
            if (!_localPlayer) return;
            var map = InputSystemManager.Disable("XR");
            map["Recenter"].started -= OnRecenter;
        }
        
        public void InitializeLocalPlayer()
        {
            if (!XRManager.Enabled)
            {
                enabled = false;
                return;
            }

            var handTrackingManager = GetComponentInChildren<HandTrackingManager>();
            if (handTrackingManager != null)
            {
                handTrackingManager.GestureChanged += HandleHandGestureChanged;
            }

            var bEyeSpace = trackingSpaceType == TrackingOriginModeFlags.Device;
            var bMeta = XRManager.Vendor == XRVendor.Meta;

            if (bMeta)
            {
#if OCULUS_BUILD
                if (TryGetComponent(out OVRManager ovrManager))
                {
                     ovrManager.trackingOriginType =
                            bEyeSpace ? OVRManager.TrackingOrigin.EyeLevel : OVRManager.TrackingOrigin.Stage;
                }
#else
                if (XRManager.IsOpenXREnabled())
                {
                    foreach (var ctrl in controllerOffsets)
                        ctrl.localRotation = Quaternion.Euler(45, 0, 0);
                }
#endif
            }

            var originOk = XRManager.SetTrackingOriginMode(trackingSpaceType, true);
            var headOffset = bEyeSpace ? headHeight : 0;
            trackingSpace.localPosition = new Vector3(0.0f, headOffset, 0.0f);
            
            if (!originOk)
                Recenter();
            
            var map = InputSystemManager.Enable("XR");
            map["Recenter"].started += OnRecenter;
            
            Invoke(nameof(Recenter), 1.0f);
            
            _localPlayer = true;
        }
        
        public void SetPassthroughEnabled(bool passthrough)
        {
#if OCULUS_BUILD
            if (mainCamera.TryGetComponent(out OVRPassthroughLayer layer))
                layer.enabled = passthrough;
#endif
            var target = mainCamera.GetComponent<Camera>();
            target.clearFlags = passthrough ? CameraClearFlags.SolidColor : CameraClearFlags.Skybox;
            target.backgroundColor = passthrough ? Color.clear : Color.black;
        }
        
        #region Recenter
        
        public void Recenter()
        {
#if OCULUS_BUILD
            if (OVRManager.display != null)
            {
                OVRManager.display.RecenterPose();
                return;
            }
#endif

            if (!XRManager.Recenter())
                HardRecenter();
        }
        
        private void OnRecenter(InputAction.CallbackContext context)
        {
            Recenter();
        }

        private void HardRecenter()
        {
            var bEyeSpace = trackingSpaceType == TrackingOriginModeFlags.Device;
            var headOffset = bEyeSpace ? headHeight : 0;
            var trackingLoc = -mainCamera.transform.localPosition;
            trackingLoc.y += headOffset;
            trackingSpace.localPosition = trackingLoc;
        }
        
        #endregion
        
        #region Event Handlers
        
        private void HandleHandGestureChanged(HandTrackingManager.HandGestures gesture, bool left, bool prev, bool now)
        {
            var littlePinch = gesture == HandTrackingManager.HandGestures.PinchLittle && now;
            if (!littlePinch) return;

            if (left || !_canRecenter) return;
            Recenter();
            StartCoroutine(WaitForRecenter());
        }
        
        private IEnumerator WaitForRecenter()
        {
            _canRecenter = false;
            yield return CoroutineFactory.WaitForSecondsUnscaled(1.0f);
            _canRecenter = true;
        }
        
        #endregion
    }
}