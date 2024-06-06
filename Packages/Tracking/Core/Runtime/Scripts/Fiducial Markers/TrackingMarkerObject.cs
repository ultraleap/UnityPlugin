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
        [SerializeField] private LeapServiceProvider _leapServiceProvider;

        [Tooltip("The Transform to keep aligned to the center of this tracked object")]
        [SerializeField] private Transform _targetObject;

        private LeapTransform _trackerPosWorldspace;
        private TrackingMarker[] _markers;
        private Transform[] _markerTransforms;

        private List<FiducialPoseEventArgs> _poses = new List<FiducialPoseEventArgs>();
        private float _previousFiducialFrameTime = -1;
        private int _previousBestFiducialID = -1;

        private float _fiducialBaseTime = -1;
        private float _fiducialFPS = 0;

        private bool _onlyUpdateVisibleMarkers = false;

        private void Awake()
        {
            _markers = GetComponentsInChildren<TrackingMarker>();

            _markerTransforms = new Transform[_markers.Length];
            for (int x = 0; x < _markers.Length; x++)
                _markerTransforms[x] = _markers[x].transform;

            if (_leapServiceProvider == null)
                _leapServiceProvider = FindObjectOfType<LeapServiceProvider>();
        }

        private void Start()
        {
            if (_leapServiceProvider != null)
            {
                Controller controller = _leapServiceProvider.GetLeapController();
                controller.FiducialPose -= CaptureAndProcessFiducialFrames;
                controller.FiducialPose += CaptureAndProcessFiducialFrames;
            }
            else
            {
                Debug.Log("Unable to begin Fiducial Marker tracking. Cannot connect to a Leap Service Provider.");
            }
        }

        private void CaptureAndProcessFiducialFrames(object sender, FiducialPoseEventArgs poseEvent)
        {
            if (_fiducialBaseTime == -1)
                _fiducialBaseTime = poseEvent.timestamp;

            //Work out how long it has been since we saw the first fiducial
            float timeSinceFirstFiducial = (poseEvent.timestamp - _fiducialBaseTime) / 1000000;

            //If the current AprilTag frame has advanced, process the previous one
            if (_previousFiducialFrameTime != -1 && timeSinceFirstFiducial != _previousFiducialFrameTime)
            {
                //Get the device position in world space as a LeapTransform to use in future calculations
                _trackerPosWorldspace = _leapServiceProvider.DeviceOriginWorldSpace;

                if (_onlyUpdateVisibleMarkers)
                {
                    //For testing
                    for (int x = 0; x < _markers.Length; x++)
                    {
                        if (_markers[x] == null)
                            continue;

                        FiducialPoseEventArgs pose = _poses.FirstOrDefault(o => o.id == _markers[x].id);
                        if (pose != null)
                        {
                            _markers[x].transform.position = GetMarkerWorldSpacePosition(pose.translation.ToVector3());
                            _markers[x].transform.rotation = GetMarkerWorldSpaceRotation(pose.rotation.ToQuaternion());
                        }
                        _markers[x].gameObject.SetActive(pose != null);
                    }
                }
                else
                {
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

                    //TODO: add some "smartness" to it, so if the "best" one from prev frame is still highly rated & hasn't moved far (rotation to cam?), just use that instead
                    _poses = _poses.OrderBy(o => o.estimated_error).ToList();
                    FiducialPoseEventArgs previousBest = _poses.FirstOrDefault(o => o.id == _previousBestFiducialID);


                    //TODO: just thought - for barista, we could just run through EVERY tracked marker & calculate the avg transform, then average that
                    //, so basically loop this code for every tracked marker and then take an average of the position
                    //would be high on compute but would work better nice for handoffs, and could add a tiny lerp to that using the delta for april


                    //If we have a pose to use, find the associated marker GameObject
                    if (lowestErrorPose != null)
                    {
                        TrackingMarker markerObject = _markers.FirstOrDefault(o => o.id == lowestErrorPose.id);
                        if (markerObject != null)
                        {
                            //Position all markers relative to the best tracked marker by parenting, moving, then unparenting
                            Transform[] prevParents = new Transform[_markers.Length + 1];
                            for (int x = 0; x < _markers.Length; x++)
                            {
                                if (_markers[x].id == markerObject.id)
                                    continue;
                                prevParents[x] = _markers[x].transform.parent;
                                _markers[x].transform.parent = markerObject.transform;
                            }
                            prevParents[_markers.Length] = _targetObject.parent;
                            _targetObject.parent = markerObject.transform;

                            markerObject.transform.position = GetMarkerWorldSpacePosition(lowestErrorPose.translation.ToVector3());
                            markerObject.transform.rotation = GetMarkerWorldSpaceRotation(lowestErrorPose.rotation.ToQuaternion());

                            for (int x = 0; x < _markers.Length; x++)
                            {
                                _markers[x].IsActiveMarker = _markers[x].id == markerObject.id;
                                if (_markers[x].IsActiveMarker)
                                    continue;
                                _markers[x].transform.parent = prevParents[x];
                            }
                            _targetObject.parent = prevParents[_markers.Length];
                        }
                    }

                    //Update debug "IsTracked" state
                    for (int x = 0; x < _markers.Length; x++)
                    {
                        _markers[x].IsTracked = _poses.FirstOrDefault(o => o.id == _markers[x].id) != null;
                    }
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