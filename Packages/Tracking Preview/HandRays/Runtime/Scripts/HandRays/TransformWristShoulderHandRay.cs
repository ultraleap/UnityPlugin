/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2024.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using UnityEngine;

namespace Leap.Preview.HandRays
{
    /// <summary>
    /// Casts a ray out from a position between a wrist offset and the shoulder, through a transform.
    /// </summary>
    public class TransformWristShoulderHandRay : WristShoulderHandRay
    {
        /// <summary>
        /// The transform used to calculate the hand ray's aim position
        /// </summary>
        public Transform transformToFollow
        {
            get
            {
                return _transformToFollow;
            }
            set
            {
                if (_transformToFollow != value)
                {
                    _transformToFollow = value;
                    // Reset interpolations on transform change
                    _interpCalcTime = Time.time;
                }
            }
        }

        [SerializeField]
        private Transform _transformToFollow;

        [Range(0f, 1f), Tooltip("The amount that the interpolation time will be applied to the ray.")]
        public float interpolationAmount = 0f;
        // Very slightly interp the interp amount
        private float _interpAmountTime = 0.05f;

        [Tooltip("The maximum length of time that the object will interpolate over. This amount is controlled by the interpolation amount.")]
        public float interpolationTime = 2f;
        private Vector3 _interpolatedDirection = Vector3.zero;
        private float _interpCalcTime = 0, _interpCurrentTime = 0, _interpCurrentAmount = 0;

        protected override Vector3 CalculateVisualAimPosition()
        {
            return transformToFollow == null ? handRayDirection.Hand.GetPredictedPinchPosition() : transformToFollow.position;
        }

        protected override Vector3 CalculateAimPosition()
        {
            return aimPositionFilter.Filter(transformToFollow == null ? handRayDirection.Hand.GetStablePinchPosition() : transformToFollow.position);
        }

        protected override Vector3 CalculateDirection()
        {
            return CalculateInterpolatedDirection(base.CalculateDirection());
        }

        private Vector3 CalculateInterpolatedDirection(Vector3 currentDirection)
        {

            if (_interpCalcTime == Time.time)
            {
                return _interpolatedDirection;
            }

            if (_interpCalcTime - Time.time > interpolationAmount)
            {
                _interpolatedDirection = currentDirection;
            }

            _interpCalcTime = Time.time;

            // If the hand is moving very slowly the user is clearly trying to be more accurate.
            float tempAmount = interpolationAmount;

            if (handRayDirection.Hand.PalmVelocity.magnitude < 0.03f)
            {
                tempAmount += Mathf.InverseLerp(0.03f, 0f, handRayDirection.Hand.PalmVelocity.magnitude) * 0.5f;
            }

            _interpCurrentAmount = Mathf.Lerp(_interpCurrentAmount, tempAmount, Time.deltaTime * (1.0f / _interpAmountTime));

            _interpCurrentTime = Mathf.Lerp(0, interpolationTime, _interpCurrentAmount);

            _interpolatedDirection = Vector3.Lerp(_interpolatedDirection, currentDirection, Time.deltaTime * (1.0f / _interpCurrentTime));

            return _interpolatedDirection.normalized;
        }
    }
}