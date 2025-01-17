/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2024.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using Leap.PhysicalHands;
using UnityEngine;

namespace Leap.Examples
{
    /// <summary>
    /// This example script constructs behavior for a UI object that can open
    /// a "workstation" panel when placed outside of an anchor.
    /// The panel is closed when the object is moved at or over a given speed.
    /// The anchorable object is set to kinematic when in workstation mode.
    /// </summary>
    public class WorkstationBehaviourExample : MonoBehaviour, IPhysicalHandGrab
    {
        /// <summary>
        /// If the rigidbody of this object moves faster than this speed and the object
        /// is in workstation mode, it will exit workstation mode.
        /// </summary>
        public const float MAX_SPEED_AS_WORKSTATION = 0.005F;

        /// <summary>
        /// The gameobject that should be set to active when the workstation is open
        /// </summary>
        public GameObject workstation;

        private AnchorableBehaviour _anchObj;

        private bool _wasKinematicBeforeActivation = false;

        public enum WorkstationState { Closed, Open }
        public WorkstationState workstationState;

        private bool _grabbed;
        private Rigidbody _rigidbody;


        void OnValidate()
        {
            refreshRequiredComponents();
        }

        void Start()
        {
            _rigidbody = GetComponent<Rigidbody>();

            refreshRequiredComponents();

            if (!_anchObj.TryAnchorNearestOnGraspEnd)
            {
                Debug.LogWarning("WorkstationBehaviour expects its AnchorableBehaviour's tryAnchorNearestOnGraspEnd property to be enabled.", this.gameObject);
            }
        }

        void OnDestroy()
        {
            _anchObj.OnPostTryAnchorOnGraspEnd -= onPostObjectGraspEnd;
        }

        public void ActivateWorkstation()
        {
            if (workstationState != WorkstationState.Open)
            {
                _wasKinematicBeforeActivation = _rigidbody.isKinematic;
                _rigidbody.isKinematic = true;
            }

            workstation.SetActive(true);
            workstationState = WorkstationState.Open;
        }

        public void DeactivateWorkstation()
        {
            _rigidbody.isKinematic = _wasKinematicBeforeActivation;

            workstation.SetActive(false);
            workstationState = WorkstationState.Closed;
        }

        private void refreshRequiredComponents()
        {
            _anchObj = GetComponent<AnchorableBehaviour>();

            _anchObj.OnPostTryAnchorOnGraspEnd -= onPostObjectGraspEnd;
            _anchObj.OnPostTryAnchorOnGraspEnd += onPostObjectGraspEnd;
        }

        private void FixedUpdate()
        {
            if (_grabbed)
            {
                onGraspedMovement();
            }
        }

        private void onGraspedMovement()
        {
            // If the velocity of the object while grasped is too large, exit workstation mode.
            if (workstationState == WorkstationState.Open
#if UNITY_6000_0_OR_NEWER
                && _rigidbody.linearVelocity.magnitude > MAX_SPEED_AS_WORKSTATION)
#else
                && _rigidbody.velocity.magnitude > MAX_SPEED_AS_WORKSTATION)
#endif
            {
                DeactivateWorkstation();
            }
        }

        private void onPostObjectGraspEnd()
        {
            if (_anchObj.FindPreferredAnchor() == null && !_anchObj.isAttached)
            {
                // Choose a good position and rotation for workstation mode
                Vector3 targetPosition = _rigidbody.position;
                Quaternion targetRotation = determineWorkstationRotation(targetPosition);

                workstation.transform.SetPositionAndRotation(targetPosition, targetRotation);

                ActivateWorkstation();
            }
            else
            {
                // Ensure the workstation is not active or being deactivated if
                // we are attaching to an anchor.
                DeactivateWorkstation();
            }
        }

        private Quaternion determineWorkstationRotation(Vector3 workstationPosition)
        {
            Vector3 toCamera = Camera.main.transform.position - workstationPosition;
            toCamera.y = 0F;
            Quaternion placementRotation = Quaternion.LookRotation(toCamera.normalized, Vector3.up);

            return placementRotation;
        }

        void IPhysicalHandGrab.OnHandGrab(ContactHand hand)
        {
            _grabbed = true;
        }

        void IPhysicalHandGrab.OnHandGrabExit(ContactHand hand)
        {
            _grabbed = false;
            onPostObjectGraspEnd();
        }
    }
}
