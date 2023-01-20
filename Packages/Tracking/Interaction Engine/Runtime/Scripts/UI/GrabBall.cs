using UnityEngine;

namespace Leap.Unity.Interaction
{
    public class GrabBall : MonoBehaviour
    {
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
        public float lerpSpeed = 1.0f;

        private Vector3 _restrictedGrabBallPosition;

        private Pose _attachedObjectTargetPose = Pose.identity;
        private Transform _head;
        private Vector3 _attachedObjectOffset;
        private Transform _transformHelper;
        private bool _moveToPositionOnUngrasp = false;

        private float LERP_POSITION_LIMIT = 0.005f;
        private float LERP_ROTATION_LIMIT = 1;

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
                if (!_moveToPositionOnUngrasp)
                {
                    return;
                }

                UpdateAttachedObjectTargetPose();
                _moveToPositionOnUngrasp = IsAttachedObjectCloseToTargetPose();

                if (_moveToPositionOnUngrasp)
                {
                    grabBallInteractionBehaviour.transform.position = Vector3.Lerp(grabBallInteractionBehaviour.transform.position, _restrictedGrabBallPosition, Time.deltaTime * lerpSpeed);
                }
                return;
            }

            _moveToPositionOnUngrasp = true;
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
            Vector3 directionToGrabBall = (grabBallInteractionBehaviour.transform.position - _head.position).normalized;
            float cappedGrabBallDist = Vector3.Distance(grabBallInteractionBehaviour.transform.position, _head.position);

            if (cappedGrabBallDist >= maxHorizontalDistanceFromHead)
            {
                cappedGrabBallDist = maxHorizontalDistanceFromHead;
            }
            else if (cappedGrabBallDist <= minHorizontalDistanceFromHead)
            {
                cappedGrabBallDist = minHorizontalDistanceFromHead;
            }
            Vector3 cappedGrabBallPos = _head.position + directionToGrabBall * cappedGrabBallDist;

            if (cappedGrabBallPos.y >= _head.position.y + maxHeightFromHead) cappedGrabBallPos.y = _head.position.y + maxHeightFromHead;
            if (cappedGrabBallPos.y <= _head.position.y - minHeightFromHead) cappedGrabBallPos.y = _head.position.y - minHeightFromHead;
            _restrictedGrabBallPosition = cappedGrabBallPos;
        }

        private void UpdateAttachedObjectTargetPose()
        {
            RestrictGrabBallPosition();

            _transformHelper.position = _restrictedGrabBallPosition;
            _transformHelper.rotation = CalculateLookAtRotation(_transformHelper.position, _head.position);

            //Rotation - always face, or maintain initial rotation?

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
         * [] toggle attach object facing user
         * [] constant lerp speed ?
         * [] nice editor ui
         * [] visualise restriction
         */
    }
}