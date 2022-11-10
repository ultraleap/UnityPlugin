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

        [Range(0f, 1f), Tooltip("The amount that the interpolation time will be applied to the ray.")]
        public float interpolationAmount = 0f;

        [Tooltip("The maximum length of time that the object will interpolate over. This amount is controlled by the interpolation amount.")]
        public float interpolationTime = 2f;
        private Vector3 _interpolatedPosition = Vector3.zero;
        private float _interpCalcTime = 0, _interpCurrentTime = 0;

        protected override Vector3 CalculateVisualAimPosition()
        {
            return transformToFollow == null ? handRayDirection.Hand.GetPredictedPinchPosition() : CalculateInterpolatedPosition();
        }

        protected override Vector3 CalculateAimPosition()
        {
            return aimPositionFilter.Filter(transformToFollow == null ? handRayDirection.Hand.GetStablePinchPosition() : CalculateInterpolatedPosition(), Time.time);
        }

        private Vector3 CalculateInterpolatedPosition()
        {
            if (transformToFollow == null)
            {
                return handRayDirection.Hand.GetStablePinchPosition();
            }

            if (_interpCalcTime == Time.time)
            {
                return _interpolatedPosition;
            }

            if (_interpCalcTime - Time.time > interpolationAmount)
            {
                _interpolatedPosition = transformToFollow.position;
            }

            _interpCalcTime = Time.time;

            _interpCurrentTime = Mathf.Lerp(0, interpolationTime, interpolationAmount);

            _interpolatedPosition = Vector3.Lerp(_interpolatedPosition, transformToFollow.position, Time.deltaTime * (1.0f / _interpCurrentTime));

            return _interpolatedPosition;
        }

    }
}