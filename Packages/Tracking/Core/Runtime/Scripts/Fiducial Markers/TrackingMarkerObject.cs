/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2024.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using LeapInternal;

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

        Vector3 targetPos;
        Quaternion targetRot;

        private void Start()
        {
            // Ensure the LeapServiceProvider exists for us to position the device in world space
            if (leapServiceProvider == null)
            {
                leapServiceProvider = FindObjectOfType<LeapServiceProvider>();
            }

            // Listen to events for the new marker poses
            if (leapServiceProvider != null)
            {
                leapServiceProvider.GetLeapController().FiducialPose -= OnFiducialMarkerPose;
                leapServiceProvider.GetLeapController().FiducialPose += OnFiducialMarkerPose;
            }
            else
            {
                Debug.Log("Unable to begin Fiducial Marker tracking. Cannot connect to a Leap Service Provider.");
            }

            if (trackedObject == null)
            {
                trackedObject = transform;
            }

            targetPos = trackedObject.position;
            targetRot = trackedObject.rotation;
        }

        private void OnFiducialMarkerPose(object sender, FiducialPoseEventArgs poseEvent)
        {
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