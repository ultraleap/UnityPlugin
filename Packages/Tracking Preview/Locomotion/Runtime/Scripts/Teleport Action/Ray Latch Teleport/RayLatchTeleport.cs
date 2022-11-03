using UnityEngine;

namespace Leap.Unity.Preview.Locomotion
{
    public class RayLatchTeleport : TeleportActionBase
    {
        private PullUI _pullUI = null;
        private TeleportAnchor _latchedAnchor = null;

        private bool _latched = false;
        private Transform _transformWhenActivated;
        [SerializeField, Range(0.01f, 0.5f)] private float _distToGrabAndPull = 0.1f;

        public enum Direction { UP, DOWN };
        public Direction rayLatchDirection = Direction.UP;
        public LeapProvider leapProvider;

        public LightweightGrabDetector grabDetector;

        [Tooltip("The chirality which will be used for pinch to teleport. This will update the chirality in the pinch detector and hand ray.")]
        public Chirality chirality;
        private Chirality _chiralityLastFrame;

        [SerializeField]
        private IsFacingObject _isFacingObject;

        private bool _grabbingLastFrame = false;

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

        public override void Start()
        {
            base.Start();

            _pullUI = GetComponentInChildren<PullUI>();
            _pullUI.gameObject.SetActive(false);

            _transformWhenActivated = new GameObject("RayLatchTransformHelper").transform;
            _transformWhenActivated.transform.parent = transform;
            OnTeleport += OnTeleportActivated;
            _chiralityLastFrame = chirality;
        }

        void Update()
        {
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

            UpdateTeleportSelection(activeHand);

            if (!_latched)
            {
                if (IsValid)
                {
                    if (_currentPoint != _latchedAnchor)
                    {
                        _latchedAnchor = _currentPoint;
                    }
                }
                else
                {
                    _latchedAnchor = null;
                }
            }

            if (_latchedAnchor != null && activeHand != null)
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

        void OnGrab(Hand hand)
        {
            if (_latchedAnchor != null && !_grabbingLastFrame)
            {
                _latched = true;

                _pullUI.gameObject.SetActive(true);
                _pullUI.transform.position = hand.PalmPosition;
                _transformWhenActivated.position = hand.PalmPosition;
                _transformWhenActivated.rotation = Quaternion.LookRotation((hand.PalmPosition - Camera.main.transform.position).normalized.ProjectOnPlane(Vector3.up));
            }
        }

        void OnGrabbing(Hand hand)
        {
            if (!_latched)
            {
                return;
            }

            UpdatePullUI(hand, out bool hasPulled);
            if (hasPulled)
            {
                ActivateTeleport();
            }
        }

        void OnUngrab(Hand hand)
        {
            _pullUI.gameObject.SetActive(false);
            _latched = false;
        }

        

        private void UpdatePullUI(Hand hand, out bool hasPulled) 
        {
            Vector3 palmPositionRelativeToStart = _transformWhenActivated.InverseTransformPoint(hand.PalmPosition);
            Vector3 direction;
            float length;
            switch (rayLatchDirection)
            {
                case Direction.UP:
                    hasPulled = palmPositionRelativeToStart.y > _distToGrabAndPull;
                    direction = _transformWhenActivated.up;
                    length = palmPositionRelativeToStart.y;
                    break;
                default:
                case Direction.DOWN:
                    hasPulled = palmPositionRelativeToStart.y < -_distToGrabAndPull;
                    direction = -_transformWhenActivated.up;
                    length = -palmPositionRelativeToStart.y;
                    break;
            }

            _pullUI.SetLength(length, _distToGrabAndPull, direction);
            var lookPos = Camera.main.transform.position - _pullUI.transform.position; lookPos.y = 0;
            _pullUI.transform.rotation = Quaternion.LookRotation(lookPos);
        }

        private void UpdateTeleportSelection(Hand activeHand)
        {
            bool isFacingObjectValue = _isFacingObject.ValueThisFrame;
            if (!IsSelected)
            {
                if (!isFacingObjectValue && handRayInteractor.handRay.HandRayEnabled )
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


        /* When teleporter is used, unlatch from it and remember it */
        private void OnTeleportActivated(TeleportAnchor anchor, Vector3 position, Quaternion rotation)
        {
            _latched = false;
            _pullUI.gameObject.SetActive(false);
            _pullUI.SetLength(0.0f, 1.0f, Vector3.up);
            _latchedAnchor = null;
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