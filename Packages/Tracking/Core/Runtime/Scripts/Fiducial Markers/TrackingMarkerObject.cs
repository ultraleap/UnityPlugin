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
        [Tooltip("The source of the tracking data." +
            "\n\nUsed to gather the updated Marker positions and to position the Tracked Object in world space")]
        public LeapServiceProvider leapServiceProvider;

        [Tooltip("The virtual object that the markers are associated with." +
            "\n\nIf not set, this Component's Transform will be used")]
        public Transform trackedObject;

        [Tooltip("A collections of markers positioned relative to the Tracked Object." +
            "\n\nNote: These should be children of the Tracked Object in the hierarchy")]
        public TrackingMarker[] markers;

        LeapTransform trackerPosWorldSpace;

        public Vector3 TargetPos { get { return targetPos; } }
        public Quaternion TargetRot { get { return targetRot; } }

        private Vector3 targetPos;
        private Quaternion targetRot;

        private void Start()
        {
            if (leapServiceProvider == null)
                leapServiceProvider = FindObjectOfType<LeapServiceProvider>();

            if (leapServiceProvider != null)
            {
                Controller controller = leapServiceProvider.GetLeapController();
                controller.FiducialPose += FiducialCalculateMinMax;
                //controller.FiducialPose += FiducialOutputAllMarkers;
                controller.FiducialPose += FiducialSaveFrameData;
            }
            else
            {
                Debug.Log("Unable to begin Fiducial Marker tracking. Cannot connect to a Leap Service Provider.");
            }

            if (trackedObject == null)
                trackedObject = transform;

            targetPos = trackedObject.position;
            targetRot = trackedObject.rotation;
        }

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

        private void FiducialOutputAllMarkers(object sender, FiducialPoseEventArgs poseEvent)
        {
            TrackingMarker m = markers.FirstOrDefault(o => o.id == poseEvent.id);
            if (m == null)
                return;

            m.FiducialPose = poseEvent;

            trackerPosWorldSpace = leapServiceProvider.DeviceOriginWorldSpace;
            m.transform.position = GetMarkerWorldSpacePosition(poseEvent.translation.ToVector3());
            m.transform.rotation = GetMarkerWorldSpaceRotation(poseEvent.rotation.ToQuaternion());
        }

        private List<FiducialPoseEventArgs> _poses = new List<FiducialPoseEventArgs>();
        private float _previousFiducialFrameTime = -1;

        private float _fiducialBaseTime = -1;

        private void FiducialSaveFrameData(object sender, FiducialPoseEventArgs poseEvent)
        {
            if (_fiducialBaseTime == -1)
                _fiducialBaseTime = poseEvent.timestamp;

            //
            //      NOTE:
            //
            //      this logic assumes two things:
            //        - that you don't get multiple entries of the same marker in the same AprilTag frame
            //        - that you don't get old AprilTag frames through alongside new ones (causing the frame time differ down rather than up)
            //
            //      those assumptions have been made because on my setup i've not had them happen, and sense would tell you that they wouldn't
            //      ... but i'm leaving this comment for the future people fixing issues when they inevitably do
            //

            //Work out how long it has been since we saw the first fiducial
            float timeSinceFirstFiducial = (poseEvent.timestamp - _fiducialBaseTime) / 1000000;

            //If the current AprilTag frame has advanced, process the previous one
            if (_previousFiducialFrameTime != -1 && timeSinceFirstFiducial != _previousFiducialFrameTime)
            {
                //Get the device position in world space as a LeapTransform to use in future calculations
                trackerPosWorldSpace = leapServiceProvider.DeviceOriginWorldSpace;

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
                    TrackingMarker markerObject = markers.FirstOrDefault(o => o.id == lowestErrorPose.id);
                    if (markerObject != null)
                    {
                        //Convert the Leap Pose to worldspace Unity units
                        Vector3 markerPos = GetMarkerWorldSpacePosition(poseEvent.translation.ToVector3());
                        Quaternion markerRot = GetMarkerWorldSpaceRotation(poseEvent.rotation.ToQuaternion());

                        //Find the offset from the marker to the tracked object, to apply the inverse to the tracked transform
                        Vector3 posOffset = trackedObject.position - markerObject.transform.position;
                        Quaternion rotOffset = Quaternion.Inverse(trackedObject.rotation) * markerObject.transform.rotation;

                        //Position the whole object
                        this.transform.position = markerPos + posOffset;
                        this.transform.rotation = markerRot * Quaternion.Inverse(rotOffset);
                    }
                }

                //Now, for debugging, lets position every marker to see which we ended up using & how far out the others were relative to that
                for (int i = 0; i < markers.Length; i++)
                {
                    if (markers[i] == null)
                        continue;

                    FiducialPoseEventArgs pose = _poses.FirstOrDefault(o => o.id == markers[i].id);
                    markers[i].FiducialPose = pose;

                    if (pose != null)
                    {
                        markers[i].transform.position = GetMarkerWorldSpacePosition(pose.translation.ToVector3());
                        markers[i].transform.rotation = GetMarkerWorldSpaceRotation(pose.rotation.ToQuaternion());
                    }
                }

                //Clear the previous frame data ready to log the current AprilTag frame
                _poses.Clear();
            }

            //Store data into the current AprilTag frame
            _poses.Add(poseEvent);
            _previousFiducialFrameTime = timeSinceFirstFiducial;
        }

        #region Utilities

        Vector3 GetMarkerWorldSpacePosition(Vector3 trackedMarkerPosition)
        {
            // Apply the leapToUnityTransform Transform and then apply the trackerPosWorldSpace Transform
            trackedMarkerPosition = CopyFromLeapCExtensions.LeapToUnityTransform.TransformPoint(trackedMarkerPosition);
            trackedMarkerPosition = trackerPosWorldSpace.TransformPoint(trackedMarkerPosition);

            return trackedMarkerPosition;
        }

        Quaternion GetMarkerWorldSpaceRotation(Quaternion trackedMarkerRotation)
        {
            // Apply the leapToUnityTransform Transform and then apply the trackerPosWorldSpace Transform
            trackedMarkerRotation = CopyFromLeapCExtensions.LeapToUnityTransform.TransformQuaternion(trackedMarkerRotation);
            trackedMarkerRotation = trackerPosWorldSpace.TransformQuaternion(trackedMarkerRotation);

            return trackedMarkerRotation;
        }

        #endregion
    }
}