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

            UpdateTargetPose();
        }

        public float lerpSpeed = 1.0f;
        private void Update()
        {
            if (_interactionBehaviour.isGrasped)
            {
                UpdateTargetPose();
            }

            Pose attachedObjectPose = attachedObject.ToPose().Lerp(_targetPose, Time.deltaTime * lerpSpeed);
            attachedObject.SetPose(attachedObjectPose);
        }

        private void UpdateTargetPose()
        {

            _transformHelper.position = transform.position;
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
    }
}