/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2024.                                   *
 * Ultraleap proprietary and confidential.                                    *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/
using UnityEngine;

namespace Leap.Preview.HandRays
{
    /// <summary>
    /// Casts a ray out from a position between a wrist offset and the shoulder, through the palm.
    /// </summary>
    public class PalmWristShoulderHandRay : WristShoulderHandRay
    {
        private Vector3 palmWristOffset = new Vector3(0, 0.2f, 0);

        protected override Vector3 CalculateVisualAimPosition()
        {
            return handRayDirection.Hand.PalmPosition;
        }

        protected override Vector3 CalculateAimPosition()
        {
            return aimPositionFilter.Filter(handRayDirection.Hand.PalmPosition);
        }

        protected override Vector3 GetWristOffsetPosition(Hand hand)
        {
            Vector3 localWristPosition = palmWristOffset;
            if (hand.IsRight)
            {
                localWristPosition.x = -localWristPosition.x;
            }

            transformHelper.transform.position = hand.WristPosition;
            transformHelper.transform.rotation = hand.Rotation;
            return transformHelper.TransformPoint(localWristPosition);
        }
    }
}