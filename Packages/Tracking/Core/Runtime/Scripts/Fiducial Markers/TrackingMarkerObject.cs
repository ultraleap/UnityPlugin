/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2024.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using LeapInternal;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Leap.Unity
{
    public class TrackingMarkerObject : MonoBehaviour
    {
        [Tooltip("The source of the tracking data.")]
        public LeapServiceProvider leapServiceProvider;

        private LeapTransform _trackerPosWorldspace;
        private TrackingMarker[] _markers;

        private List<FiducialPoseEventArgs> _poses = new List<FiducialPoseEventArgs>();
        private float _previousFiducialFrameTime = -1;

        private float _fiducialBaseTime = -1;
        private float _fiducialFPS = 0;

        private void Awake()
        {
            _markers = GetComponentsInChildren<TrackingMarker>();

            if (leapServiceProvider == null)
                leapServiceProvider = FindObjectOfType<LeapServiceProvider>();
        }

        private void Start()
        {
            if (leapServiceProvider != null)
            {
                Controller controller = leapServiceProvider.GetLeapController();
                controller.FiducialPose -= FiducialCalculateMinMax;
                controller.FiducialPose += FiducialCalculateMinMax;
                controller.FiducialPose -= FiducialSaveFrameData;
                controller.FiducialPose += FiducialSaveFrameData;
            }
            else
            {
                Debug.Log("Unable to begin Fiducial Marker tracking. Cannot connect to a Leap Service Provider.");
            }
        }

        private void FiducialSaveFrameData(object sender, FiducialPoseEventArgs poseEvent)
        {
            if (_fiducialBaseTime == -1)
                _fiducialBaseTime = poseEvent.timestamp;

            //Work out how long it has been since we saw the first fiducial
            float timeSinceFirstFiducial = (poseEvent.timestamp - _fiducialBaseTime) / 1000000;

            //If the current AprilTag frame has advanced, process the previous one
            if (_previousFiducialFrameTime != -1 && timeSinceFirstFiducial != _previousFiducialFrameTime)
            {
                //Get the device position in world space as a LeapTransform to use in future calculations
                _trackerPosWorldspace = leapServiceProvider.DeviceOriginWorldSpace;

                //Find the pose in the prev frame with the lowest error
                FiducialPoseEventArgs lowestErrorPose = null;
                for (int i = 0; i < _poses.Count; i++)
                {
                    if (lowestErrorPose == null)
                    {
                        lowestErrorPose = _poses[i];
                        continue;
                    }
                    if (lowestErrorPose.estimated_error > _poses[i].estimated_error)
                        lowestErrorPose = _poses[i];
                }

                //If we have a pose to use, find the associated marker GameObject and position us
                if (lowestErrorPose != null)
                {
                    TrackingMarker markerObject = _markers.FirstOrDefault(o => o.id == lowestErrorPose.id);
                    if (markerObject != null)
                    {
                        Vector3 desiredPosition = GetMarkerWorldSpacePosition(poseEvent.translation.ToVector3());
                        Quaternion desiredRotation = GetMarkerWorldSpaceRotation(poseEvent.rotation.ToQuaternion());

                        // Reference to the parent and child transforms
                        Transform parentTransform = transform;
                        Transform childTransform = markerObject.transform;

                        // Calculate the world position and rotation needed for the parent
                        Vector3 parentPosition = desiredPosition - parentTransform.TransformVector(childTransform.localPosition);
                        Quaternion parentRotation = desiredRotation * Quaternion.Inverse(childTransform.localRotation);

                        // Apply the calculated position and rotation to the parent
                        parentTransform.position = parentPosition;
                        parentTransform.rotation = parentRotation;
                    }
                }

                //For debugging: lets position every marker to see which we ended up using & how far out the others were relative to that
                for (int i = 0; i < _markers.Length; i++)
                {
                    continue;

                    if (_markers[i] == null)
                        continue;

                    FiducialPoseEventArgs pose = _poses.FirstOrDefault(o => o.id == _markers[i].id);
                    if (pose != null)
                    {
                        _markers[i].transform.position = GetMarkerWorldSpacePosition(pose.translation.ToVector3());
                        _markers[i].transform.rotation = GetMarkerWorldSpaceRotation(pose.rotation.ToQuaternion());
                    }
                    _markers[i].gameObject.SetActive(pose != null);
                }

                //Clear the previous frame data ready to log the current AprilTag frame
                _poses.Clear();
                _fiducialFPS = 1.0f / (timeSinceFirstFiducial - _previousFiducialFrameTime);
            }

            //Store data into the current AprilTag frame
            _poses.Add(poseEvent);
            _previousFiducialFrameTime = timeSinceFirstFiducial;
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Handles.Label(transform.position, "Fiducial FPS: " + _fiducialFPS);
        }
#endif

        #region Error Testing

        private float _maxError = float.NegativeInfinity;
        private float _minError = float.PositiveInfinity;

        private void OnDestroy()
        {
            Debug.Log("MIN Error: " + _minError);
            Debug.Log("MAX Error: " + _maxError);
        }

        private void FiducialCalculateMinMax(object sender, FiducialPoseEventArgs poseEvent)
        {
            if (poseEvent.estimated_error < _minError)
            {
                _minError = poseEvent.estimated_error;
            }
            if (poseEvent.estimated_error > _maxError)
            {
                _maxError = poseEvent.estimated_error;
            }
        }

        #endregion

        #region Utilities

        Vector3 GetMarkerWorldSpacePosition(Vector3 trackedMarkerPosition)
        {
            // Apply the leapToUnityTransform Transform and then apply the trackerPosWorldSpace Transform
            trackedMarkerPosition = CopyFromLeapCExtensions.LeapToUnityTransform.TransformPoint(trackedMarkerPosition);
            trackedMarkerPosition = _trackerPosWorldspace.TransformPoint(trackedMarkerPosition);

            return trackedMarkerPosition;
        }

        Quaternion GetMarkerWorldSpaceRotation(Quaternion trackedMarkerRotation)
        {
            // Apply the leapToUnityTransform Transform and then apply the trackerPosWorldSpace Transform
            trackedMarkerRotation = CopyFromLeapCExtensions.LeapToUnityTransform.TransformQuaternion(trackedMarkerRotation);
            trackedMarkerRotation = _trackerPosWorldspace.TransformQuaternion(trackedMarkerRotation);

            return trackedMarkerRotation;
        }

        #endregion
    }
}