/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2024.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using Leap.Unity.Attributes;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.Examples
{
    /// <summary>
    /// This deals with rotating the turntable whenever any fingertips are intersecting with it.
    /// It also calls TurntableVisuals.UpdateVisuals() when any turntable parameters have changed.
    /// </summary>
    public class Turntable : MonoBehaviour
    {
        [SerializeField]
        private LeapProvider _provider;

        [Header("Turntable Shape")]
        [SerializeField]
        [Tooltip("The local height of the upper section of the turntable.")]
        private float _tableHeight;
        public float TableHeight => _tableHeight;

        [MinValue(0)]
        [SerializeField]
        [Tooltip("The radius of the upper section of the turntable.")]
        private float _tableRadius;
        public float TableRadius => _tableRadius;

        [MinValue(0)]
        [SerializeField]
        [Tooltip("The length of the edge that connects the upper and lower sections of the turntable.")]
        private float _edgeLength;

        [Range(0, 90)]
        [SerializeField]
        [Tooltip("The angle the edge forms with the upper section of the turntable.")]
        private float _edgeAngle = 45;

        [Header("Turntable Motion")]
        [MinValue(0)]
        [SerializeField]
        [Tooltip("How much to scale the rotational motion by.  A value of 1 causes no extra scale.")]
        private float _rotationScale = 1.5f;

        [MinValue(0.00001f)]
        [SerializeField]
        [Tooltip("How much to smooth the velocity while the user is touching the turntable.")]
        private float _rotationSmoothing = 0.1f;

        [Range(0, 1)]
        [SerializeField]
        [Tooltip("The damping factor to use to damp the rotational velocity of the turntable.")]
        private float _rotationDamping = 0.95f;

        [MinValue(0)]
        [SerializeField]
        [Tooltip("The speed under which the turntable will stop completely.")]
        private float _minimumSpeed = 0.01f;

        public float LowerLevelHeight
        {
            get
            {
                return _tableHeight - _edgeLength * Mathf.Sin(_edgeAngle * Mathf.Deg2Rad);
            }
        }

        public float LowerLevelRadius
        {
            get
            {
                return _tableRadius + _edgeLength * Mathf.Cos(_edgeAngle * Mathf.Deg2Rad);
            }
        }

        // Maps a finger from a specific finger to the world tip position when it first entered the turntable
        private Dictionary<int, Vector3> _currTipPoints = new Dictionary<int, Vector3>();
        private Dictionary<int, Vector3> _prevTipPoints = new Dictionary<int, Vector3>();

        private SmoothedFloat _smoothedVelocity;
        private float _rotationalVelocity;

        private TurntableVisuals _turntableVisuals;

        private void OnValidate()
        {
            if (!Application.isPlaying)
            {
                if (_turntableVisuals == null) _turntableVisuals = GetComponent<TurntableVisuals>();
                if (_turntableVisuals != null) _turntableVisuals.UpdateVisuals();
            }
        }

        private void Awake()
        {
            if (_provider == null)
            {
                _provider = Hands.Provider;
            }

            _smoothedVelocity = new SmoothedFloat();
            _smoothedVelocity.delay = _rotationSmoothing;

            if (_turntableVisuals == null) _turntableVisuals = GetComponent<TurntableVisuals>();
            if (_turntableVisuals != null) _turntableVisuals.ArcDegrees = 360;
        }

        private void Update()
        {
            Leap.Unity.Utils.Swap(ref _currTipPoints, ref _prevTipPoints);

            _currTipPoints.Clear();
            foreach (var hand in _provider.CurrentFrame.Hands)
            {
                // use a fingerID that allows for unique IDs for each finger of each hand. 0-4 = left, 5-9 = right
                int fingerID = hand.IsLeft ? 0 : hand.Fingers.Count;
                foreach (var finger in hand.Fingers)
                {
                    Vector3 worldTip = finger.Bone(Bone.BoneType.TYPE_DISTAL).NextJoint;
                    Vector3 localTip = transform.InverseTransformPoint(worldTip);

                    if (IsPointInsideTurntable(localTip))
                    {
                        _currTipPoints[fingerID] = worldTip;
                    }

                    fingerID++;
                }
            }

            float deltaAngleSum = 0;
            float deltaAngleWeight = 0;
            foreach (var pair in _currTipPoints)
            {
                Vector3 currWorldTip = pair.Value;
                Vector3 prevWorldTip;

                if (!_prevTipPoints.TryGetValue(pair.Key, out prevWorldTip))
                {
                    continue;
                }

                Vector3 currLocalTip = transform.InverseTransformPoint(currWorldTip);
                Vector3 prevLocalTip = transform.InverseTransformPoint(prevWorldTip);

                deltaAngleSum += Vector2.SignedAngle(prevLocalTip.xz(), currLocalTip.xz()) * _rotationScale * -1.0f;
                deltaAngleWeight += 1.0f;
            }

            if (deltaAngleWeight > 0.0f)
            {
                float deltaAngle = deltaAngleSum / deltaAngleWeight;

                Vector3 localRotation = transform.localEulerAngles;
                localRotation.y += deltaAngle;
                transform.localEulerAngles = localRotation;

                _smoothedVelocity.Update(deltaAngle / Time.deltaTime, Time.deltaTime);
                _rotationalVelocity = _smoothedVelocity.value;
            }
            else
            {
                _rotationalVelocity *= _rotationDamping;
                if (Mathf.Abs(_rotationalVelocity) < _minimumSpeed)
                {
                    _rotationalVelocity = 0;
                }

                Vector3 localRotation = transform.localEulerAngles;
                localRotation.y += _rotationalVelocity * Time.deltaTime;
                transform.localEulerAngles = localRotation;
            }
        }

        private bool IsPointInsideTurntable(Vector3 localPoint)
        {
            if (localPoint.y > _tableHeight || localPoint.y < LowerLevelHeight)
            {
                return false;
            }

            float heightFactor = Mathf.Clamp01(Mathf.InverseLerp(_tableHeight, LowerLevelHeight, localPoint.y));
            float effectiveRadius = Mathf.Lerp(_tableRadius, LowerLevelRadius, heightFactor);

            float pointRadius = new Vector2(localPoint.x, localPoint.z).magnitude;
            if (pointRadius > effectiveRadius || pointRadius < effectiveRadius - 0.05f)
            {
                return false;
            }

            return true;
        }
    }
}