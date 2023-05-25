/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2023.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using UnityEngine;

namespace Leap.Unity.Preview.Locomotion
{
    /// <summary>
    /// PinchToTeleport allows you to point with a hand ray interactor and pinch with your hand to teleport where you're pointing to
    /// It has an optional rotation toggle, which when enabled, whilst pinching allows you to set your rotation by moving your hand horizontally
    /// </summary>
    public class PinchToTeleport : TeleportActionBase
    {
        [Header("Pinch To Teleport - Setup")]
        public LeapProvider leapProvider;

        [SerializeField]
        private IsFacingObject _isFacingObject;

        [SerializeField]
        private LightweightPinchDetector _pinchDetector;

        [Tooltip("The chirality which will be used for pinch to teleport. This will update the chirality in the pinch detector and hand ray.")]
        public Chirality chirality;

        [Header("Pinch To Teleport - Rotation Settings")]
        [Tooltip("If true, allows you to move your hand left/right whilst pinching to control the direction you're facing upon teleporting" +
            "\n Not to be used in tandem with useHeadsetForwardRotation.")]
        public bool useRotation = false;

        [Tooltip("The distance your hand needs to move to rotate the teleport anchor 180 degrees")]
        public float maxRotationMovementDistance = 0.2f;

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
            _chiralityLastFrame = chirality;
        }

        protected void OnEnable()
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

        protected override void Update()
        {
            base.Update();

            if (chirality != _chiralityLastFrame)
            {
                UpdateChirality();
            }
            _chiralityLastFrame = chirality;

            if (leapProvider == null || leapProvider.CurrentFrame == null)
            {
                return;
            }
            Hand activeHand = leapProvider.CurrentFrame.GetHand(chirality);
            if (activeHand == null)
            {
                return;
            }

            UpdateTeleportSelection(activeHand);
        }

        private void UpdateTeleportSelection(Hand activeHand)
        {
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
                _rotationOnPinch = _currentAnchor.transform.rotation.eulerAngles;
                _useCustomRotation = useRotation && !useHeadsetForwardRotationForFixed;
                CalculateCustomRotation(hand);
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

                CalculateCustomRotation(hand);
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
                _currentAnchor.transform.rotation = Quaternion.Euler(_rotationOnPinch);
                handRayRenderer.SetActive(true);
                _currentTeleportAnchorLocked = false;
                _useCustomRotation = false;
            }
        }

        private void CalculateCustomRotation(Hand hand)
        {
            float rotationDistance = _pinchTransformHelper.InverseTransformPoint(hand.PalmPosition).x;
            float rotationAngle = rotationDistance * (180 / maxRotationMovementDistance);
            Quaternion newRotation = Quaternion.Euler(_rotationOnPinch.x, _rotationOnPinch.y + rotationAngle, _rotationOnPinch.z);
            _customRotation = newRotation;
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

            if (_pinchDetector == null)
            {
                _pinchDetector = GetComponentInChildren<LightweightPinchDetector>(true);
            }
        }
    }
}