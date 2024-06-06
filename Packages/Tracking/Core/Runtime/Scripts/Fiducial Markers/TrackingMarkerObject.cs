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
            // Ensure the LeapServiceProvider exists for us to position the device in world space
            if (leapServiceProvider == null)
                leapServiceProvider = FindObjectOfType<LeapServiceProvider>();

            // Listen to events for the new marker poses
            if (leapServiceProvider != null)
            {
                Controller controller = leapServiceProvider.GetLeapController();
                controller.FiducialPose -= OnFiducialMarkerPose;
                controller.FiducialPose += OnFiducialMarkerPose;
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

        private float _latestWrittenTimestamp = -1;
        private Dictionary<float, List<FiducialPoseEventArgs>> _fiducialFrames = new Dictionary<float, List<FiducialPoseEventArgs>>();

        private void OnFiducialMarkerPose(object sender, FiducialPoseEventArgs poseEvent)
        {

            TrackingMarker m = markers.FirstOrDefault(o => o.id == poseEvent.id);
            if (m == null)
                return;

            m.FiducialPose = poseEvent;

            trackerPosWorldSpace = leapServiceProvider.DeviceOriginWorldSpace;
            m.transform.position = GetMarkerWorldSpacePosition(poseEvent.translation.ToVector3());
            m.transform.rotation = GetMarkerWorldSpaceRotation(poseEvent.rotation.ToQuaternion());

            return;


            Debug.Log((poseEvent.timestamp* 100000) + " -> " + poseEvent.id);

            if (!_fiducialFrames.ContainsKey(poseEvent.timestamp))
            {
                _fiducialFrames.Add(poseEvent.timestamp, new List<FiducialPoseEventArgs>());
            }
            _fiducialFrames[poseEvent.timestamp].Add(poseEvent);

            if (_latestWrittenTimestamp > poseEvent.timestamp)
            {
                Debug.LogWarning("Prev timestamp was greater than this one!");
            }

            //We've started a new frame. Consume the last one.
            if (_latestWrittenTimestamp < poseEvent.timestamp)
            {
                List<FiducialPoseEventArgs> poses = _fiducialFrames[poseEvent.timestamp];

                for (int i = 0; i < poses.Count; i++)
                {
                    TrackingMarker markerObject = markers.FirstOrDefault(o => o.id == poses[i].id);
                    if (markerObject == null)
                        continue;

                    markerObject.FiducialPose = poses[i];

                    trackerPosWorldSpace = leapServiceProvider.DeviceOriginWorldSpace;
                    markerObject.transform.position = GetMarkerWorldSpacePosition(poses[i].translation.ToVector3());
                    markerObject.transform.rotation = GetMarkerWorldSpaceRotation(poses[i].rotation.ToQuaternion());
                }

                if (poses.Count > 1)
                {
                    Debug.LogWarning("Multiple poses in this timestamp: " + poses.Count);
                }
            }

            _latestWrittenTimestamp = poseEvent.timestamp;
            return;



            /*
            // We cannot place the marker properly while the ServiceProvider does not exist
            if (leapServiceProvider == null || leapServiceProvider.enabled == false)
            {
                return;
            }

            // Find a matching TrackingMarker to the one from the event by matching ids
            TrackingMarker markerObject = null;

            for (int i = 0; i < markers.Length; i++)
            {
                if (markers[i] != null && markers[i].id == poseEvent.id)
                {
                    markerObject = markers[i];
                    break;
                }
            }

            if (markerObject == null)
            {
                // We don't have this marker
                return;
            }

            // Get the device position in world space as a LeapTransform to use in future calculations
            trackerPosWorldSpace = leapServiceProvider.DeviceOriginWorldSpace;

            // Convert the Leap Pose to worldspace Unity units
            Vector3 markerPos = GetMarkerWorldSpacePosition(poseEvent.translation.ToVector3());
            Quaternion markerRot = GetMarkerWorldSpaceRotation(poseEvent.rotation.ToQuaternion());

            // Find the offset from the marker to the tracked object, to apply the inverse to the tracked transform
            Vector3 posOffset = trackedObject.position - markerObject.transform.position;
            Quaternion rotOffset = Quaternion.Inverse(trackedObject.rotation) * markerObject.transform.rotation;

            targetPos = markerPos + posOffset;
            targetRot = markerRot * Quaternion.Inverse(rotOffset);
            */
        }


        private void Update()
        {
            trackedObject.position = targetPos;
            trackedObject.rotation = targetRot;
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