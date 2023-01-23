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

        /// <summary>
        /// What restrictions are currently being applied to the grab ball
        /// </summary>
        public GrabBallRestrictionStatus grabBallRestrictionStatus;

        /// <summary>
        /// The pose the grab ball is actually at, including restrictions
        /// </summary>
        [HideInInspector] public Pose grabBallPose;

        [Tooltip("The object controlled by the grab ball")]
        public Transform attachedObject;
        [Tooltip("The interaction behaviour attached to the grab ball. If left empty, will attempt to find an Interaction Behaviour on the current gameobject")]
        public InteractionBehaviour grabBallInteractionBehaviour;
        [Tooltip("How fast the attached object lerps to its new pose, and how fast the grab ball lerps back to its position on ungrasp when restricted")]
        public float lerpSpeed = 10f;

        [Tooltip("If enabled, the vertical tilt is populated from attached object's x rotation")]
        public bool useAttachedObjectsXRotation = true;

        [Tooltip("The vertical tilt of the attached object")]
        public float xRotation = 0;

        [Tooltip("Enable to restrict the distance the grab ball can move from the head. Useful to keep the attached object close enough to interact with")]
        public bool restrictGrabBallDistanceFromHead = true;
        [Tooltip("The furthest away from your head horizontally that the grab ball can move")]
        public float maxHorizontalDistanceFromHead = 0.5f;
        [Tooltip("The closest to your head horizontally that the grab ball can move")]
        public float minHorizontalDistanceFromHead = 0.1f;
        [Tooltip("The furthest away from your head vertically that the grab ball can move")]
        public float maxHeightFromHead = 0.3f;
        [Tooltip("The closest to your head vertically that the grab ball can move")]
        public float minHeightFromHead = 0.65f;
        public bool drawGrabBallRestrictionGizmos = true;

        private Pose _attachedObjectTargetPose = Pose.identity;
        private Transform _head;
        private Vector3 _attachedObjectOffset;
        private Transform _transformHelper;
        private bool _moveGrabBallToPositionOnUngrasp = false;

        private const float LERP_POSITION_LIMIT = 0.001f;
        private const float LERP_ROTATION_LIMIT = 1;


        private void OnEnable()
        {
            _attachedObjectOffset = InverseTransformPointUnscaled(grabBallInteractionBehaviour.transform, attachedObject.position);
            if (useAttachedObjectsXRotation) { xRotation = attachedObject.rotation.eulerAngles.x; }
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
            UpdateAttachedObjectTargetPose();
        }

        private void Update()
        {
            _moveGrabBallToPositionOnUngrasp = true;

            if (grabBallInteractionBehaviour.isGrasped)
            {
                UpdateAttachedObjectTargetPose();
                _moveGrabBallToPositionOnUngrasp = true;
            }
            else if (_moveGrabBallToPositionOnUngrasp)
            {
                _moveGrabBallToPositionOnUngrasp = Vector3.Distance(grabBallInteractionBehaviour.transform.position, grabBallPose.position) > LERP_POSITION_LIMIT;

                if (_moveGrabBallToPositionOnUngrasp)
                {
                    grabBallInteractionBehaviour.transform.position = Vector3.Lerp(grabBallInteractionBehaviour.transform.position, grabBallPose.position, Time.deltaTime * lerpSpeed);
                }
            }

            if (!IsAttachedObjectCloseToTargetPose())
            {
                UpdateAttachedObjectTargetPose();
                Pose attachedObjectPose = attachedObject.ToPose().Lerp(_attachedObjectTargetPose, Time.deltaTime * lerpSpeed);
                attachedObject.SetPose(attachedObjectPose);
            }
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

        /// <summary>
        /// Restricts the grab ball in a cylinder defined relative to the user's head position
        /// </summary>
        private void RestrictGrabBallPosition()
        {
            grabBallPose.position = grabBallInteractionBehaviour.transform.position;
            Vector3 headAtGrabBallHeight = _head.position;
            headAtGrabBallHeight.y = grabBallInteractionBehaviour.transform.position.y;

            Vector3 grabBallDirection = (grabBallInteractionBehaviour.transform.position - headAtGrabBallHeight).normalized;
            float clampedDistance = Vector3.Distance(headAtGrabBallHeight, grabBallInteractionBehaviour.transform.position);
            clampedDistance = Mathf.Clamp(clampedDistance, minHorizontalDistanceFromHead, maxHorizontalDistanceFromHead);

            grabBallRestrictionStatus.horizontalMax = clampedDistance >= maxHorizontalDistanceFromHead;

            if (grabBallRestrictionStatus.horizontalMax)
            {
                grabBallPose.position = headAtGrabBallHeight + grabBallDirection * maxHorizontalDistanceFromHead;
            }

            grabBallRestrictionStatus.horizontalMin = clampedDistance <= minHorizontalDistanceFromHead;
            if (grabBallRestrictionStatus.horizontalMin)
            {
                grabBallPose.position = headAtGrabBallHeight + grabBallDirection * minHorizontalDistanceFromHead;
            }

            float verticalDistanceFromHead = grabBallInteractionBehaviour.transform.position.y - _head.position.y;

            grabBallRestrictionStatus.heightMax = verticalDistanceFromHead >= maxHeightFromHead;
            if (grabBallRestrictionStatus.heightMax)
            {
                grabBallPose.position.y = _head.position.y + maxHeightFromHead;
            }

            grabBallRestrictionStatus.heightMin = verticalDistanceFromHead <= -minHeightFromHead;
            if (grabBallRestrictionStatus.heightMin)
            {
                grabBallPose.position.y = _head.position.y - minHeightFromHead;
            }
        }

        private void UpdateAttachedObjectTargetPose()
        {
            if (restrictGrabBallDistanceFromHead)
            {
                RestrictGrabBallPosition();
            } 
            else
            {
                grabBallPose.position = grabBallInteractionBehaviour.transform.position;
            }

            _transformHelper.position = grabBallPose.position;
            grabBallPose.rotation = LookAtRotationParallelToHorizon(grabBallPose.position, _head.position);
            _transformHelper.rotation = grabBallPose.rotation;

            _attachedObjectTargetPose.position = _transformHelper.TransformPoint(_attachedObjectOffset);
            Quaternion attachedObjectRotation = LookAtRotationParallelToHorizon(attachedObject.transform.position, _head.position);
            _attachedObjectTargetPose.rotation = Quaternion.Euler(xRotation, attachedObjectRotation.eulerAngles.y, attachedObjectRotation.eulerAngles.z);
        }
        private bool IsAttachedObjectCloseToTargetPose()
        {
            return Vector3.Distance(attachedObject.position, _attachedObjectTargetPose.position) < LERP_POSITION_LIMIT
                && Quaternion.Angle(attachedObject.rotation, _attachedObjectTargetPose.rotation) < LERP_ROTATION_LIMIT;
        }

        private Quaternion LookAtRotationParallelToHorizon(Vector3 posA, Vector3 posB)
        {
            return Quaternion.AngleAxis(Quaternion.LookRotation((posA - posB).normalized).eulerAngles.y, Vector3.up);
        }

        /// <summary>
        /// Transforms position from world space to local space, regardless of object scale
        /// </summary>
        private Vector3 InverseTransformPointUnscaled(Transform transform, Vector3 position)
        {
            var worldToLocalMatrix = Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one).inverse;
            return worldToLocalMatrix.MultiplyPoint3x4(position);
        }

        private void OnDrawGizmos()
        {
            if (_head == null)
            {
                _head = Camera.main.transform;
            }

            if (drawGrabBallRestrictionGizmos && restrictGrabBallDistanceFromHead)
            {
                Vector3 normal = Vector3.up;

                // Draw Horizontal Max Restriction
                Vector3 centerPosition = _head.position;
                centerPosition.y = Mathf.Clamp(grabBallInteractionBehaviour.transform.position.y, _head.position.y - minHeightFromHead, _head.position.y + maxHeightFromHead);
                Utils.DrawCircle(centerPosition, normal, maxHorizontalDistanceFromHead, grabBallRestrictionStatus.horizontalMax ? Color.green : Color.gray);

                //Draw Horizontal Min Restriction
                Utils.DrawCircle(centerPosition, normal, minHorizontalDistanceFromHead, grabBallRestrictionStatus.horizontalMin ? Color.green : Color.gray);

                //Draw Height Max Restriction
                centerPosition.y = _head.position.y + maxHeightFromHead;
                Utils.DrawCircle(centerPosition, normal, maxHorizontalDistanceFromHead, grabBallRestrictionStatus.heightMax ? Color.green : Color.gray);

                //Draw Height Min Restriction
                centerPosition.y = _head.position.y - minHeightFromHead;
                Utils.DrawCircle(centerPosition, normal, maxHorizontalDistanceFromHead, grabBallRestrictionStatus.heightMin ? Color.green : Color.gray);
            }
        }
    }
}