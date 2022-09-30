/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2022.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace Leap.Unity.Preview
{

    public class PaintCursor : MonoBehaviour
    {
        [Header("Pinch Detector")]

        public PinchDetector pinchDetector;


        [Header("Cursor Following")]

        public CursorFollowType cursorFollowType = CursorFollowType.Dynamic;

        public enum CursorFollowType
        {
            Rigid,
            Dynamic
        }

        [Header("Misc")]

        public RectToroid _rectToroidPinchTarget;
        public MeshRenderer _rectToroidPinchTargetRenderer;
        public RectToroid _rectToroidPinchState;
        public MeshRenderer _rectToroidPinchStateRenderer;

        [HideInInspector]
        public HandModelBase _handModel;

        private float _thicknessMult = 1.5F;
        private float _radius = 0F;
        private float _minRadius = 0.02F;
        private float _maxRadius = 0.03F;

        public Vector3 Position
        {
            get { return this.transform.position; }
        }
        public Quaternion Rotation
        {
            get { return this.transform.rotation; }
        }
        public bool IsPinching
        {
            get
            {
                return this.pinchDetector.IsActive;
            }
        }
        public bool IsTracked
        {
            get
            {
                if (this.pinchDetector.HandModel == null) return false;
                return this.pinchDetector.HandModel.IsTracked;
            }
        }
        public bool DidStartPinch
        {
            get
            {
                return this.pinchDetector.DidStartPinch;
            }
        }
        public Chirality Handedness
        {
            get
            {
                return this.pinchDetector.HandModel.Handedness;
            }
        }

        protected virtual void OnEnable()
        {
            _handModel = pinchDetector.GetComponentInParent<HandModelBase>();
            _minRadius = pinchDetector.ActivateDistance / 2F;
        }

        protected virtual void Update()
        {
            if (pinchDetector.HandModel == null)
                pinchDetector.HandModel = new List<HandModelBase>(GameObject.FindObjectsOfType<HandModelBase>()).FirstOrDefault(o => o.gameObject.name == "Capsule Hand Right");

            Leap.Hand hand = null;
            if (pinchDetector.HandModel != null)
            {
                hand = pinchDetector.HandModel.GetLeapHand();
            }

            if (hand == null || hand.GetIndex() == null || hand.GetThumb() == null) return;

            var indexPos = hand.GetIndex().TipPosition;
            var thumbPos = hand.GetThumb().TipPosition;
            var indexThumbDist = Vector3.Distance(indexPos, thumbPos);

            // Cursor follow type
            {
                var rigidLocalPosition = this.transform.parent.InverseTransformPoint(
                                                     hand.GetPredictedPinchPosition());

                switch (cursorFollowType)
                {
                    case CursorFollowType.Rigid:
                        this.transform.localPosition = rigidLocalPosition;
                        break;
                    case CursorFollowType.Dynamic:
                        var pinchPos = (indexPos + thumbPos) / 2f;

                        var idlePos = this.transform.parent.TransformPoint(rigidLocalPosition);
                        var effPinchStrength = 0f;
                        if (IsPinching)
                        {
                            effPinchStrength = 1f;
                        }
                        else
                        {
                            effPinchStrength = indexThumbDist.Map(0.10f, 0.02f, 0f, 1f);
                        }
                        var finalPos = Vector3.Lerp(idlePos, pinchPos, effPinchStrength);

                        this.transform.position = finalPos;
                        break;
                }
            }

            // Calc radius
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
