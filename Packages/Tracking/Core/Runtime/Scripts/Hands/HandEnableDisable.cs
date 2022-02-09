/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2021.                                   *
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
        protected override void Awake()
        {
            // Suppress Warnings Related to Kinematic Rigidbodies not supporting Continuous Collision Detection
#if UNITY_2018_3_OR_NEWER
            Rigidbody[] bodies = GetComponentsInChildren<Rigidbody>();
            foreach (Rigidbody body in bodies)
            {
                if (body.isKinematic && body.collisionDetectionMode == CollisionDetectionMode.Continuous)
                {
                    body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
                }
            }
#endif

            base.Awake();
        }

        protected override void HandReset()
        {
            gameObject.SetActive(true);
        }

        protected override void HandFinish()
        {
            gameObject.SetActive(false);
        }

    }
}