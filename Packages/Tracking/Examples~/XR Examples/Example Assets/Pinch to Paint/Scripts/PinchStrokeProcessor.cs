/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2024.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using Leap.Attributes;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Examples
{
    /// <summary>
    /// This manages Pinch Painting for one given hand. It needs a PaintCursor. 
    /// It listens to the PaintCursor for pinch status and positions and
    /// manages adding new stroke points in the correct positions, and playing painting audio with correct volume and pitch.
    /// It creates a StrokeProcessor which deals with the actual stroke creation logic.
    /// </summary>
    public class PinchStrokeProcessor : MonoBehaviour
    {
        private const float MAX_SEGMENT_LENGTH = 0.02F;

        /// <summary>
        /// The thickness of the created strokes.
        /// </summary>
        [SerializeField, Tooltip("The thickness of the created strokes")]
        private float Thickness = 0.001f;

        [SerializeField, Tooltip("A PinchStrokeProcessor needs a reference to a PaintCursor, for information such as pinch status and position")]
        private PaintCursor _paintCursor;
        [SerializeField, Tooltip("Empty GameObject that will be the parent object for the ribbon objects created at runtime")]
        private GameObject _ribbonParentObject;
        [SerializeField] private Material _ribbonMaterial;

        [SerializeField, Tooltip("If the angle between camera view direction and camera to pinch cursor direction is greater than this, we won't draw")]
        private float _acceptableFOVAngle = 50f;

        [Header("Effect Settings")]
        [SerializeField] private AudioClip _beginDrawingClip;
        [SerializeField] private AudioSource _soundEffectSource;
        [Range(0, 1), Tooltip("This affects the volume of the soundEffectsSource, which plays both the beginDrawingClip and the paintingLoop while painting")]
        [SerializeField] private float _volumeScale = 1;
        [MinValue(0), Tooltip("This affects the volume and the pitch of the soundEffectsSource, which plays both the beginDrawingClip and the paintingLoop while painting")]
        [SerializeField] private float _maxEffectSpeed = 10;
        [MinMax(0, 2), Tooltip("Range of the pitch of the Painting Loop Audio")]
        [SerializeField] private Vector2 _pitchRange = new Vector2(0.8f, 1);
        [MinValue(0), Tooltip("Used to smooth the pitch and volume changes depending on the speed of painting and controlled by the above floats")]
        [SerializeField] private float _smoothingDelay = 0.05f;


        private StrokeProcessor _strokeProcessor;
        private bool _firstStrokePointAdded = false;
        private Vector3 _lastStrokePoint = Vector3.zero;

        /// <summary>
        /// Used to determine speed of painting
        /// </summary>
        private Vector3 _prevPosition;
        private SmoothedFloat _smoothedSpeed = new SmoothedFloat();


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
            _strokeProcessor.StrokeRenderer = thickRibbonRenderer;
        }

        void Update()
        {
            // check whether we are trying to draw within an acceptable camera FOV
            float angleFromCameraLookVector = Vector3.Angle(Camera.main.transform.forward, _paintCursor.transform.position - Camera.main.transform.position);
            bool withinAcceptableCameraFOV = angleFromCameraLookVector < _acceptableFOVAngle;


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

            if ((!_paintCursor.IsTracked || !_paintCursor.pinchDetector.IsPinching) && _strokeProcessor.IsActualizingStroke)
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
            _prevPosition = _paintCursor.transform.position;
        }

        private void StartActualizingStroke()
        {
            _strokeProcessor.StartStroke();
            _soundEffectSource.volume = 1;
            _soundEffectSource.PlayOneShot(_beginDrawingClip);
            _soundEffectSource.Play();
        }

        List<StrokePoint> newStrokePoints = new List<StrokePoint>();

        /// <summary>
        /// Updates the painting audio's volume and pitch based on the painting speed and
        /// creates and adds new stroke points to the stroke processor
        /// </summary>
        private void UpdateStroke()
        {
            // calculate current speed of painting
            float speed = Vector3.Distance(_paintCursor.transform.position, _prevPosition) / Time.deltaTime;
            _prevPosition = _paintCursor.transform.position;
            _smoothedSpeed.Update(speed, Time.deltaTime);

            // use speed to set the painting audio's volume and pitch
            float effectPercent = Mathf.Clamp01(_smoothedSpeed.value / _maxEffectSpeed);
            _soundEffectSource.volume = effectPercent * _volumeScale;
            _soundEffectSource.pitch = Mathf.Lerp(_pitchRange.x, _pitchRange.y, effectPercent);


            // add new strokePoints to the stroke

            Vector3 strokePosition = _paintCursor.transform.position;
            newStrokePoints.Clear();

            // if no new stroke points have been added for a while and the current stroke position is further away from the last stroke position than the MAX_SEGMENT_LENGTH, 
            // add multiple stroke points in between the last stroke point and the current stroke position
            if (_firstStrokePointAdded && Vector3.Distance(_lastStrokePoint, strokePosition) > MAX_SEGMENT_LENGTH)
            {
                float segmentFraction = Vector3.Distance(_lastStrokePoint, strokePosition) / MAX_SEGMENT_LENGTH;
                int numSegments = (int)Mathf.Floor(segmentFraction);
                Vector3 segment = (strokePosition - _lastStrokePoint).normalized * MAX_SEGMENT_LENGTH;
                Vector3 curPos = _lastStrokePoint;
                for (int i = 0; i < numSegments; i++)
                {
                    if (ShouldAddStrokePoint(curPos + segment)) newStrokePoints.Add(ProcessStrokePoint(curPos + segment));
                    curPos += segment;
                }
            }

            if (ShouldAddStrokePoint(strokePosition)) newStrokePoints.Add(ProcessStrokePoint(strokePosition));

            // update stroke with the new stroke points
            if (newStrokePoints.Count > 0) _strokeProcessor.UpdateStroke(newStrokePoints);
        }

        /// <summary>
        /// returns whether the input point should be added as a new stroke point, 
        /// by looking at the distance between the last added stroke point and the input point
        /// </summary>
        private bool ShouldAddStrokePoint(Vector3 point)
        {
            return !_firstStrokePointAdded ||
                Vector3.Distance(_lastStrokePoint, point) >= Thickness;
        }

        /// <summary>
        /// Creates and returns a new stroke Point from the input point position
        /// </summary>
        private StrokePoint ProcessStrokePoint(Vector3 point)
        {
            StrokePoint strokePoint = new StrokePoint();
            strokePoint.position = point;
            strokePoint.rotation = Quaternion.identity;
            strokePoint.handOrientation = _paintCursor.transform.rotation * Quaternion.Euler(0F, 180F, 0F);
            strokePoint.thickness = Thickness;

            _firstStrokePointAdded = true;
            _lastStrokePoint = strokePoint.position;

            return strokePoint;
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