using System;
using System.Collections.Generic;
using Demonixis.ToolboxV2.XR;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.XR.Hands;
using UnityEngine.XR.Hands.Samples.VisualizerSample;
#if UNITY_VISIONOS
using UnityEngine.InputSystem.XR;
#endif
using Wacki;

namespace Demonixis.ToolboxV2.XR
{
    public class HandTrackingManager : MonoBehaviour
    {
        public enum HandGestures
        {
            Relax,
            Grab,
            PistolPose,
            Shoot,
            Reload,
            PinchIndex,
            PinchMiddle,
            PinchRing,
            PinchLittle
        }

        public enum FingerPinch
        {
            Index = 0,
            Middle,
            Ring,
            Little
        }

        private const int NumHands = 2;
        private const int NumFingers = 5;
        private const float LowThreshold = 0.3f;
        private const float HighThreshold = 0.7f;
        private const int ThumbFingerIndex = 0;
        private const int IndexFingerIndex = 1;
        private const int MiddleFingerIndex = 2;
        private const int RingFingerIndex = 3;
        private const int LittleFingerIndex = 4;
        private const float ThumbHighThreshold = 0.4f;
        private const float PinchThreshold = 0.02f;

        private readonly bool[] _trackingState = new bool[NumHands];
        private readonly float[] _leftFingersValues = new float[NumFingers];
        private readonly float[] _rightFingersValues = new float[NumFingers];
        private XRHandSubsystem _handSubsystem;
        private Dictionary<HandGestures, bool> _leftGestures;
        private Dictionary<HandGestures, bool> _rightGestures;
        private Transform[] _leftFingerProximals = new Transform[NumFingers];
        private Transform[] _rightFingerProximals = new Transform[NumFingers];
        private bool _handVisible;

        [FormerlySerializedAs("_origin")] [SerializeField] private Transform origin;
        [FormerlySerializedAs("_leftSkeleton")] [SerializeField] private XRHandSkeletonDriver leftSkeleton;
        [FormerlySerializedAs("_rightSkeleton")] [SerializeField] private XRHandSkeletonDriver rightSkeleton;
        [FormerlySerializedAs("_laserPointer")] [SerializeField] IUILaserPointer laserPointer;
        [FormerlySerializedAs("_motionControllerGameObjects")] [SerializeField] private GameObject[] motionControllerGameObjects;
        [FormerlySerializedAs("_handTrackingGameObjects")] [SerializeField] private GameObject[] handTrackingGameObjects;

        public bool HandsVisible
        {
            get => _handVisible;
            set
            {
                _handVisible = value;
                
#if UNITY_VISIONOS
                _handVisible = false;
#endif
               if (TryGetComponent(out HandVisualizer visualizer));
                visualizer.drawMeshes = _handVisible;
            }
        }

        public event Action<bool, bool> HandTrackingEnableChanged;
        public event Action<HandGestures, bool, bool, bool> GestureChanged;

        public bool Tracked(bool left)
        {
            return _trackingState[left ? 0 : 1];
        }
        
        private void EnsureStarted()
        {
            if (_leftGestures != null) return;

            if (!XRManager.Enabled)
            {
                gameObject.SetActive(false);
                Destroy(gameObject);
                return;
            }

            _leftGestures = InitializeGestureArray();
            _rightGestures = InitializeGestureArray();

            var joins = leftSkeleton.jointTransformReferences;
            PopulateFingers(ref _leftFingerProximals, joins);

            joins = rightSkeleton.jointTransformReferences;
            PopulateFingers(ref _rightFingerProximals, joins);

#if UNITY_VISIONOS
            HandsVisible = false;
#else
            var leftEvent = leftSkeleton.GetComponent<XRHandTrackingEvents>();
            leftEvent.trackingChanged.AddListener(OnLeftHandTrackingChanged);

            var rightEvent = rightSkeleton.GetComponent<XRHandTrackingEvents>();
            rightEvent.trackingChanged.AddListener(OnRightHandTrackingChanged);
#endif
        }

        private void Start()
        {
            EnsureStarted();

            var loader = XRManager.GetXRLoader();
            if (loader != null)
                _handSubsystem = loader.GetLoadedSubsystem<XRHandSubsystem>();

#if UNITY_VISIONOS
            OnHandTrackingChanged(true, true);
            OnHandTrackingChanged(false, true);
#endif
        }

        private void Update()
        {
            TryCheckGesturesForHand(true);
            TryCheckGesturesForHand(false);
        }

        private void OnLeftHandTrackingChanged(bool tracked)
        {
#if !UNITY_VISIONOS
            OnHandTrackingChanged(true, tracked);
#endif
        }

        private void OnRightHandTrackingChanged(bool tracked)
        {
#if !UNITY_VISIONOS
            OnHandTrackingChanged(false, tracked);
#endif
        }

        private void OnHandTrackingChanged(bool leftHand, bool tracked)
        {
            EnsureStarted();

            var index = leftHand ? 0 : 1;

            _trackingState[index] = tracked;
            motionControllerGameObjects[index].SetActive(!tracked);
            handTrackingGameObjects[index].SetActive(tracked);

            if (!leftHand)
            {
                laserPointer.AllowExternalPressInput = tracked;
                laserPointer.ExternalPressInputValue = false;
            }

            HandTrackingEnableChanged?.Invoke(leftHand, tracked);
        }

        private void OnGestureChanged(HandGestures gestures, bool leftHand, bool previewGestureState,
            bool newGestureState)
        {
            GestureChanged?.Invoke(gestures, leftHand, previewGestureState, newGestureState);
        }

        private bool CheckGesture(HandGestures gesture, ref float[] array)
        {
            if (gesture == HandGestures.Relax)
            {
                // Don't take the Thumb
                for (var i = IndexFingerIndex; i < array.Length; i++)
                {
                    if (array[i] > LowThreshold)
                        return false;
                }

                return true;
            }

            if (gesture == HandGestures.Grab)
            {
                // Don't take the Thumb
                for (var i = IndexFingerIndex; i <= MiddleFingerIndex; i++)
                {
                    if (array[i] < HighThreshold)
                        return false;
                }

                return true;
            }
            
            if (gesture == HandGestures.PistolPose)
            {
                // Only Middle + Ring to prevent bad detection of Pinky
                for (var i = MiddleFingerIndex; i < array.Length - 1; i++)
                {
                    if (array[i] < HighThreshold)
                        return false;
                }

                return true;
            }

            if (gesture == HandGestures.Shoot)
            {
                return array[IndexFingerIndex] >= HighThreshold;
            }

            if (gesture == HandGestures.Reload)
            {
                return array[ThumbFingerIndex] >= ThumbHighThreshold;
            }

            return false;
        }

        private bool IsPinching(bool left, FingerPinch index)
        {
            if (_handSubsystem == null) return false;

            var hand = left ? _handSubsystem.leftHand : _handSubsystem.rightHand;
            if (!hand.isTracked) return false;

            var indexJoint = index switch
            {
                FingerPinch.Index => XRHandJointID.IndexTip,
                FingerPinch.Middle => XRHandJointID.MiddleTip,
                FingerPinch.Ring => XRHandJointID.RingTip,
                FingerPinch.Little => XRHandJointID.LittleTip,
                _ => throw new ArgumentOutOfRangeException(nameof(index), index, null)
            };

            var thumbTip = hand.GetJoint(XRHandJointID.ThumbTip);
            var indexTip = hand.GetJoint(indexJoint);

            if (TryToWorldPose(thumbTip, origin, out var thumbPos) &&
                TryToWorldPose(indexTip, origin, out var indexPos))
            {
                var distance = Vector3.Distance(thumbPos, indexPos);
                if (distance < PinchThreshold)
                {
                    return true;
                }
            }

            return false;
        }

        public bool TryToWorldPose(XRHandJoint joint, Transform origin, out Vector3 result)
        {
            var xrOriginPose = new Pose(origin.position, origin.rotation);
            if (joint.TryGetPose(out Pose jointPose))
            {
                result = jointPose.GetTransformedBy(xrOriginPose).position;
                return true;
            }

            result = Vector3.zero;
            return false;
        }
        
        private void TryCheckGesturesForHand(bool left)
        {
            if (!_trackingState[left ? 0 : 1]) return;

            var gestureArray = left ? _leftGestures : _rightGestures;
            var fingerValues = left ? _leftFingersValues : _rightFingersValues;
            var proximalArray = left ? _leftFingerProximals : _rightFingerProximals;

            // Check
            for (var i = 0; i < proximalArray.Length; i++)
            {
                var boneRotX = Mathf.Abs(proximalArray[i].localEulerAngles.x);
                if (boneRotX > 90) boneRotX = 0;
                var boneRate = boneRotX / 90.0f;

                if (left)
                    _leftFingersValues[i] = boneRate;
                else
                    _rightFingersValues[i] = boneRate;
            }

            // Relax
            var newRelaxGesture = CheckGesture(HandGestures.Relax, ref fingerValues);
            var oldRelaxGesture = gestureArray[HandGestures.Relax];
            if (newRelaxGesture != oldRelaxGesture)
            {
                gestureArray[HandGestures.Relax] = newRelaxGesture;
                OnGestureChanged(HandGestures.Relax, left, oldRelaxGesture, newRelaxGesture);
            }

            // Grab
            var newGrabGesture = CheckGesture(HandGestures.Grab, ref fingerValues);
            var oldGrabGesture = gestureArray[HandGestures.Grab];
            if (newGrabGesture != oldGrabGesture)
            {
                gestureArray[HandGestures.Grab] = newGrabGesture;
                OnGestureChanged(HandGestures.Grab, left, oldGrabGesture, newGrabGesture);
            }
            
            // Pistol
            var newGunGesture = CheckGesture(HandGestures.PistolPose, ref fingerValues);
            var oldGunGesture = gestureArray[HandGestures.PistolPose];
            if (newGunGesture != oldGunGesture)
            {
                gestureArray[HandGestures.PistolPose] = newGunGesture;
                OnGestureChanged(HandGestures.PistolPose, left, oldGunGesture, newGunGesture);
            }
            
            // Shoot
            var newShootGesture = CheckGesture(HandGestures.Shoot, ref fingerValues);
            var oldShootGesture = gestureArray[HandGestures.Shoot];
            if (newShootGesture != oldShootGesture)
            {
                gestureArray[HandGestures.Shoot] = newShootGesture;

                // Gun Pose Required
                if (gestureArray[HandGestures.PistolPose])
                    OnGestureChanged(HandGestures.Shoot, left, oldShootGesture, newShootGesture);
            }
            
            // Reload
            var newReloadGesture = CheckGesture(HandGestures.Reload, ref fingerValues);
            var oldReloadGesture = gestureArray[HandGestures.Reload];
            if (newReloadGesture != oldReloadGesture)
            {
                gestureArray[HandGestures.Reload] = newReloadGesture;

                // Gun Pose Required
                if (gestureArray[HandGestures.PistolPose])
                    OnGestureChanged(HandGestures.Reload, left, oldReloadGesture, newReloadGesture);
            }

            // Pinch
            CheckPinch(left, FingerPinch.Index);
            CheckPinch(left, FingerPinch.Middle);
            CheckPinch(left, FingerPinch.Ring);
            CheckPinch(left, FingerPinch.Little);
        }

        private void CheckPinch(bool left, FingerPinch pinchTarget)
        {
            var gestureTarget = pinchTarget switch
            {
                FingerPinch.Index => HandGestures.PinchIndex,
                FingerPinch.Middle => HandGestures.PinchMiddle,
                FingerPinch.Ring => HandGestures.PinchRing,
                FingerPinch.Little => HandGestures.PinchLittle
            };

            var gestureArray = left ? _leftGestures : _rightGestures;
            var newPinchGesture = IsPinching(left, pinchTarget);
            var oldPinchGesture = gestureArray[gestureTarget];

            if (newPinchGesture != oldPinchGesture)
            {
                gestureArray[gestureTarget] = newPinchGesture;
                OnGestureChanged(gestureTarget, left, oldPinchGesture, newPinchGesture);

                if (pinchTarget == FingerPinch.Index && !left && newPinchGesture)
                {
                    laserPointer.ExternalPressInputValue = true;
                }
            }
        }
        
        private static Dictionary<HandGestures, bool> InitializeGestureArray()
        {
            var names = Enum.GetNames(typeof(HandGestures));
            var arr = new Dictionary<HandGestures, bool>(names.Length);

            for (var i = 0; i < names.Length; i++)
                arr.Add((HandGestures)i, false);

            return arr;
        }

        private static void PopulateFingers(ref Transform[] proximalArray, List<JointToTransformReference> joins)
        {
            foreach (var join in joins)
            {
                if (join.xrHandJointID == XRHandJointID.ThumbProximal)
                    proximalArray[ThumbFingerIndex] = join.jointTransform;
                else if (join.xrHandJointID == XRHandJointID.IndexProximal)
                    proximalArray[IndexFingerIndex] = join.jointTransform;
                else if (join.xrHandJointID == XRHandJointID.MiddleProximal)
                    proximalArray[MiddleFingerIndex] = join.jointTransform;
                else if (join.xrHandJointID == XRHandJointID.RingProximal)
                    proximalArray[RingFingerIndex] = join.jointTransform;
                else if (join.xrHandJointID == XRHandJointID.LittleProximal)
                    proximalArray[LittleFingerIndex] = join.jointTransform;
            }
        }
    }
}