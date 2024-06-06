/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2024.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Leap.Unity
{
    public class TrackingMarker : MonoBehaviour
    {
        [Tooltip("The AprilTag marker ID associated with this marker." +
            "\n\nNote: This must be unique within the scene")]
        public int id;

        public FiducialPoseEventArgs FiducialPose
        {
            set
            {
                _fiducialPose = value;
                _lastUpdateFrame = Time.frameCount;
            }
            get
            {
                return _fiducialPose;
            }
        }
        private FiducialPoseEventArgs _fiducialPose = null;
        private int _lastUpdateFrame = -1;

        private MeshRenderer _renderer;

        private void Awake()
        {
            _renderer = GetComponentInChildren<MeshRenderer>(); 
        }

        private void Update()
        {
            if (_renderer == null)
                return;

            if (_lastUpdateFrame < Time.frameCount - 60)
            {
                _renderer.enabled = false;
            }
            else if (!_renderer.enabled)
            {
                _renderer.enabled = true;
            }

            if (_renderer.enabled && _fiducialPose != null)
            {
                float remap = RemapValue(_fiducialPose.estimated_error, 6.991618E-13f, 4.107744E-06f, 0.0f, 1.0f);
                _renderer.material.color = new Color(remap, 0.0f, 0.0f);
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (_fiducialPose == null || _renderer == null || !_renderer.enabled)
                return;

            //Handles.Label(this.transform.position, _fiducialPose.timestamp.ToString());
            Handles.Label(this.transform.position, _fiducialPose.estimated_error.ToString());
        }

        private float RemapValue(float value, float fromMin, float fromMax, float toMin, float toMax)
        {
            if (value > fromMax)
                value = fromMax;
            else if (value < fromMin)
                value = fromMin;

            float fromRange = fromMax - fromMin;
            float toRange = toMax - toMin;
            float scaledValue = (value - fromMin) / fromRange;
            float remappedValue = toMin + (scaledValue * toRange);
            return remappedValue;
        }
    }
}