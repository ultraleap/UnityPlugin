/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2023.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using UnityEngine;

namespace Leap.Unity.Interaction
{
    /// <summary>
    /// A representation of an object with an attached object which can be restricted relative to the users head
    /// 
    /// Most useful for 3D UI panels to ensure they are reachable
    /// </summary>
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

        [Tooltip("The interaction behaviour attached to the grab ball. If left empty, will attempt to find an Interaction Behaviour on the current gameobject")]
        public InteractionBehaviour grabBallInteractionBehaviour;

        [Tooltip("The object controlled by the grab ball")]
        public Transform attachedObject;
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
        [Tooltip("The highest point from your head vertically that the grab ball can move. This value will be positive in most cases")]
        public float maxHeightFromHead = 0.3f;
        [Tooltip("The lowest point from your head vertically that the grab ball can move. This value will be negative in most cases")]
        public float minHeightFromHead = -0.65f;
        public bool drawGrabBallRestrictionGizmos = true;

        public bool continuouslyRestrictGrabBallDistanceFromHead = true;

        private Pose _attachedObjectTargetPose = Pose.identity;
        private Transform _head;
        private Vector3 _attachedObjectOffset;
        private Transform _transformHelper;
        private bool _moveGrabBallToPositionOnUngrasp = false;

        private const float LERP_POSITION_LIMIT = 0.001f;
        private const float LERP_ROTATION_LIMIT = 1;

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

            _attachedObjectOffset = InverseTransformPointUnscaled(grabBallInteractionBehaviour.transform, attachedObject.position);

            if (useAttachedObjectsXRotation)
            {
                xRotation = attachedObject.rotation.eulerAngles.x;
            }

            _transformHelper = new GameObject("GrabBall_TransformHelper").transform;
            _transformHelper.SetParent(transform);
            UpdateAttachedObjectTargetPose(false);
        }

        private void Update()
        {
            if (grabBallInteractionBehaviour.isGrasped)
            {
                UpdateAttachedObjectTargetPose();
                _moveGrabBallToPositionOnUngrasp = true;
            }
            else if (_moveGrabBallToPositionOnUngrasp)
            {
                grabBallInteractionBehaviour.transform.position = Vector3.Lerp(grabBallInteractionBehaviour.transform.position, grabBallPose.position, Time.deltaTime * lerpSpeed);

                if (Vector3.Distance(grabBallInteractionBehaviour.transform.position, grabBallPose.position) < LERP_POSITION_LIMIT)
                {
                    _moveGrabBallToPositionOnUngrasp = false;
                }
            }
            else if (continuouslyRestrictGrabBallDistanceFromHead)
            {
                ConstrainGrabBallToArea();
            }

            if (IsAttachedObjectCloseToTargetPose())
            {
                attachedObject.SetPose(_attachedObjectTargetPose);
            }
            else
            {
                UpdateAttachedObjectTargetPose();
                Pose attachedObjectPose = attachedObject.ToPose().Lerp(_attachedObjectTargetPose, Time.deltaTime * lerpSpeed);
                attachedObject.SetPose(attachedObjectPose);
            }
        }

        void ConstrainGrabBallToArea()
        {
            grabBallInteractionBehaviour.transform.position = Vector3.Lerp(grabBallInteractionBehaviour.transform.position, grabBallPose.position, Time.deltaTime * lerpSpeed);
            UpdateAttachedObjectTargetPose(false);
        }

        /// <summary>
        /// Sets the Grab Ball's offset, relative to its Attached Object, moving the Grab Ball
        /// The new offset is treated as a position in the Attached Object's local space.
        /// </summary>
        public void SetGrabBallOffset(Vector3 newOffset)
        {
            _transformHelper.position = attachedObject.TransformPoint(newOffset);
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
            // Horizontal restriction
            Vector3 grabBallHorizontalDiff = grabBallInteractionBehaviour.transform.position - _head.position;
            grabBallHorizontalDiff.y = 0;

            float horizontalDistance = grabBallHorizontalDiff.magnitude;

            if (horizontalDistance > maxHorizontalDistanceFromHead)
            {
                // clamp distance to max
                grabBallPose.position = _head.position + (grabBallHorizontalDiff.normalized * maxHorizontalDistanceFromHead);
                grabBallRestrictionStatus.horizontalMax = true;
                grabBallRestrictionStatus.horizontalMin = false;
            }
            else if (horizontalDistance < minHorizontalDistanceFromHead)
            {
                // clamp distance to min
                grabBallPose.position = _head.position + (grabBallHorizontalDiff.normalized * minHorizontalDistanceFromHead);
                grabBallRestrictionStatus.horizontalMin = true;
                grabBallRestrictionStatus.horizontalMax = false;
            }
            else
            {
                // no restriction necessary
                grabBallPose.position = grabBallInteractionBehaviour.transform.position;
                grabBallRestrictionStatus.horizontalMax = false;
                grabBallRestrictionStatus.horizontalMin = false;
            }

            // Y restriction
            float yDiff = grabBallInteractionBehaviour.transform.position.y - _head.position.y;

            if (yDiff > maxHeightFromHead)
            {
                // clamp height to max
                grabBallPose.position.y = _head.position.y + maxHeightFromHead;
                grabBallRestrictionStatus.heightMax = true;
                grabBallRestrictionStatus.heightMin = false;
            }
            else if (yDiff < minHeightFromHead)
            {
                // clamp height to min
                grabBallPose.position.y = _head.position.y + minHeightFromHead;
                grabBallRestrictionStatus.heightMin = true;
                grabBallRestrictionStatus.heightMax = false;
            }
            else
            {
                // no restriction necessary
                grabBallPose.position.y = grabBallInteractionBehaviour.transform.position.y;
                grabBallRestrictionStatus.heightMax = false;
                grabBallRestrictionStatus.heightMin = false;
            }
        }

        private void UpdateAttachedObjectTargetPose(bool setRotation = true)
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

            if (setRotation)
            {
                grabBallPose.rotation = LookAtRotationParallelToHorizon(grabBallPose.position, _head.position);
                _transformHelper.rotation = grabBallPose.rotation;
            }

            _attachedObjectTargetPose.position = _transformHelper.TransformPoint(_attachedObjectOffset);

            if (setRotation)
            {
                _attachedObjectTargetPose.rotation = Quaternion.Euler(xRotation, grabBallPose.rotation.eulerAngles.y, grabBallPose.rotation.eulerAngles.z);
            }
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
                centerPosition.y = Mathf.Clamp(grabBallInteractionBehaviour.transform.position.y, _head.position.y + minHeightFromHead, _head.position.y + maxHeightFromHead);
                Leap.Unity.Utils.DrawCircle(centerPosition, normal, maxHorizontalDistanceFromHead, grabBallRestrictionStatus.horizontalMax ? Color.green : Color.gray);

                //Draw Horizontal Min Restriction
                Leap.Unity.Utils.DrawCircle(centerPosition, normal, minHorizontalDistanceFromHead, grabBallRestrictionStatus.horizontalMin ? Color.green : Color.gray);

                //Draw Height Max Restriction
                centerPosition.y = _head.position.y + maxHeightFromHead;
                Leap.Unity.Utils.DrawCircle(centerPosition, normal, maxHorizontalDistanceFromHead, grabBallRestrictionStatus.heightMax ? Color.green : Color.gray);

                //Draw Height Min Restriction
                centerPosition.y = _head.position.y + minHeightFromHead;
                Leap.Unity.Utils.DrawCircle(centerPosition, normal, maxHorizontalDistanceFromHead, grabBallRestrictionStatus.heightMin ? Color.green : Color.gray);
            }
        }
    }
}