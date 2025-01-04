using System.Collections;
using Demonixis.ToolboxV2.Utils;
using Demonixis.ToolboxV2.XR;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR;

namespace Wacki
{
    public sealed class IUILaserPointer : MonoBehaviour
    {
        public enum AutoInitializeModes
        {
            None,
            InitAndHide,
            InitAndShow
        }

        private GameObject _hitPoint;
        private GameObject _pointer;
        private float _distanceLimit;
        private bool _enabled = true;
        private bool _initialized;
        private bool _locked;
        private bool _inputPressed;
        private bool _ready;
        private bool _inputReading;

        [SerializeField] private float laserThickness = 0.002f;
        [SerializeField] private float laserHitScale = 0.02f;
        [SerializeField] private Color color = Color.blue;
        [SerializeField] private Material laserMaterial;
        [SerializeField] private AutoInitializeModes autoInitializeMode = AutoInitializeModes.None;
        [SerializeField] private bool forceLaserHidden;
        [SerializeField] private bool visionOsDisabled = true;
        [SerializeField] private InputActionReference pressActionRef;

        public bool AllowExternalPressInput { get; set; }
        public bool ExternalPressInputValue { get; set; }
        public bool HapicEnabled { get; set; } = true;

        public bool ShouldBypass
        {
            get
            {
#if UNITY_VISIONOS
                return _visionOsDisabled;
#else
                return false;
#endif
            }
        }

        public bool IsActive
        {
            get => _enabled;
            set
            {
                if (!_initialized)
                {
                    Initialize();
                }

                _enabled = value;
                _locked = false;
                _hitPoint.SetActive(value && !forceLaserHidden);
                _pointer.SetActive(value && !forceLaserHidden);

                if (value)
                {
                    _inputReading = true;
                    SetInputActionEnabled(true);
                }
                else
                {
                    _inputReading = false;
                    SetInputActionEnabled(false);
                }
            }
        }

        public bool LineVisible
        {
            get => _pointer.activeSelf;
            set
            {
                if (!_initialized)
                {
                    Initialize();
                }

                _pointer.GetComponent<MeshRenderer>().enabled = value && !forceLaserHidden;
            }
        }

        public Material SharedMaterial
        {
            get => _hitPoint.GetComponent<MeshRenderer>().sharedMaterial;
            set
            {
                _pointer.GetComponent<MeshRenderer>().sharedMaterial = value;
                _hitPoint.GetComponent<MeshRenderer>().sharedMaterial = value;
            }
        }

        public GameObject CurrentWidget { get; set; }

        private void Awake()
        {
            if (autoInitializeMode == AutoInitializeModes.None) return;

            Initialize();
            SetInputActionEnabled(true);
        }

        private void SetInputActionEnabled(bool inputEnabled)
        {
            if (ShouldBypass || pressActionRef == null) return;

            if (inputEnabled)
            {
                pressActionRef.action?.Enable();
            }
            else
            {
                pressActionRef.action?.Disable();
            }

            _inputReading = inputEnabled;
        }

        private void OnEnable()
        {
            SetInputActionEnabled(true);
            _locked = false;
        }

        private void OnDisable()
        {
            SetInputActionEnabled(false);
            _locked = false;
        }

        private void OnApplicationPause(bool pause)
        {
            _locked = false;
        }

        public void GetPositionAndRotation(out Vector3 position, out Quaternion rotation)
        {
            var transform1 = transform;
            position = transform1.position;
            rotation = transform1.rotation;
        }

        public void Initialize()
        {
            if (_initialized) return;

            _pointer = GameObject.CreatePrimitive(PrimitiveType.Cube);
            _pointer.transform.SetParent(transform, false);
            _pointer.transform.localScale = new Vector3(laserThickness, laserThickness, 100.0f);
            _pointer.transform.localPosition = new Vector3(0.0f, 0.0f, 50.0f);

            _hitPoint = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            _hitPoint.transform.SetParent(transform, false);
            _hitPoint.transform.localScale = new Vector3(laserHitScale, laserHitScale, laserHitScale);
            _hitPoint.transform.localPosition = new Vector3(0.0f, 0.0f, 100.0f);
            _hitPoint.SetActive(false);

            // remove the colliders on our primitives
            Destroy(_hitPoint.GetComponent<SphereCollider>());
            Destroy(_pointer.GetComponent<BoxCollider>());

            _pointer.GetComponent<MeshRenderer>().sharedMaterial = laserMaterial;
            _hitPoint.GetComponent<MeshRenderer>().sharedMaterial = laserMaterial;

            if (pressActionRef != null && !ShouldBypass)
            {
                var action = pressActionRef.action;
                if (!action.enabled)
                    action.Enable();
                
                action.started += _ => _inputPressed = true;
                action.canceled += _ => _inputPressed = false;
            }

            _initialized = true;

            if (autoInitializeMode == AutoInitializeModes.InitAndHide)
                SetActive(false);
        }

        public void SetActive(bool isActive)
        {
            gameObject.SetActive(isActive);
            IsActive = isActive;
        }

        private void Update()
        {
            if (!_enabled || _pointer == null || ShouldBypass) return;

            if (!_ready)
            {
                if (LaserPointerInputModule.TryGetInstance(out LaserPointerInputModule module))
                {
                    module.AddController(this);
                    _ready = true;
                }
            }

            var transform1 = transform;
            var origin = transform1.position;
            var direction = transform1.forward;

            Ray ray = new Ray(origin, direction);
            RaycastHit hitInfo;

            bool bHit = Physics.Raycast(ray, out hitInfo);
            float distance = 100.0f;

            if (bHit)
            {
                distance = hitInfo.distance;
            }

            // ugly, but has to do for now
            if (_distanceLimit > 0.0f)
            {
                distance = Mathf.Min(distance, _distanceLimit);
                bHit = true;
            }

            _pointer.transform.localScale = new Vector3(laserThickness, laserThickness, distance);
            _pointer.transform.localPosition = new Vector3(0.0f, 0.0f, distance * 0.5f);

            if (bHit)
            {
                _hitPoint.SetActive(true);
                _hitPoint.transform.localPosition = new Vector3(0.0f, 0.0f, distance);
            }
            else
            {
                _hitPoint.SetActive(false);
            }

            // reset the previous distance limit
            _distanceLimit = -1.0f;
        }

        public void LimitLaserDistance(float distance)
        {
            if (distance < 0.0f) return;

            if (_distanceLimit < 0.0f)
            {
                _distanceLimit = distance;
            }
            else
            {
                _distanceLimit = Mathf.Min(_distanceLimit, distance);
            }
        }

        public void OnEnterControl(GameObject control)
        {
            if (!HapicEnabled) return;

            XRManager.Vibrate(this, XRNode.LeftHand, 0.1f, 0.15f);
            XRManager.Vibrate(this, XRNode.RightHand, 0.1f, 0.15f);
        }

        public void OnExitControl(GameObject control)
        {
        }

        public bool ButtonDown()
        {
            if (ShouldBypass) return false;

            bool isPressed = _inputPressed;

            if (AllowExternalPressInput && _inputReading)
            {
                isPressed |= ExternalPressInputValue;
                ExternalPressInputValue = false;
            }

            return isPressed && !_locked;
        }

        public bool ButtonUp()
        {
            if (ShouldBypass) return false;

            bool isUp = !_inputPressed;

            if (AllowExternalPressInput && _inputReading)
                isUp |= ExternalPressInputValue;

            return isUp && _locked;
        }

        public void RequestLockControls()
        {
            if (!_locked)
            {
                StartCoroutine(LockControls());
            }
        }

        private IEnumerator LockControls()
        {
            _locked = true;
            yield return CoroutineFactory.WaitForSeconds(0.5f);
            _locked = false;
        }
    }
}