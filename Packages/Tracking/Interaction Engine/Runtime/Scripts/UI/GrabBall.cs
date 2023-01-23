using UnityEngine;

namespace Leap.Unity.Interaction
{
    public class GrabBall : MonoBehaviour
    {
        public struct GrabBallRestrictionStatus
        {
            public bool horizontalMin;
            public bool horizontalMax;
            public bool heightMin;
            public bool heightMax;
            public bool IsRestricted { get { return (horizontalMin || horizontalMax || heightMin || heightMax); } }
        }

        public GrabBallRestrictionStatus grabBallRestrictionStatus;

        public Transform attachedObject;

        [Tooltip("If enabled, the vertical tilt is populated from attached object's x rotation")]
        public bool useObjectsXRotation = true;

        [Tooltip("The vertical tilt of the attached object")]
        public float xRotation = 0;
        [Tooltip("The interaction behaviour attached to the grab ball. If left empty, will attempt to find an Interaction Behaviour on the current gameobject.")]
        public InteractionBehaviour grabBallInteractionBehaviour;

        public float maxHorizontalDistanceFromHead = 0.5f;
        public float minHorizontalDistanceFromHead = 0.1f;
        public float maxHeightFromHead = 0.3f;
        public float minHeightFromHead = 0.65f;
        public float lerpSpeed = 10f;

        private Vector3 _restrictedGrabBallPosition;
        private Pose _attachedObjectTargetPose = Pose.identity;
        private Transform _head;
        private Vector3 _attachedObjectOffset;
        private Transform _transformHelper;
        private bool _moveGrabBallToPositionOnUngrasp = false;

        private const float LERP_POSITION_LIMIT = 0.005f;
        private const float LERP_ROTATION_LIMIT = 1;

        private void OnEnable()
        {
            _attachedObjectOffset = InverseTransformPointUnscaled(grabBallInteractionBehaviour.transform, attachedObject.position);
            if (useObjectsXRotation) { xRotation = attachedObject.rotation.eulerAngles.x; }
        }

        private void Start()
        {
            if (Physics.autoSyncTransforms)
            {
                Debug.LogWarning(
                    "Physics.autoSyncTransforms is enabled. This will cause Interaction "
                + "Buttons and similar elements to 'wobble' when this script is used to "
                + "move a parent transform. You can modify this setting in "
                + "Edit->Project Settings->Physics.");
            }
            _head = Camera.main.transform;
            if (grabBallInteractionBehaviour == null)
            {
                grabBallInteractionBehaviour = GetComponent<InteractionBehaviour>();
                if (grabBallInteractionBehaviour == null)
                {
                    Debug.LogWarning("No interaction behaviour found for grab ball", gameObject);
                }
            }

            _transformHelper = new GameObject("GrabBall_TransformHelper").transform;
            _transformHelper.SetParent(transform);

            RestrictGrabBallPosition();
            UpdateAttachedObjectTargetPose();
        }

        private void Update()
        {
            if (!grabBallInteractionBehaviour.isGrasped)
            {
                if (!_moveGrabBallToPositionOnUngrasp)
                {
                    return;
                }

                UpdateAttachedObjectTargetPose();
                _moveGrabBallToPositionOnUngrasp = Vector3.Distance(grabBallInteractionBehaviour.transform.position, _restrictedGrabBallPosition) > LERP_POSITION_LIMIT;

                if (_moveGrabBallToPositionOnUngrasp)
                {
                    grabBallInteractionBehaviour.transform.position = Vector3.Lerp(grabBallInteractionBehaviour.transform.position, _restrictedGrabBallPosition, Time.deltaTime * lerpSpeed);
                }
                return;
            }

            _moveGrabBallToPositionOnUngrasp = true;
            UpdateAttachedObjectTargetPose();

            if (IsAttachedObjectCloseToTargetPose())
            {
                return;
            }

            Pose attachedObjectPose = attachedObject.ToPose().Lerp(_attachedObjectTargetPose, Time.deltaTime * lerpSpeed);
            attachedObject.SetPose(attachedObjectPose);
        }

        private bool IsAttachedObjectCloseToTargetPose()
        {
            return Vector3.Distance(attachedObject.position, _attachedObjectTargetPose.position) < LERP_POSITION_LIMIT
                && Quaternion.Angle(attachedObject.rotation, _attachedObjectTargetPose.rotation) < LERP_ROTATION_LIMIT;
        }

        private void RestrictGrabBallPosition()
        {
            _restrictedGrabBallPosition = grabBallInteractionBehaviour.transform.position;
            Vector3 headAtGrabBallHeight = _head.position;
            headAtGrabBallHeight.y = grabBallInteractionBehaviour.transform.position.y;

            Vector3 grabBallDirection = (grabBallInteractionBehaviour.transform.position - headAtGrabBallHeight).normalized;
            float clampedDistance = Vector3.Distance(headAtGrabBallHeight, grabBallInteractionBehaviour.transform.position);
            clampedDistance = Mathf.Clamp(clampedDistance, minHorizontalDistanceFromHead, maxHorizontalDistanceFromHead);

            grabBallRestrictionStatus.horizontalMax = clampedDistance >= maxHorizontalDistanceFromHead;

            if (grabBallRestrictionStatus.horizontalMax)
            {
                _restrictedGrabBallPosition = headAtGrabBallHeight + grabBallDirection * maxHorizontalDistanceFromHead;
            }
            
            grabBallRestrictionStatus.horizontalMin = clampedDistance <= minHorizontalDistanceFromHead;
            if (grabBallRestrictionStatus.horizontalMin)
            {
                _restrictedGrabBallPosition = headAtGrabBallHeight + grabBallDirection * minHorizontalDistanceFromHead;
            }

            float verticalDistanceFromHead = grabBallInteractionBehaviour.transform.position.y - _head.position.y;

            grabBallRestrictionStatus.heightMax = verticalDistanceFromHead >= maxHeightFromHead;
            if (grabBallRestrictionStatus.heightMax)
            {
                _restrictedGrabBallPosition.y = _head.position.y + maxHeightFromHead;
            }

            grabBallRestrictionStatus.heightMin = verticalDistanceFromHead <= -minHeightFromHead;
            if (grabBallRestrictionStatus.heightMin)
            {
                _restrictedGrabBallPosition.y = _head.position.y - minHeightFromHead;
            }
        }

        private void UpdateAttachedObjectTargetPose()
        {
            RestrictGrabBallPosition();

            _transformHelper.position = _restrictedGrabBallPosition;
            _transformHelper.rotation = CalculateLookAtRotation(_transformHelper.position, _head.position);

            _attachedObjectTargetPose.position = _transformHelper.TransformPoint(_attachedObjectOffset);
            _attachedObjectTargetPose.rotation = CalculateLookAtRotation(attachedObject.transform.position, _head.position, xRotation);
        }

        private Quaternion CalculateLookAtRotation(Vector3 posA, Vector3 posB)
        {
            return Quaternion.AngleAxis(Quaternion.LookRotation((posA - posB).normalized).eulerAngles.y, Vector3.up);
        }

        private Quaternion CalculateLookAtRotation(Vector3 posA, Vector3 posB, float xRotation)
        {
            Quaternion lookAtRotation = CalculateLookAtRotation(posA, posB);
            return Quaternion.Euler(xRotation, lookAtRotation.eulerAngles.y, lookAtRotation.eulerAngles.z);
        }

        /// <summary>
        /// Transforms position from world space to local space, regardless of object scale
        /// </summary>
        private Vector3 InverseTransformPointUnscaled(Transform transform, Vector3 position)
        {
            var worldToLocalMatrix = Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one).inverse;
            return worldToLocalMatrix.MultiplyPoint3x4(position);
        }

        /// <summary>
        /// Sets the Grab Ball's offset, relative to its Attached Object, moving the Grab Ball
        /// The new offset is treated as a position in the Attached Object's local space.
        /// </summary>
        public void SetGrabBallOffset(Vector3 newOffset)
        {
            _transformHelper.position = grabBallInteractionBehaviour.transform.position;
            _transformHelper.transform.position = attachedObject.TransformPoint(newOffset);
            _transformHelper.rotation = attachedObject.rotation;
            _attachedObjectOffset = InverseTransformPointUnscaled(_transformHelper.transform, attachedObject.position);
            grabBallInteractionBehaviour.transform.position = _transformHelper.position;
        }

        /// <summary>
        /// Sets the Attached Object's offset, relative its Grab Ball, moving the Attached Object
        /// The new offset is treated as a position in the Grab Ball's local space.
        /// </summary>
        public void SetAttachedObjectOffset(Vector3 newOffset)
        {
            _attachedObjectOffset = newOffset;
            UpdateAttachedObjectTargetPose();
            attachedObject.SetPose(_attachedObjectTargetPose);
        }

        /*
         * TODO:
         * [x] restrict grab ball position
         * [x] lerp grab ball back to home position on ungrab
         * [x] fix bumps on edge of restriction
         * [x] make this independent of interaction behaviour
         * [x] add public way to change offset of attached object
         * [x] visualise restriction
         * [] ghosted grab ball
         * [] grab ball grow/shrink based on primary hover
         * [] nice editor ui
         */
    }
}