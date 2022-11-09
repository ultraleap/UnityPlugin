/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2022.                                   *
 * Ultraleap proprietary and confidential.                                    *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/
using Leap.Unity.Preview.HandRays;
using UnityEngine;

namespace Leap.Unity.Preview.Locomotion
{
    /// <summary>
    /// RayLatchTeleport snaps onto the teleport anchor you're pointing at, and teleports you once you pull either up or down.
    /// Note: Free Teleportation is not currently supported by RayLatchTeleport.
    /// </summary>
    public class RayLatchTeleport : TeleportActionBase
    {
        public enum Direction { UP, DOWN };
        [Header("Ray Latch - Setup")]
        public LeapProvider leapProvider;
        public LightweightGrabDetector grabDetector;
        public GameObject FixedHandRay;
        public GameObject FreeHandRay;

        [SerializeField]
        private IsFacingObject _isFacingObject;

        [Tooltip("The chirality which will be used for teleporting. Setting this will also set the chirality in the grab detector and hand ray.")]
        public Chirality chirality;

        [Tooltip("The direction in which you will need to pull in order to teleport")]
        public Direction rayLatchDirection = Direction.UP;

        [Header("Ray Latch - Interaction Setup")]

        [Tooltip("How far you will need to pull, in order to activate the teleport")]
        [SerializeField, Range(0.01f, 0.5f)]
        private float _distanceToGrabAndPull = 0.1f;

        private Chirality _chiralityLastFrame;
        private bool _grabbingLastFrame = false;
        private PullUI _pullUI = null;
        private TeleportAnchor _latchedAnchor = null;
        private Transform _transformWhenActivated;

        protected void OnEnable()
        {
            //movementType = TeleportActionMovementType.FIXED;
            grabDetector.OnGrab += OnGrab;
            grabDetector.OnGrabbing += OnGrabbing;
            grabDetector.OnUngrab += OnUngrab;

            OnTeleport += OnTeleportActivated;
            OnMovementTypeChanged += MovementTypeChanged;

            OnMovementTypeChanged(movementType);
            UpdateChirality();
        }

        private void OnDisable()
        {
            grabDetector.OnGrab -= OnGrab;
            grabDetector.OnGrabbing -= OnGrabbing;
            grabDetector.OnUngrab -= OnUngrab;

            OnTeleport -= OnTeleportActivated;
            OnMovementTypeChanged -= MovementTypeChanged;
        }

        public override void Start()
        {
            base.Start();

            if(leapProvider == null)
            {
                leapProvider = FindObjectOfType<LeapProvider>();
            }

            _pullUI = GetComponentInChildren<PullUI>();
            _pullUI.gameObject.SetActive(false);

            _transformWhenActivated = new GameObject("RayLatchTransformHelper").transform;
            _transformWhenActivated.transform.parent = transform;
            _chiralityLastFrame = chirality;
        }


        protected override void Update()
        {
            base.Update();

            if (chirality != _chiralityLastFrame)
            {
                UpdateChirality();
            }
            _chiralityLastFrame = chirality;

            Hand activeHand = leapProvider.CurrentFrame.GetHand(chirality);
            if (activeHand == null)
            {
                return;
            }


            if (!_currentTeleportAnchorLocked)
            {
                UpdateTeleportSelection(activeHand);
                if (IsValid)
                {
                    if (_currentAnchor != _latchedAnchor)
                    {
                        _latchedAnchor = _currentAnchor;
                    }
                }
                else
                {
                    _latchedAnchor = null;
                }
            }

            if (_currentTeleportAnchorLocked && activeHand != null)
            {
                handRayRenderer.lineRenderer.positionCount = 2;
                handRayRenderer.lineRenderer.SetPosition(0, activeHand.PalmPosition);
                handRayRenderer.lineRenderer.SetPosition(1, _latchedAnchor.transform.position);
                handRayRenderer.SetActive(true);
            }

            _grabbingLastFrame = grabDetector.IsGrabbing;
        }

        private void UpdateChirality()
        {
            handRayInteractor.handRay.chirality = chirality;
            grabDetector.chirality = chirality;
        }

        private void OnGrab(Hand hand)
        {
            if (_latchedAnchor != null && !_grabbingLastFrame)
            {
                _currentTeleportAnchorLocked = true;

                handRayRenderer.ignoreRayInteractor = true;

                _pullUI.gameObject.SetActive(true);
                _pullUI.transform.position = hand.PalmPosition;
                _transformWhenActivated.position = hand.PalmPosition;
                Vector3 forward = Vector3.ProjectOnPlane((hand.PalmPosition - Camera.main.transform.position).normalized, Vector3.up);
                _transformWhenActivated.rotation = Quaternion.LookRotation(forward);
            }
        }

        private void OnGrabbing(Hand hand)
        {
            if (!_currentTeleportAnchorLocked)
            {
                return;
            }

            UpdatePullUI(hand, out bool hasPulled);
            if (hasPulled)
            {
                ActivateTeleport();
            }
        }

        private void OnUngrab(Hand hand)
        {
            _pullUI.gameObject.SetActive(false);
            _currentTeleportAnchorLocked = false;
            handRayRenderer.ignoreRayInteractor = false;
        }

        private void UpdatePullUI(Hand hand, out bool hasPulled) 
        {
            Vector3 palmPositionRelativeToStart = _transformWhenActivated.InverseTransformPoint(hand.PalmPosition);
            Vector3 direction;
            float length;
            switch (rayLatchDirection)
            {
                case Direction.UP:
                    hasPulled = palmPositionRelativeToStart.y > _distanceToGrabAndPull;
                    direction = _transformWhenActivated.up;
                    length = palmPositionRelativeToStart.y;
                    break;
                default:
                case Direction.DOWN:
                    hasPulled = palmPositionRelativeToStart.y < -_distanceToGrabAndPull;
                    direction = -_transformWhenActivated.up;
                    length = -palmPositionRelativeToStart.y;
                    break;
            }

            _pullUI.SetProgress(length, _distanceToGrabAndPull, direction);
            var lookPos = Camera.main.transform.position - _pullUI.transform.position; lookPos.y = 0;
            _pullUI.transform.rotation = Quaternion.LookRotation(lookPos);
        }

        private void UpdateTeleportSelection(Hand activeHand)
        {
            bool isFacingObjectValue = _isFacingObject.ValueThisFrame;
            if (!IsSelected)
            {
                if (!isFacingObjectValue && handRayInteractor.handRay.HandRayEnabled)
                {
                    SelectTeleport(true);
                }
            }
            else
            {
                if (!_currentTeleportAnchorLocked && (isFacingObjectValue || !handRayInteractor.handRay.HandRayEnabled))
                {
                    SelectTeleport(false);
                }
            }
        }

        private void OnTeleportActivated(TeleportAnchor anchor, Vector3 position, Quaternion rotation)
        {
            _currentTeleportAnchorLocked = false;
            handRayRenderer.ignoreRayInteractor = false;

            _pullUI.gameObject.SetActive(false);
            _pullUI.SetProgress(0.0f, 1.0f, Vector3.up);
            _latchedAnchor = null;
        }

        private void MovementTypeChanged(TeleportActionMovementType movementType)
        {
            GameObject handRayRig;
            if(movementType == TeleportActionMovementType.FREE)
            {
                handRayRig = FreeHandRay;
                FixedHandRay.SetActive(false);
            }
            else
            {
                handRayRig = FixedHandRay;
                FreeHandRay.SetActive(false);
            }

            handRayRenderer = handRayRig.GetComponent<HandRayRenderer>();
            handRayInteractor = handRayRig.GetComponent<HandRayInteractor>();
        }
    }
}