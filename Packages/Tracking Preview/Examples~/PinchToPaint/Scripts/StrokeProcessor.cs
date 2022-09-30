/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2022.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using UnityEngine;
using System.Collections.Generic;

namespace Leap.Unity.Preview
{
    [System.Serializable]
    public struct StrokePoint
    {

        public Vector3 position;
        public Vector3 normal;
        public Quaternion rotation;
        public Quaternion handOrientation;
        public float deltaTime;
        public float thickness;
        public Color color;

    }

    public class StrokeProcessor
    {

        // Stroke processing configuration
        private int _maxMemory = 8;

        // Stroke state
        private bool _isBufferingStroke = false;
        private bool _isActualizingStroke = false;

        private RingBuffer<StrokePoint> _strokeBuffer;
        private RingBuffer<int> _actualizedStrokeIdxBuffer;
        private int _actualizedStrokeIdx = 0;

        private List<StrokePoint> _strokeOutput = null;
        private int _outputBufferEndOffset = 0;

        public bool IsBufferingStroke { get { return _isBufferingStroke; } }
        public bool IsActualizingStroke { get { return _isActualizingStroke; } }

        // Stroke renderers
        private List<IStrokeRenderer> _strokeRenderers = null;

        // Stroke buffer renderers
        private List<IStrokeBufferRenderer> _strokeBufferRenderers = null;

        public StrokeProcessor()
        {
            _strokeRenderers = new List<IStrokeRenderer>();
            _strokeBufferRenderers = new List<IStrokeBufferRenderer>();
            _strokeOutput = new List<StrokePoint>();
            _strokeBuffer = new RingBuffer<StrokePoint>(_maxMemory);
            _actualizedStrokeIdxBuffer = new RingBuffer<int>(_maxMemory);
        }

        public void RegisterStrokeRenderer(IStrokeRenderer strokeRenderer)
        {
            _strokeRenderers.Add(strokeRenderer);
            if (_isBufferingStroke)
            {
                Debug.LogError("[StrokeProcessor] Stroke in progress; Newly registered stroke renderers will not render the entire stroke if a stroke is already in progress.");
            }
        }

        public void RegisterPreviewStrokeRenderer(IStrokeBufferRenderer strokeBufferRenderer)
        {
            _strokeBufferRenderers.Add(strokeBufferRenderer);
            if (_isBufferingStroke)
            {
                Debug.LogError("[StrokeProcessor] Stroke buffer already active; Newly registered stroke buffer renderers will not render the entire preview stroke if a stroke is already in progress.");
            }
        }

        public void BeginTrackingStroke()
        {
            if (_isBufferingStroke)
            {
                Debug.LogError("[StrokeProcessor] Stroke in progress; cannot begin new stroke. Call EndStroke() to finalize the current stroke first.");
                return;
            }
            _isBufferingStroke = true;

            if (_strokeBuffer != null) _strokeBuffer.Clear();
            if (_actualizedStrokeIdxBuffer != null)_actualizedStrokeIdxBuffer.Clear();

            for (int i = 0; i < _strokeBufferRenderers.Count; i++)
            {
                _strokeBufferRenderers[i].InitializeRenderer();
            }
        }

        public void StartStroke()
        {
            if (!_isBufferingStroke)
            {
                BeginTrackingStroke();
            }

            if (_isActualizingStroke)
            {
                Debug.LogError("[StrokeProcessor] Stroke already actualizing; cannot begin actualizing stroke. Call StopActualizingStroke() first.");
                return;
            }
            _isActualizingStroke = true;
            _actualizedStrokeIdx = 0;
            _strokeOutput = new List<StrokePoint>(); // can't clear -- other objects have references to the old stroke output.
            _outputBufferEndOffset = 0;

            for (int i = 0; i < _strokeRenderers.Count; i++)
            {
                _strokeRenderers[i].InitializeRenderer();
            }
        }


        // shouldUpdateRenderers provides an optimization for updating multiple stroke points at once,
        // where it's more efficient to do the updating without rendering and then refreshing renderers
        // at the end.
        private void UpdateStroke(StrokePoint strokePoint, bool shouldUpdateRenderers = true)
        {
            if(_strokeBuffer == null)
            {
                _strokeBuffer = new RingBuffer<StrokePoint>(8);
                _actualizedStrokeIdxBuffer = new RingBuffer<int>(8);
            }

            _strokeBuffer.Add(strokePoint);
            _actualizedStrokeIdxBuffer.Add(-1);

            FilterPositionMovingAverage(_strokeBuffer);
            FilterPitchYawRoll(_strokeBuffer);

            if (_isActualizingStroke)
            {
                _actualizedStrokeIdxBuffer.SetLatest(_actualizedStrokeIdx++);

                // Output points from the buffer to the actualized stroke output.
                int offset = Mathf.Min(_outputBufferEndOffset, _strokeBuffer.Count - 1);
                for (int i = 0; i <= offset; i++)
                {
                    int outputIdx = Mathf.Max(0, _outputBufferEndOffset - (_strokeBuffer.Count - 1)) + i;
                    StrokePoint bufferStrokePoint = _strokeBuffer.Get(_strokeBuffer.Count - 1 - (Mathf.Min(_strokeBuffer.Count - 1, _outputBufferEndOffset) - i));
                    if (outputIdx > _strokeOutput.Count - 1)
                    {
                        _strokeOutput.Add(bufferStrokePoint);
                    }
                    else
                    {
                        _strokeOutput[outputIdx] = bufferStrokePoint;
                    }
                }
                _outputBufferEndOffset += 1;

                // Refresh stroke renderers.
                if (shouldUpdateRenderers)
                {
                    UpdateStrokeRenderers();
                }
            }

            // Refresh stroke preview renderers.
            if (shouldUpdateRenderers)
            {
                UpdateStrokeBufferRenderers();
            }
        }

        private void FilterPositionMovingAverage(RingBuffer<StrokePoint> data)
        {
            int NEIGHBORHOOD = 4;
            for (int i = Mathf.Min(data.Count - 1, NEIGHBORHOOD); i >= 0; i--)
            {
                StrokePoint point = data.GetFromEnd(i);
                point.position = CalcNeighborAverage(i, NEIGHBORHOOD, data);
                data.SetFromEnd(i, point);
            }
        }

        private Vector3 CalcNeighborAverage(int index, int R, RingBuffer<StrokePoint> data)
        {
            Vector3 neighborSum = data.GetFromEnd(index).position;
            int numPointsInRadius = 1;
            while (index + R > data.Count - 1 || index - R < 0) R -= 1;
            for (int r = 1; r <= R; r++)
            {
                neighborSum += data.GetFromEnd((index - r)).position;
                neighborSum += data.GetFromEnd((index + r)).position;
                numPointsInRadius += 2;
            }
            return neighborSum / numPointsInRadius;
        }

        private void FilterPitchYawRoll(RingBuffer<StrokePoint> data)
        {
            if (data.Count < 1) return;
            if (data.Count == 1)
            {
                StrokePoint point = data.Get(0);
                point.rotation = point.handOrientation;
                point.normal = point.rotation * Vector3.up;
                data.Set(0, point);
            }
            else if (data.Count >= 2)
            {
                for (int offset = 0; offset < data.Count - 1; offset++)
                {
                    StrokePoint point = data.Get(0 + offset);
                    Vector3 T = point.rotation * Vector3.forward;
                    Vector3 N = point.rotation * Vector3.up;

                    Vector3 segmentDirection = (data.Get(1 + offset).position - point.position).normalized;

                    // Pitch correction
                    Vector3 sD_TN = (Vector3.Dot(T, segmentDirection) * T + Vector3.Dot(N, segmentDirection) * N).normalized;
                    Vector3 T_x_sD_TN = Vector3.Cross(T, sD_TN);
                    float T_x_sD_TN_magnitude = Mathf.Clamp(T_x_sD_TN.magnitude, 0F, 1F); // Fun fact! Sometimes the magnitude of this vector is 0.000002 larger than 1F, which causes NaNs from Mathf.Asin().
                    Quaternion pitchCorrection;
                    if (Vector3.Dot(T, sD_TN) >= 0F)
                    {
                        pitchCorrection = Quaternion.AngleAxis(Mathf.Asin(T_x_sD_TN_magnitude) * Mathf.Rad2Deg, T_x_sD_TN.normalized);
                    }
                    else
                    {
                        pitchCorrection = Quaternion.AngleAxis(180F - (Mathf.Asin(T_x_sD_TN_magnitude) * Mathf.Rad2Deg), T_x_sD_TN.normalized);
                    }

                    // Yaw correction
                    Vector3 T_pC = pitchCorrection * T;
                    Vector3 T_pC_x_sD = Vector3.Cross(T_pC, segmentDirection);
                    Quaternion yawCorrection = Quaternion.AngleAxis(Mathf.Asin(T_pC_x_sD.magnitude) * Mathf.Rad2Deg, T_pC_x_sD.normalized);

                    // Roll correction (align to canvas)
                    T = pitchCorrection * yawCorrection * T;
                    N = pitchCorrection * yawCorrection * N;
                    Vector3 handUp = point.handOrientation * Vector3.up;
                    Vector3 handDown = point.handOrientation * Vector3.down;
                    Vector3 canvasDirection;
                    if (Vector3.Dot(N, handUp) >= 0F)
                    {
                        canvasDirection = handUp;
                    }
                    else
                    {
                        canvasDirection = handDown;
                    }
                    Vector3 B = Vector3.Cross(T, N).normalized; // binormal
                    Vector3 canvasCastNB = (Vector3.Dot(N, canvasDirection) * N + Vector3.Dot(B, canvasDirection) * B);
                    Vector3 N_x_canvasNB = Vector3.Cross(N, canvasCastNB.normalized);
                    float N_x_canvasNB_magnitude = Mathf.Clamp(N_x_canvasNB.magnitude, 0F, 1F); // Fun fact! Sometimes the magnitude of this vector is 0.000002 larger than 1F, which causes NaNs from Mathf.Asin().
                    Quaternion rollCorrection = Quaternion.AngleAxis(
                      DeadzoneDampenFilter(canvasCastNB.magnitude) * Mathf.Asin(N_x_canvasNB_magnitude) * Mathf.Rad2Deg,
                      N_x_canvasNB.normalized
                      );

                    point.rotation = pitchCorrection * yawCorrection * rollCorrection * point.rotation;
                    point.normal = point.rotation * Vector3.up;

                    data.Set(0 + offset, point);

                    StrokePoint nextPoint = data.Get(1 + offset);
                    nextPoint.rotation = point.rotation;
                    nextPoint.normal = point.normal;
                    data.Set(1 + offset, nextPoint);
                }
            }
        }

        // Assumes input from 0 to 1.
        private float DeadzoneDampenFilter(float input)
        {
            float deadzone = 0.5F;
            float dampen = 0.2F;
            return Mathf.Max(0F, (input - deadzone) * dampen);
        }

        public void UpdateStroke(List<StrokePoint> strokePoints)
        {
            // UpdateStroke without updating renderers.
            for (int i = 0; i < strokePoints.Count; i++)
            {
                UpdateStroke(strokePoints[i], false);
            }

            // Manually update renderers.
            if (strokePoints.Count > 0)
            {
                if (_isActualizingStroke)
                {
                    UpdateStrokeRenderers();
                }
                UpdateStrokeBufferRenderers();
            }
        }

        private void UpdateStrokeRenderers()
        {
            for (int i = 0; i < _strokeRenderers.Count; i++)
            {
                _strokeRenderers[i].UpdateRenderer(_strokeOutput, _maxMemory);
            }
        }

        private void UpdateStrokeBufferRenderers()
        {
            for (int i = 0; i < _strokeBufferRenderers.Count; i++)
            {
                _strokeBufferRenderers[i].RefreshRenderer(_strokeBuffer);
            }
        }

        public void StopActualizingStroke()
        {
            if (!_isActualizingStroke)
            {
                Debug.LogError("[StrokeProcessor] Can't stop actualizing stroke; stroke never began actualizing in the first place.");
                Debug.Break();
            }
            _isActualizingStroke = false;

            for (int i = 0; i < _strokeRenderers.Count; i++)
            {
                _strokeRenderers[i].FinalizeRenderer();
            }
        }

        public void EndStroke()
        {
            if (_isActualizingStroke)
            {
                StopActualizingStroke();
            }

            _isBufferingStroke = false;

            for (int i = 0; i < _strokeBufferRenderers.Count; i++)
            {
                _strokeBufferRenderers[i].StopRenderer();
            }
        }

    }

    public static class RingBufferExtensions
    {

        public static T GetFromEnd<T>(this RingBuffer<T> ringBuffer, int idxFromEnd)
        {
            return ringBuffer.Get(ringBuffer.Count - 1 - idxFromEnd);
        }

        public static void SetFromEnd<T>(this RingBuffer<T> ringBuffer, int idxFromEnd, T value)
        {
            ringBuffer.Set(ringBuffer.Count - 1 - idxFromEnd, value);
        }

    }


}