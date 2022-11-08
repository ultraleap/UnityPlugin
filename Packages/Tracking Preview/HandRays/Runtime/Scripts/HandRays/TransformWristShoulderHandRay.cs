/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2022.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using UnityEngine;

namespace Leap.Unity.Preview.HandRays
{
    /// <summary>
    /// Casts a ray out from a position between a wrist offset and the shoulder, through a transform.
    /// </summary>
    public class TransformWristShoulderHandRay : WristShoulderHandRay
    {
        /// <summary>
        /// The transform used to calculate the hand ray's aim position
        /// </summary>
        public Transform transformToFollow;

        protected override Vector3 CalculateVisualAimPosition()
        {
            return transformToFollow == null ? handRayDirection.Hand.GetPredictedPinchPosition() : transformToFollow.position;
        }

        protected override Vector3 CalculateAimPosition()
        {
            return aimPositionFilter.Filter(transformToFollow == null ? handRayDirection.Hand.GetStablePinchPosition() : transformToFollow.position, Time.time);
        }

    }
}