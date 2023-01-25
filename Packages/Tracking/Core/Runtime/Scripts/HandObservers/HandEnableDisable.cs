/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2023.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using UnityEngine;

namespace Leap.Unity
{
    /// <summary>
    /// A component to be attached to a HandModelBase to handle starting and ending of
    /// tracking. 
    /// The parent gameobjet is activated when tracking begins and deactivated when
    /// tracking ends.
    /// </summary>
    public class HandEnableDisable : HandTransitionBehavior
    {

        [Tooltip("When enabled, freezes the hand in its current active state")]
        public bool FreezeHandState = false;

        protected override void Awake()
        {
            // Suppress Warnings Related to Kinematic Rigidbodies not supporting Continuous Collision Detection
            Rigidbody[] bodies = GetComponentsInChildren<Rigidbody>();
            foreach (Rigidbody body in bodies)
            {
                if (body.isKinematic && body.collisionDetectionMode == CollisionDetectionMode.Continuous)
                {
                    body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
                }
            }

            base.Awake();
        }

        protected override void HandReset()
        {
            if (FreezeHandState)
            {
                return;
            }

            gameObject.SetActive(true);
        }

        protected override void HandFinish()
        {
            if (FreezeHandState)
            {
                return;
            }
            gameObject.SetActive(false);
        }
    }
}