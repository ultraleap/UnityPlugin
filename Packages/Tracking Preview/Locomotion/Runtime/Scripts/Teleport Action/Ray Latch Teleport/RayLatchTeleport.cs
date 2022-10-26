using Leap;
using Leap.Unity;
using Leap.Unity.Preview.HandRays;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.Preview.Locomotion
{
    public class RayLatchTeleport : TeleportActionBase
    {
        private PullUI _pullUpUI = null;
        private TeleportAnchor _latchedAnchor = null;

        private bool _hasLockedIntoAnchor = false;
        private Transform _transformWhenActivated;
        [SerializeField, Range(0.01f, 0.5f)] private float _distToGrabAndPull = 0.1f;

        public enum Direction { UP, DOWN, LEFT, RIGHT, FORWARDS, BACKWARDS };
        public Direction rayLatchDirection = Direction.UP;
        public LeapProvider leapProvider;

        public LightweightGrabDetector grabDetector;

        [Header("The chirality which will be used for pinch to teleport. This will update the chirality in the pinch detector and hand ray.")]
        public Chirality chirality;

        public override void Start()
        {
            base.Start();

            _pullUpUI = GetComponentInChildren<PullUI>();
            _pullUpUI.gameObject.SetActive(false);

            _transformWhenActivated = new GameObject("RayLatchTransformHelper").transform;
            _transformWhenActivated.transform.parent = transform;
            OnTeleport += OnTeleporterActivated;
        }

        private void OnEnable()
        {
            grabDetector.OnGrab += OnGrab;
            grabDetector.OnGrabbing += OnGrabbing;
            grabDetector.OnUngrab += OnUngrab;

            UpdateChirality();
        }

        private void OnDisable()
        {
            grabDetector.OnGrab -= OnGrab;
            grabDetector.OnGrabbing -= OnGrabbing;
            grabDetector.OnUngrab -= OnUngrab;
        }

        private void UpdateChirality()
        {
            handRayInteractor.handRay.chirality = chirality;
            grabDetector.chirality = chirality;
        }

        void OnGrab(Hand hand)
        {

        }

        void OnGrabbing(Hand hand)
        {

        }

        void OnUngrab(Hand hand)
        {

        }

        void Update()
        {
            SelectTeleport();
            Hand hand = leapProvider.CurrentFrame.GetHand(handRayInteractor.handRay.chirality);

            //If we aren't locked to a teleport point...
            if (!_hasLockedIntoAnchor)
            {
                //Work out if we should latch onto a teleport point
                if (IsValid)
                {
                    if (_currentPoint != _latchedAnchor)
                    {
                        Unlatch(_latchedAnchor);
                        Latch(_currentPoint);
                    }
                }
                else
                {
                    Unlatch(_latchedAnchor);
                }

                //If we have latched onto one, wait for the user to grab - this locks us into the point
                if (_latchedAnchor != null && hand != null && hand.GrabStrength >= 1.0f)
                {
                    _hasLockedIntoAnchor = true;

                    _pullUpUI.gameObject.SetActive(true);
                    _pullUpUI.transform.position = hand.PalmPosition;
                    _transformWhenActivated.position = hand.PalmPosition;
                    _transformWhenActivated.rotation = Quaternion.LookRotation((hand.PalmPosition - Camera.main.transform.position).normalized.ProjectOnPlane(Vector3.up));
                    
                }
            }

            else
            {
                //Pull in to a teleport by grabbing and moving in a direction
                if (hand != null && hand.GrabStrength >= 0.95f)
                {
                    Vector3 direction = Vector3.up;
                    bool hasPulled = false;
                    Vector3 palmPositionRelativeToStart = _transformWhenActivated.InverseTransformPoint(hand.PalmPosition);
                    float length = 0;
                    switch (rayLatchDirection)
                    {
                        case Direction.UP:
                            hasPulled = palmPositionRelativeToStart.y > _distToGrabAndPull;
                            direction = _transformWhenActivated.up;
                            length = palmPositionRelativeToStart.y;
                            break;
                        case Direction.DOWN:
                            hasPulled = palmPositionRelativeToStart.y < -_distToGrabAndPull;
                            direction = -_transformWhenActivated.up;
                            length = -palmPositionRelativeToStart.y;
                            break;
                        case Direction.LEFT:
                            hasPulled = palmPositionRelativeToStart.x < -_distToGrabAndPull;
                            direction = -_transformWhenActivated.right;
                            length = -palmPositionRelativeToStart.x;
                            break;
                        case Direction.RIGHT:
                            hasPulled = palmPositionRelativeToStart.x > _distToGrabAndPull;
                            direction = _transformWhenActivated.right;
                            length = palmPositionRelativeToStart.x;
                            break;
                        case Direction.FORWARDS:
                            hasPulled = palmPositionRelativeToStart.z > _distToGrabAndPull;
                            direction = _transformWhenActivated.forward;
                            length = palmPositionRelativeToStart.z;
                            break;
                        case Direction.BACKWARDS:
                            hasPulled = palmPositionRelativeToStart.z < -_distToGrabAndPull;
                            direction = -_transformWhenActivated.forward;
                            length = -palmPositionRelativeToStart.z;
                            break;
                    }

                    _pullUpUI.SetLength(length, _distToGrabAndPull, direction);
                    var lookPos = Camera.main.transform.position - _pullUpUI.transform.position; lookPos.y = 0;
                    _pullUpUI.transform.rotation = Quaternion.LookRotation(lookPos);

                    if (hasPulled)
                    {
                        ActivateTeleport();
                    }
                }
                else
                {
                    _pullUpUI.gameObject.SetActive(false);
                    _hasLockedIntoAnchor = false;
                }

            }

            //Draw ray to point if we are latched or locked
            if (_latchedAnchor != null && hand != null)
            {
                handRayRenderer.lineRenderer.positionCount = 2;
                handRayRenderer.lineRenderer.SetPosition(0, hand.PalmPosition);
                handRayRenderer.lineRenderer.SetPosition(1, _latchedAnchor.transform.position);
                handRayRenderer.SetActive(true);
            }
        }

        /* Latch to a teleport point */
        private void Latch(TeleportAnchor point)
        {
            if (point == null) return;
            _latchedAnchor = point;
        }

        /* Unlatch from a teleport point */
        private void Unlatch(TeleportAnchor point)
        {
            if (point == null) return;
            _latchedAnchor = null;
            Debug.Log("Unlatch");
        }

        /* When teleporter is used, unlatch from it and remember it */
        private void OnTeleporterActivated(TeleportAnchor anchor, Vector3 position, Quaternion rotation)
        {
            Debug.Log("OnTeleporterActivated: " + anchor.name);
            _hasLockedIntoAnchor = false;
            _pullUpUI.gameObject.SetActive(false);
            _pullUpUI.SetLength(0.0f, 1.0f, Vector3.up);
            Unlatch(anchor);
        }
    }
    #region Extensions

    public static class RayLatchVector3Extensions
    {
        public static Vector3 ProjectOnPlane(this Vector3 v, Vector3 n)
        {
            return Vector3.ProjectOnPlane(v, n);
        }
    }

    #endregion
}