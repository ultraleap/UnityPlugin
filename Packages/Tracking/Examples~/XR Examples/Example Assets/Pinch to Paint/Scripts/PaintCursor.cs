/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2024.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using UnityEngine;

namespace Leap.Examples
{
    /// <summary>
    /// The paint cursor deals with the visuals of the paint cursor, 
    /// and keeps track of the current pinch status and painting status
    /// </summary>
    public class PaintCursor : MonoBehaviour
    {
        /// <summary>
        /// The pinchDetector associated with this cursor
        /// </summary>
        public PinchDetector pinchDetector;


        [SerializeField] private RectToroid _rectToroidPinchTarget;
        [SerializeField] private MeshRenderer _rectToroidPinchTargetRenderer;
        [SerializeField] private RectToroid _rectToroidPinchState;
        [SerializeField] private MeshRenderer _rectToroidPinchStateRenderer;

        // cursor visuals
        private float _thicknessMult = 1.5F;
        private float _radius = 0F;
        private float _minRadius = 0.02F;
        private float _maxRadius = 0.03F;

        /// <summary>
        /// Is the hand that is assigned to the associated pinch detector currently tracked?
        /// </summary>
        public bool IsTracked
        {
            get
            {
                return this.pinchDetector.IsTracked();
            }
        }
        /// <summary>
        /// True if the a pinch has started in the last frame
        /// </summary>
        public bool DidStartPinch
        {
            get
            {
                return this.pinchDetector.PinchStartedThisFrame;
            }
        }

        protected virtual void OnEnable()
        {
            _minRadius = pinchDetector.activateDistance / 2F;
        }

        protected virtual void Update()
        {
            Hand hand = null;
            pinchDetector.TryGetHand(out hand);

            if (hand == null || hand.Index == null || hand.Thumb == null)
            {
                _rectToroidPinchTargetRenderer.enabled = false;
                _rectToroidPinchStateRenderer.enabled = false;
                return;
            }

            _rectToroidPinchTargetRenderer.enabled = true;
            _rectToroidPinchStateRenderer.enabled = true;

            var indexPos = hand.Index.TipPosition;
            var thumbPos = hand.Thumb.TipPosition;
            var indexThumbDist = Vector3.Distance(indexPos, thumbPos);

            // Update the cursor position
            Vector3 indexThumbMiddle = (indexPos + thumbPos) / 2f;
            float effectivePinchStrength = pinchDetector.IsPinching ? 1f : indexThumbDist.Map(0.10f, 0.02f, 0f, 1f);

            var finalPos = Vector3.Lerp(hand.GetPredictedPinchPosition(), indexThumbMiddle, effectivePinchStrength);
            this.transform.position = finalPos;

            // Calculate cursor radius. Mulitply by 0.8, because the cursor should be between the
            // pinching fingers rather than exactly on them
            float pinchRadiusTarget = indexThumbDist / 2f * 0.8f;

            pinchRadiusTarget = Mathf.Clamp(pinchRadiusTarget, _minRadius, _maxRadius);
            _radius = Mathf.Lerp(_radius, pinchRadiusTarget, 20f * Time.deltaTime);

            // Set cursor radius
            if (_rectToroidPinchTarget.Radius != _minRadius)
            {
                _rectToroidPinchTarget.Radius = _minRadius * _thicknessMult;
            }
            _rectToroidPinchState.Radius = _radius * _thicknessMult;
        }
    }
}