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
                _renderer.enabled = _fiducialPose != null;
            }
            get
            {
                return _fiducialPose;
            }
        }
        private FiducialPoseEventArgs _fiducialPose = null;

        private MeshRenderer _renderer;

        private void Awake()
        {
            _renderer = GetComponentInChildren<MeshRenderer>(); 
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (_fiducialPose == null)
                return;

            //Handles.Label(this.transform.position, _fiducialPose.timestamp.ToString());
            Handles.Label(this.transform.position, _fiducialPose.estimated_error.ToString());
        }
#endif
    }
}