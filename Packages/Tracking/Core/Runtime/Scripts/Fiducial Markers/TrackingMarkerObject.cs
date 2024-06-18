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
using UnityEngine;

namespace Leap.Unity
{
    public class TrackingMarkerObject : MonoBehaviour
    {
        [Tooltip("The source of the tracking data.")]
        [SerializeField] private LeapServiceProvider _leapServiceProvider;

        [Tooltip("The Transform to keep aligned to this tracked object")]
        [SerializeField] private Transform _trackedObject;

        [Tooltip("The amount of damping to apply to the lerp")]
        [SerializeField] private float dampingAmount = 20f;

        [Tooltip("Reduce latancy at the expense of pose fidelity")]
        [SerializeField] private bool _improveSwim = false;

        public Action OnTrackingStart, OnTrackingLost;

        public bool Tracked { get { return _tracked; } }
        private bool _tracked = false;

        private int _framesBeforeLostTracking = 30;
        private int _frameLastTracked;

        private LeapTransform _trackerPosWorldspace;
        private TrackingMarker[] _markers;
        private Transform[] _markerTransforms;

        private List<(FiducialPoseEventArgs PoseEvent, LeapTransform Transform)> _poses = new List<(FiducialPoseEventArgs PoseEvent, LeapTransform Transform)>();
        private float _previousFiducialFrameTime = -1;
        private int _previousBestFiducialID = -1;

        private float _fiducialBaseTime = -1;
        private float _fiducialFPS = 0;

        private Controller _leapController;

        Vector3 targetPos;
        Quaternion targetRot;

        private void Awake()
        {
            _markers = GetComponentsInChildren<TrackingMarker>();

            _markerTransforms = new Transform[_markers.Length];
            for (int i = 0; i < _markers.Length; i++)
                _markerTransforms[i] = _markers[i].transform;

            if (_leapServiceProvider == null)
                _leapServiceProvider = FindObjectOfType<LeapServiceProvider>();
        }

        private void OnEnable()
        {
            if (_leapServiceProvider != null)
            {
                _leapController = _leapServiceProvider.GetLeapController();
                _leapController.FiducialPose += CaptureAndProcessFiducialFrames;
            }
            else
            {
                Debug.Log("Unable to begin Fiducial Marker tracking. Cannot connect to a Leap Service Provider.");
            }

            if (_trackedObject == null)
            {
                _trackedObject = transform;
            }
        }

        private void OnDisable()
        {
            _leapController.FiducialPose -= CaptureAndProcessFiducialFrames;
        }

        private void OnDestroy()
        {
            _leapController.FiducialPose -= CaptureAndProcessFiducialFrames;
        }

        private void CaptureAndProcessFiducialFrames(object sender, FiducialPoseEventArgs poseEvent)
        {
            // We cannot place the marker properly while the ServiceProvider does not exist
            if (_leapServiceProvider == null || _leapServiceProvider.enabled == false)
            {
                return;
            }

            try
            {
                // If this object is destroyed, return
                if (gameObject == null && !ReferenceEquals(gameObject, null))
                {
                    _leapController.FiducialPose -= CaptureAndProcessFiducialFrames;
                    return;
                }
            }
            catch
            {
                // Sometimes things get destroyed too fast and we get a missing reference exception when trying to access the gameobject,
                // This lets us exit that scenario gracefully
                _leapController.FiducialPose -= CaptureAndProcessFiducialFrames;
                return;
            }

            if (_fiducialBaseTime == -1)
                _fiducialBaseTime = poseEvent.timestamp;

            // Work out how long it has been since we saw the first fiducial
            float timeSinceFirstFiducial = (poseEvent.timestamp - _fiducialBaseTime) / 1000000;

            //If the current AprilTag frame has advanced, process the previous one
            if (_previousFiducialFrameTime != -1 && timeSinceFirstFiducial != _previousFiducialFrameTime)
            {
                ProcessMarkerFrame(timeSinceFirstFiducial);
            }

            //Store data into the current AprilTag frame
            if (_markers.Count(m => m.id == poseEvent.id) > 0)
            {
                _poses.Add((poseEvent, _leapServiceProvider.DeviceOriginWorldSpace));
            }

            _previousFiducialFrameTime = timeSinceFirstFiducial;
        }

        private void Update()
        {
            if (Time.frameCount - _frameLastTracked > _framesBeforeLostTracking)
            {
                if (_tracked)
                {
                    _tracked = false;
                    OnTrackingLost?.Invoke();
                }
            }
            else
            {
                if (!_tracked)
                {
                    _tracked = true;
                    OnTrackingStart?.Invoke();
                }
            }

            if (_improveSwim)
            {
                UpdateFiducials();

                _trackedObject.position = targetPos;
                _trackedObject.rotation = targetRot;
            }
            else
            { 
                _trackedObject.position = Vector3.Lerp(_trackedObject.position, targetPos, Time.deltaTime * dampingAmount);
                _trackedObject.rotation = Quaternion.Slerp(_trackedObject.rotation, targetRot, Time.deltaTime * dampingAmount);
            }
        }

        private void UpdateFiducials()
        {
            // We cannot place the marker properly while the ServiceProvider does not exist
            if (_leapServiceProvider == null || _leapServiceProvider.enabled == false)
            {
                return;
            }

            try
            {
                // If this object is destroyed, return
                if (gameObject == null && !ReferenceEquals(gameObject, null))
                {
                    _leapController.FiducialPose -= CaptureAndProcessFiducialFrames;
                    return;
                }
            }
            catch
            {
                // Sometimes things get destroyed too fast and we get a missing reference exception when trying to access the gameobject,
                // This lets us exit that scenario gracefully
                _leapController.FiducialPose -= CaptureAndProcessFiducialFrames;
                return;
            }

            if (_fiducialBaseTime == -1)
                return;

            // Work out how long it has been since we saw the first fiducial
            float timeSinceFirstFiducial = (LeapC.GetNow() -_fiducialBaseTime) / 1000000;

            (FiducialPoseEventArgs PoseEvent, LeapTransform LeapTransform) bestPose = (null, new LeapTransform());

            //If the current AprilTag frame has advanced, process the previous one
            if (_previousFiducialFrameTime != -1 && timeSinceFirstFiducial != _previousFiducialFrameTime)
            {
                bestPose = ProcessMarkerFrame(timeSinceFirstFiducial);
            }

            //Store data into the current AprilTag frame
            if (bestPose.PoseEvent != null && _markers.Count(m => m.id == bestPose.PoseEvent.id) > 0)
            {
                _poses.Add(bestPose);
            }
        }

        private (FiducialPoseEventArgs pose, LeapTransform transform) ProcessMarkerFrame(float timeSinceFirstFiducial)
        {
            (FiducialPoseEventArgs PoseEvent, LeapTransform LeapTransform) bestPose = (null, new LeapTransform());

            //Get the device position in world space as a LeapTransform to use in future calculations
            _trackerPosWorldspace = _leapServiceProvider.DeviceOriginWorldSpace;

            if (_poses.Count != 0)
            {
                _markers.ForEach(o => o?.gameObject?.SetActive(true));

                //Get the pose with the lowest error: if the previous best is still tracked, stick with it

                if (_improveSwim)
                {
                    _poses = _poses.OrderBy(o => o.PoseEvent.timestamp).ToList();
                }
                else
                {
                    _poses = _poses.OrderBy(o => o.PoseEvent.estimated_error).ToList();
                }

                bestPose = _poses[0];
                var lastBest = _poses.FirstOrDefault(o => o.PoseEvent.id == _previousBestFiducialID);

                if (lastBest.PoseEvent != null)
                {
                    bestPose = lastBest;
                }

                _previousBestFiducialID = bestPose.PoseEvent.id;

                TrackingMarker markerObject = _markers?.Where(m => m.id == bestPose.PoseEvent.id).First();

                // Convert the Leap Pose to worldspace Unity units
                Vector3 markerPos = GetMarkerWorldSpacePosition(bestPose.PoseEvent.translation.ToVector3(), bestPose.LeapTransform);
                Quaternion markerRot = GetMarkerWorldSpaceRotation(bestPose.PoseEvent.rotation.ToQuaternion(), bestPose.LeapTransform);

                // Find the offset from the marker to the tracked object, to apply the inverse to the tracked transform
                Vector3 posOffset = _trackedObject.position - markerObject.transform.position;
                Quaternion rotOffset = Quaternion.Inverse(_trackedObject.rotation) * markerObject.transform.rotation;

                targetPos = markerPos + posOffset;
                targetRot = markerRot * Quaternion.Inverse(rotOffset);

                //Only enable tracked markers, and update them to their real positions (gives us a nice render)
                //for (int i = 0; i < _markers.Length; i++)
                //{
                //    if (_markers[i] == null)
                //        continue;
                //    _markers[i].DebugText = "";
                //
                //    FiducialPoseEventArgs pose = _poses.FirstOrDefault(o => o.id == _markers[i].id);
                //    bool hasPose = pose != null;
                //    if (hasPose)
                //    {
                //        _markers[i].transform.position = GetMarkerWorldSpacePosition(pose.translation.ToVector3());
                //        _markers[i].transform.rotation = GetMarkerWorldSpaceRotation(pose.rotation.ToQuaternion());
                //        _markers[i].DebugText = "Error: " + pose.estimated_error.ToString("F20");
                //    }
                //    _markers[i].gameObject.SetActive(hasPose);
                //    _markers[i].IsTracked = hasPose;
                //    _markers[i].IsHighlighted = hasPose && best == pose;
                //}
                _frameLastTracked = Time.frameCount;
            }
            else
            {
                _markers.ForEach(o => o?.gameObject?.SetActive(false));
                
            }

            _poses.Clear();
            _fiducialFPS = 1.0f / (timeSinceFirstFiducial - _previousFiducialFrameTime);
            return bestPose;
        }

        #region Utilities

        Vector3 GetMarkerWorldSpacePosition(Vector3 trackedMarkerPosition, LeapTransform leapTransform)
        {
            // Apply the leapToUnityTransform Transform and then apply the trackerPosWorldSpace Transform
            trackedMarkerPosition = CopyFromLeapCExtensions.LeapToUnityTransform.TransformPoint(trackedMarkerPosition);

            if (_improveSwim)
            {
                trackedMarkerPosition = leapTransform.TransformPoint(trackedMarkerPosition);
            }
            else
            {
                trackedMarkerPosition = _trackerPosWorldspace.TransformPoint(trackedMarkerPosition);
            }


            return trackedMarkerPosition;
        }

        Quaternion GetMarkerWorldSpaceRotation(Quaternion trackedMarkerRotation, LeapTransform leapTransform)
        {
            // Apply the leapToUnityTransform Transform and then apply the trackerPosWorldSpace Transform
            trackedMarkerRotation = CopyFromLeapCExtensions.LeapToUnityTransform.TransformQuaternion(trackedMarkerRotation);

            if (_improveSwim)
            {
                trackedMarkerRotation = leapTransform.TransformQuaternion(trackedMarkerRotation);
            }
            else
            {
                trackedMarkerRotation = _trackerPosWorldspace.TransformQuaternion(trackedMarkerRotation);
            }

            return trackedMarkerRotation;
        }


        #endregion
    }
}