/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2024.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using Leap.Unity.Attributes;
using UnityEngine;
using UnityEngine.Serialization;

namespace Leap.Unity
{
    /// <summary>
    /// A basic utility class to aid in creating pinch based actions.  Once linked with a HandModelBase, it can
    /// be used to detect pinch gestures that the hand makes.
    /// </summary>
    public class PinchDetector : AbstractHoldDetector
    {
        protected const float MM_TO_M = 0.001f;

        [Tooltip("The distance at which to enter the pinching state.")]
        [Header("Distance Settings")]
        [MinValue(0)]
        [Units("meters")]
        [FormerlySerializedAs("_activatePinchDist")]
        public float ActivateDistance = .03f; //meters
        [Tooltip("The distance at which to leave the pinching state.")]
        [MinValue(0)]
        [Units("meters")]
        [FormerlySerializedAs("_deactivatePinchDist")]
        public float DeactivateDistance = .04f; //meters

        public bool IsPinching { get { return this.IsHolding; } }
        public bool DidStartPinch { get { return this.DidStartHold; } }
        public bool DidEndPinch { get { return this.DidRelease; } }

        protected bool _isPinching = false;

        protected float _lastPinchTime = 0.0f;
        protected float _lastUnpinchTime = 0.0f;

        protected Vector3 _pinchPos;
        protected Quaternion _pinchRotation;

        protected virtual void OnValidate()
        {
            ActivateDistance = Mathf.Max(0, ActivateDistance);
            DeactivateDistance = Mathf.Max(0, DeactivateDistance);

            //Activate value cannot be less than deactivate value
            if (DeactivateDistance < ActivateDistance)
            {
                DeactivateDistance = ActivateDistance;
            }
        }

        private float GetPinchDistance(Hand hand)
        {
            var indexTipPosition = hand.GetIndex().TipPosition;
            var thumbTipPosition = hand.GetThumb().TipPosition;
            return Vector3.Distance(indexTipPosition, thumbTipPosition);
        }

        protected override void ensureUpToDate()
        {
            if (Time.frameCount == _lastUpdateFrame)
            {
                return;
            }
            _lastUpdateFrame = Time.frameCount;

            _didChange = false;

            Hand hand = Hands.Right;

            if (hand == null)
            {
                changeState(false);
                return;
            }

            _distance = GetPinchDistance(hand);
            _rotation = hand.Basis.rotation;
            _position = (hand.Fingers[0].TipPosition + hand.Fingers[1].TipPosition) * .5f;

            if (IsActive)
            {
                if (_distance > DeactivateDistance)
                {
                    changeState(false);
                    //return;
                }
            }
            else
            {
                if (_distance < ActivateDistance)
                {
                    changeState(true);
                }
            }

            if (IsActive)
            {
                _lastPosition = _position;
                _lastRotation = _rotation;
                _lastDistance = _distance;
                _lastDirection = _direction;
                _lastNormal = _normal;
            }
            if (ControlsTransform)
            {
                transform.position = _position;
                transform.rotation = _rotation;
            }
        }
    }
}