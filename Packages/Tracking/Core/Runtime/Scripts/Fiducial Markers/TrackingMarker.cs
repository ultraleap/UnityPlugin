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

        private GameObject[] _children; //using these to render

        private void Awake()
        {
            _children = new GameObject[transform.childCount];
            for (int i = 0; i < _children.Length; i++)
                _children[i] = transform.GetChild(i).gameObject;
        }

        private void Update()
        {
            if (_lastUpdateFrame <  Time.frameCount - 60)
                _children.ForEach(o => o.SetActive(false));
            else if (_children.Length > 0 && !_children[0].activeInHierarchy)
                _children.ForEach(o => o.SetActive(true));

        }

        private void OnDrawGizmosSelected()
        {
            if (_fiducialPose == null)
                return;

            Handles.Label(this.transform.position, _fiducialPose.timestamp.ToString());
        }
    }
}