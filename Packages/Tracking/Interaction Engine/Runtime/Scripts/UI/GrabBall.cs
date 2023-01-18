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

        public float maxHorizontalDistanceFromHead = 0.5f;
        public float minHorizontalDistanceFromHead = 0.1f;
        public float maxHeightFromHead = 0.3f;
        public float minHeightFromHead = 0.65f;

        private Vector3 _restrictedGrabBallPosition;

        private Pose _targetPose = Pose.identity;
        private Transform _head;
        private Vector3 _grabBallOffset;
        private InteractionBehaviour _interactionBehaviour;
        private Transform _transformHelper;

        private void OnEnable()
        {
            _grabBallOffset = InverseTransformPointUnscaled(transform, attachedObject.position);
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
            _interactionBehaviour = GetComponent<InteractionBehaviour>();

            _transformHelper = new GameObject("GrabBall_TransformHelper").transform;
            _transformHelper.SetParent(transform);

            RestrictGrabBallPosition();
            UpdateTargetPose();
        }

        public float lerpSpeed = 1.0f;
        private void Update()
        {
            if (_interactionBehaviour.isGrasped)
            {
                RestrictGrabBallPosition();
                UpdateTargetPose();
            }
            else if (Vector3.Distance(transform.position, _restrictedGrabBallPosition) > 0.001f)
            {
                transform.position = Vector3.Lerp(transform.position, _restrictedGrabBallPosition, Time.deltaTime * lerpSpeed);
            }

            Pose attachedObjectPose = attachedObject.ToPose().Lerp(_targetPose, Time.deltaTime * lerpSpeed);
            attachedObject.SetPose(attachedObjectPose);
        }

        private void RestrictGrabBallPosition()
        {
            Vector3 directionToGrabBall = (transform.position - _head.position).normalized;

            float cappedGrabBallDist = Vector3.Distance(transform.position, _head.position);

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
            _restrictedGrabBallPosition = Vector3.Lerp(transform.position, cappedGrabBallPos, Time.deltaTime * lerpSpeed * 10);
        }

        private void UpdateTargetPose()
        {
            _transformHelper.position = _restrictedGrabBallPosition;
            _transformHelper.rotation = CalculateLookAtRotation(_transformHelper.position, _head.position);

            //Rotation - always face, or maintain initial rotation?

            _targetPose.position = _transformHelper.TransformPoint(_grabBallOffset);
            _targetPose.rotation = CalculateLookAtRotation(attachedObject.transform.position, _head.position, xRotation);
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

        private Vector3 InverseTransformPointUnscaled(Transform transform, Vector3 position)
        {
            var worldToLocalMatrix = Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one).inverse;
            return worldToLocalMatrix.MultiplyPoint3x4(position);
        }

        /*
         * TODO:
         * [x] restrict grab ball position
         * [x] lerp grab ball back to home position on ungrab
         * [] visualise restriction
         * [] make this independent of interaction behaviour
         * [] add public way to change offset of attached object
         * [] constant lerp speed
         * [] nice editor ui
         */

        // Constant Lerp from A to B with speed
        private float moveDir;
        private void ConstantLerp(Transform t, Vector3 from, Vector3 to, float speed)
        {
            moveDir = speed * (1 / Vector3.Distance(from, to) * Time.deltaTime);
            t.position = Vector3.Lerp(from, to, moveDir);
        }
    }
}