/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2023.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using Leap.Unity.Interaction;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.InteractionEngine.Examples
{

    /// <summary>
    /// This example script constructs behavior for a UI object that can open
    /// a "workstation" panel when placed outside of an anchor.
    /// The panel is closed when the object is moved at or over a given speed.
    /// The anchorable object is set to kinematic when in workstation mode.
    /// </summary>
    [RequireComponent(typeof(InteractionBehaviour))]
    [RequireComponent(typeof(AnchorableBehaviour))]
    public class WorkstationBehaviourExample : MonoBehaviour
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

        private InteractionBehaviour _intObj;
        private AnchorableBehaviour _anchObj;

        private bool _wasKinematicBeforeActivation = false;


        public enum WorkstationState { Closed, Open }
        public WorkstationState workstationState;


        void OnValidate()
        {
            refreshRequiredComponents();
        }

        void Start()
        {
            refreshRequiredComponents();

            if (!_anchObj.tryAnchorNearestOnGraspEnd)
            {
                Debug.LogWarning("WorkstationBehaviour expects its AnchorableBehaviour's tryAnchorNearestOnGraspEnd property to be enabled.", this.gameObject);
            }
        }

        void OnDestroy()
        {
            _intObj.OnGraspedMovement -= onGraspedMovement;

            _anchObj.OnPostTryAnchorOnGraspEnd -= onPostObjectGraspEnd;
        }

        public void ActivateWorkstation()
        {
            if (workstationState != WorkstationState.Open)
            {
                _wasKinematicBeforeActivation = _intObj.rigidbody.isKinematic;
                _intObj.rigidbody.isKinematic = true;
            }

            workstation.SetActive(true);
            workstationState = WorkstationState.Open;
        }

        public void DeactivateWorkstation()
        {
            _intObj.rigidbody.isKinematic = _wasKinematicBeforeActivation;

            workstation.SetActive(false);
            workstationState = WorkstationState.Closed;
        }

        private void refreshRequiredComponents()
        {
            _intObj = GetComponent<InteractionBehaviour>();
            _anchObj = GetComponent<AnchorableBehaviour>();

            _intObj.OnGraspedMovement -= onGraspedMovement;
            _intObj.OnGraspedMovement += onGraspedMovement;

            _anchObj.OnPostTryAnchorOnGraspEnd -= onPostObjectGraspEnd;
            _anchObj.OnPostTryAnchorOnGraspEnd += onPostObjectGraspEnd;
        }

        private void onGraspedMovement(Vector3 preSolvePos, Quaternion preSolveRot,
                                       Vector3 curPos, Quaternion curRot,
                                       List<InteractionController> controllers)
        {
            // If the velocity of the object while grasped is too large, exit workstation mode.
            if (workstationState == WorkstationState.Open
                && (_intObj.rigidbody.velocity.magnitude > MAX_SPEED_AS_WORKSTATION
                || (_intObj.rigidbody.isKinematic && ((preSolvePos - curPos).magnitude / Time.fixedDeltaTime) > MAX_SPEED_AS_WORKSTATION)))
            {
                DeactivateWorkstation();
            }
        }

        private void onPostObjectGraspEnd()
        {
            if (_anchObj.FindPreferredAnchor() == null && !_anchObj.isAttached)
            {
                // Choose a good position and rotation for workstation mode

                Vector3 targetPosition = _intObj.rigidbody.position;

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

    }

}