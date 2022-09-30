/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2022.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using System.Collections.Generic;
using UnityEngine;
using Leap.Unity.Attributes;

namespace Leap.Unity.Preview
{

    public class PinchStrokeProcessor : MonoBehaviour
    {
        private const float MAX_SEGMENT_LENGTH = 0.02F;

        public float Thickness = 0.001f;

        public PaintCursor _paintCursor;
        public GameObject _ribbonParentObject;
        public Material _ribbonMaterial;

        [Header("Effect Settings")]
        public AudioSource _soundEffectSource;
        [Range(0, 1)]
        public float _volumeScale = 1;
        [MinValue(0)]
        public float _maxEffectSpeed = 10;
        [MinMax(0, 2)]
        public Vector2 _pitchRange = new Vector2(0.8f, 1);
        [MinValue(0)]
        public float _smoothingDelay = 0.05f;


        private StrokeProcessor _strokeProcessor;
        private bool _firstStrokePointAdded = false;
        private Vector3 _lastStrokePointAdded = Vector3.zero;
        private float _timeSinceLastAddition = 0F;

        private Vector3 leftHandEulerRotation = new Vector3(0F, 180F, 0F);
        private Vector3 rightHandEulerRotation = new Vector3(0F, 180F, 0F);

        private Vector3 _prevPosition;
        private SmoothedFloat _smoothedSpeed = new SmoothedFloat();
        [HideInInspector]
        public float drawTime = 0f;


        void Start()
        {
            _smoothedSpeed.delay = _smoothingDelay;

            _strokeProcessor = new StrokeProcessor();


            GameObject rendererObj = new GameObject();
            rendererObj.name = "Thick Ribbon Renderer";
            rendererObj.transform.parent = transform;
            var thickRibbonRenderer = rendererObj.AddComponent<ThickRibbonRenderer>();

            thickRibbonRenderer._finalizedRibbonParent = _ribbonParentObject;
            thickRibbonRenderer._ribbonMaterial = _ribbonMaterial;
            _strokeProcessor.RegisterStrokeRenderer(thickRibbonRenderer);
        }

        void Update()
        {
            float angleFromCameraLookVector = Vector3.Angle(Camera.main.transform.forward, _paintCursor.transform.position - Camera.main.transform.position);
            float acceptableFOVAngle = 50F;
            bool withinAcceptableCameraFOV = angleFromCameraLookVector < acceptableFOVAngle;


            if (_paintCursor.IsTracked && !_strokeProcessor.IsBufferingStroke && withinAcceptableCameraFOV)
            {
                BeginStroke();
            }

            if (_paintCursor.DidStartPinch && withinAcceptableCameraFOV && !_strokeProcessor.IsActualizingStroke)
            {
                    StartActualizingStroke();
            }

            if (_paintCursor.IsTracked && _strokeProcessor.IsBufferingStroke)
            {
                UpdateStroke();
            }

            if ((!_paintCursor.IsTracked || !_paintCursor.IsPinching) && _strokeProcessor.IsActualizingStroke)
            {
                StopActualizingStroke();
            }

            if ((!_paintCursor.IsTracked || (!_strokeProcessor.IsActualizingStroke && !withinAcceptableCameraFOV)) && _strokeProcessor.IsBufferingStroke)
            {
                EndStroke();
            }
        }

        private void BeginStroke()
        {
            _strokeProcessor.BeginTrackingStroke();
            _prevPosition = _paintCursor.Position;
        }

        private void StartActualizingStroke()
        {
            _strokeProcessor.StartStroke();
            _soundEffectSource.Play();
        }

        private List<Vector3> _cachedPoints = new List<Vector3>();
        private List<float> _cachedDeltaTimes = new List<float>();
        private void UpdateStroke()
        {
            float speed = Vector3.Distance(_paintCursor.Position, _prevPosition) / Time.deltaTime;
            _prevPosition = _paintCursor.Position;
            _smoothedSpeed.Update(speed, Time.deltaTime);

            float effectPercent = Mathf.Clamp01(_smoothedSpeed.value / _maxEffectSpeed);
            _soundEffectSource.volume = effectPercent * _volumeScale;
            _soundEffectSource.pitch = Mathf.Lerp(_pitchRange.x, _pitchRange.y, effectPercent);

            Vector3 strokePosition = _paintCursor.Position;

            _cachedPoints.Clear();
            _cachedDeltaTimes.Clear();
            if (_firstStrokePointAdded)
            {
                float posDelta = Vector3.Distance(_lastStrokePointAdded, strokePosition);
                if (posDelta > MAX_SEGMENT_LENGTH)
                {
                    float segmentFraction = posDelta / MAX_SEGMENT_LENGTH;
                    float segmentRemainder = segmentFraction % 1F;
                    int numSegments = (int)Mathf.Floor(segmentFraction);
                    Vector3 segment = (strokePosition - _lastStrokePointAdded).normalized * MAX_SEGMENT_LENGTH;
                    Vector3 curPos = _lastStrokePointAdded;
                    float segmentDeltaTime = Time.deltaTime * segmentFraction;
                    float remainderDeltaTime = Time.deltaTime * segmentRemainder;
                    float curDeltaTime = 0F;
                    for (int i = 0; i < numSegments; i++)
                    {
                        _cachedPoints.Add(curPos + segment);
                        _cachedDeltaTimes.Add(curDeltaTime + segmentDeltaTime);
                        curPos += segment;
                        curDeltaTime += segmentDeltaTime;
                    }
                    _cachedPoints.Add(strokePosition);
                    _cachedDeltaTimes.Add(curDeltaTime + remainderDeltaTime);
                    ProcessAddStrokePoints(_cachedPoints, _cachedDeltaTimes);
                }
                else
                {
                    _cachedPoints.Add(strokePosition);
                    _cachedDeltaTimes.Add(Time.deltaTime);
                    ProcessAddStrokePoints(_cachedPoints, _cachedDeltaTimes);
                }
            }
            else
            {
                _cachedPoints.Add(strokePosition);
                _cachedDeltaTimes.Add(Time.deltaTime);
                ProcessAddStrokePoints(_cachedPoints, _cachedDeltaTimes);
            }

            if (_strokeProcessor.IsActualizingStroke)
            {
                drawTime += Time.deltaTime;
            }
        }

        private List<StrokePoint> _cachedStrokePoints = new List<StrokePoint>();
        private void ProcessAddStrokePoints(List<Vector3> points, List<float> effDeltaTimes)
        {
            if (points.Count != effDeltaTimes.Count)
            {
                Debug.LogError("[PinchStrokeProcessor] Points count must match effDeltaTimes count.");
                return;
            }

            _cachedStrokePoints.Clear();
            for (int i = 0; i < points.Count; i++)
            {
                Vector3 point = points[i];
                float effDeltaTime = effDeltaTimes[i];

                bool shouldAdd = !_firstStrokePointAdded
                  || Vector3.Distance(_lastStrokePointAdded, point)
                      >= Thickness;

                _timeSinceLastAddition += effDeltaTime;

                if (shouldAdd)
                {
                    StrokePoint strokePoint = new StrokePoint();
                    strokePoint.position = point;
                    strokePoint.rotation = Quaternion.identity;
                    strokePoint.handOrientation = _paintCursor.Rotation * Quaternion.Euler((_paintCursor.Handedness == Chirality.Left ? leftHandEulerRotation : rightHandEulerRotation));
                    strokePoint.deltaTime = _timeSinceLastAddition;
                    strokePoint.thickness = Thickness;

                    _cachedStrokePoints.Add(strokePoint);

                    _firstStrokePointAdded = true;
                    _lastStrokePointAdded = strokePoint.position;
                    _timeSinceLastAddition = 0F;
                }
            }
            _strokeProcessor.UpdateStroke(_cachedStrokePoints);
        }

        private void StopActualizingStroke()
        {
            _strokeProcessor.StopActualizingStroke();
            _soundEffectSource.Pause();
            _soundEffectSource.volume = 0;
            _smoothedSpeed.reset = true;
        }

        private void EndStroke()
        {
            _strokeProcessor.EndStroke();
            _firstStrokePointAdded = false;
        }

    }


}
