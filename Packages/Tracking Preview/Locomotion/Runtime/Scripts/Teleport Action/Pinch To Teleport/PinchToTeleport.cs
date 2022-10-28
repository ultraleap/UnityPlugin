using Leap.Unity.Preview.HandRays;
using UnityEngine;

namespace Leap.Unity.Preview.Locomotion
{
    public class PinchToTeleport : TeleportActionBase
    {
        public LeapProvider leapProvider;

        [SerializeField]
        private IsFacingObject _isFacingObject;

        [SerializeField]
        private LightweightPinchDetector _pinchDetector;

        [Header("The chirality which will be used for pinch to teleport. This will update the chirality in the pinch detector and hand ray.")]
        public Chirality chirality;

        [Header("Rotation Settings"), Tooltip("If true, allows you to move your hand left/right whilst pinching to control the direction you're facing upon teleporting")]
        public bool useRotation = false;

        [Tooltip("The distance your hand needs to move to rotate the teleport anchor 180 degrees")]
        public float maxRotationMovementDistance = 0.2f;

        private Transform _palmForwardTransformHelper;
        private Transform _pinchTransformHelper;
        private Vector3 _rotationOnPinch = Vector3.zero;
        private bool _validOnPinch = false;
        private Chirality _chiralityLastFrame;

        public override void Start()
        {
            base.Start();
            if (leapProvider == null)
            {
                leapProvider = FindObjectOfType<LeapXRServiceProvider>(true);
            }

            _pinchTransformHelper = new GameObject("PinchToTeleport_PinchTransformHelper").transform;
            _pinchTransformHelper.SetParent(transform);
            _palmForwardTransformHelper = new GameObject("PinchToTeleport_PalmForwardTransformHelper").transform;
            _palmForwardTransformHelper.SetParent(transform);
        }

        private void OnEnable()
        {
            _pinchDetector.OnPinch += OnPinch;
            _pinchDetector.OnPinching += OnPinching;
            _pinchDetector.OnUnpinch += OnUnpinch;

            UpdateChirality();
        }

        private void OnDisable()
        {
            _pinchDetector.OnPinch -= OnPinch;
            _pinchDetector.OnPinching -= OnPinching;
            _pinchDetector.OnUnpinch -= OnUnpinch;
        }

        private void UpdateChirality()
        {
            handRayInteractor.handRay.chirality = chirality;
            _pinchDetector.chirality = chirality;
        }

        private void Update()
        {
            _useCustomRotation = useRotation;

            if(chirality != _chiralityLastFrame)
            {
                UpdateChirality();
            }
            _chiralityLastFrame = chirality;

            Hand activeHand = leapProvider.CurrentFrame.GetHand(_pinchDetector.chirality);
            if (activeHand == null)
            {
                return;
            }

            UpdateTeleportSelection(activeHand);
        }

        private void UpdateTeleportSelection(Hand activeHand)
        {
            handRayInteractor.handRay.chirality = _pinchDetector.chirality;
            _palmForwardTransformHelper.position = activeHand.PalmPosition;
            Quaternion palmForwardRotation = activeHand.Rotation * Quaternion.Euler(90, 0, 0);
            _palmForwardTransformHelper.rotation = palmForwardRotation;

            bool isFacingObjectValue = _isFacingObject.ValueThisFrame;
            if (!IsSelected)
            {
                if (!isFacingObjectValue && handRayInteractor.handRay.HandRayEnabled && !_pinchDetector.IsPinching)
                {
                    SelectTeleport(true);
                }
            }
            else
            {
                if (isFacingObjectValue || !handRayInteractor.handRay.HandRayEnabled)
                {
                    SelectTeleport(false);
                }
            }
        }

        private void OnPinch(Hand hand)
        {
            if (IsValid)
            {
                _pinchTransformHelper.position = hand.PalmPosition;
                _pinchTransformHelper.rotation = hand.Rotation;
                _rotationOnPinch = _currentPoint.transform.rotation.eulerAngles;
            }

            _validOnPinch = IsValid;
        }

        private void OnPinching(Hand hand)
        {
            if (!IsValid || !_validOnPinch)
            {
                return;
            }

            if (useRotation)
            {
                handRayRenderer.SetActive(false);
                _currentTeleportAnchorLocked = true;

                float rotationDistance = _pinchTransformHelper.InverseTransformPoint(hand.PalmPosition).x;
                float rotationAngle = rotationDistance * (180 / maxRotationMovementDistance);
                Quaternion newRotation = Quaternion.Euler(_rotationOnPinch.x, _rotationOnPinch.y + rotationAngle, _rotationOnPinch.z);
                _customRotation = newRotation;
                _currentPoint.IndicateRotation(newRotation);
            }
            else
            {
                if (IsSelected)
                {
                    ActivateTeleport();
                }
            }
        }

        private void OnUnpinch(Hand hand)
        {
            if (!_validOnPinch)
            {
                return;
            }

            if (useRotation && IsSelected && IsValid)
            {
                ActivateTeleport();
                _currentPoint.transform.rotation = Quaternion.Euler(_rotationOnPinch);
                handRayRenderer.SetActive(true);
                _currentTeleportAnchorLocked = false;
            }
        }

        private void OnValidate()
        {
            if (leapProvider == null)
            {
                leapProvider = FindObjectOfType<LeapXRServiceProvider>(true);
            }

            if (_isFacingObject == null)
            {
                _isFacingObject = GetComponentInChildren<IsFacingObject>(true);
            }

            if(_pinchDetector == null)
            {
                _pinchDetector = GetComponentInChildren<LightweightPinchDetector>(true);
            }
        }
    }
}