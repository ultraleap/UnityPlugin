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

        [Tooltip("The Transform to keep aligned to this tracked object")]
        [SerializeField] private Transform _targetObject;

        public Action OnTrackingStart, OnTrackingLost;

        public bool Tracked { get { return _tracked; } }
        private bool _tracked = false;

        private int _framesBeforeLostTracking = 30;
        private int _frameLastTracked;

        private LeapTransform _trackerPosWorldspace;
        private TrackingMarker[] _markers;
        private Transform[] _markerTransforms;
        private Transform _targetTransform;

        private List<FiducialPoseEventArgs> _poses = new List<FiducialPoseEventArgs>();
        private float _previousFiducialFrameTime = -1;

        private float _fiducialBaseTime = -1;
        private float _fiducialFPS = 0;

        private Vector3[] _positions = null;
        private Vector3[] _forwards = null;
        private Vector3[] _ups = null;

        private int _bestBias = 3;

        private void Awake()
        {
            _markers = GetComponentsInChildren<TrackingMarker>();

            _markerTransforms = new Transform[_markers.Length];
            for (int i = 0; i < _markers.Length; i++)
                _markerTransforms[i] = _markers[i].transform;

            if (_leapServiceProvider == null)
                _leapServiceProvider = FindObjectOfType<LeapServiceProvider>();

            _targetTransform = new GameObject("TargetTransform").transform;
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

                if (_poses.Count != 0)
                {
                    _markers.ForEach(o => o?.gameObject?.SetActive(true));

                    //Get the pose with the lowest bias and add it to the pose list multiple times - we want to bias our average towards this
                    _poses = _poses.OrderBy(o => o.estimated_error).ToList();
                    for (int i = 0; i < _bestBias; i++)
                        _poses.Add(_poses[0]);

                    //Transform to every pose
                    _positions = new Vector3[_poses.Count];
                    _forwards = new Vector3[_poses.Count];
                    _ups = new Vector3[_poses.Count];
                    for (int i = 0; i < _poses.Count; i++)
                    {
                        TransformToPose(_poses[i], false);
                        _positions[i] = _targetTransform.transform.position;
                        _forwards[i] = _targetTransform.transform.forward;
                        _ups[i] = _targetTransform.transform.up;
                    }

                    //Take the average position & calculate rotation based on forwards/up
                    Vector3 avgPosition = GetAverageVector3(_positions);
                    Vector3 avgUp = GetAverageVector3(_ups);
                    Vector3 avgForward = GetAverageVector3(_forwards);
                    _targetObject.transform.position = avgPosition;
                    _targetObject.transform.rotation = Quaternion.LookRotation(avgForward, avgUp);

                    //Only enable tracked markers, and update them to their real positions (gives us a nice render)
                    for (int i = 0; i < _markers.Length; i++)
                    {
                        if (_markers[i] == null)
                            continue;
                        _markers[i].DebugText = "";

                        FiducialPoseEventArgs pose = _poses.FirstOrDefault(o => o.id == _markers[i].id);
                        bool hasPose = pose != null;
                        if (hasPose)
                        {
                            _markers[i].transform.position = GetMarkerWorldSpacePosition(pose.translation.ToVector3());
                            _markers[i].transform.rotation = GetMarkerWorldSpaceRotation(pose.rotation.ToQuaternion());
                            _markers[i].DebugText = "Error: " + pose.estimated_error.ToString("F20");
                        }
                        _markers[i].gameObject.SetActive(hasPose);
                        _markers[i].IsTracked = hasPose;
                        _markers[i].IsHighlighted = hasPose && _poses[0] == pose;
                    }
                }
                else
                {
                    _markers.ForEach(o => o?.gameObject?.SetActive(false));
                }

                _poses.Clear();
                _fiducialFPS = 1.0f / (timeSinceFirstFiducial - _previousFiducialFrameTime);
                _frameLastTracked = Time.frameCount;
            }

            //Store data into the current AprilTag frame
            _poses.Add(poseEvent);
            _previousFiducialFrameTime = timeSinceFirstFiducial;
        }

        private void TransformToPose(FiducialPoseEventArgs pose, bool highlight)
        {
            //Position all markers relative to the tracked marker pose by parenting, moving, then unparenting
            TrackingMarker markerObject = _markers.FirstOrDefault(o => o.id == pose.id);
            if (markerObject != null)
            {
                Transform[] prevParents = new Transform[_markers.Length + 1];
                for (int x = 0; x < _markers.Length; x++)
                {
                    if (_markers[x].id == markerObject.id)
                        continue;
                    prevParents[x] = _markers[x].transform.parent;
                    _markers[x].transform.parent = markerObject.transform;
                }
                prevParents[_markers.Length] = _targetTransform.parent;
                _targetTransform.parent = markerObject.transform;

                markerObject.transform.position = GetMarkerWorldSpacePosition(pose.translation.ToVector3());
                markerObject.transform.rotation = GetMarkerWorldSpaceRotation(pose.rotation.ToQuaternion());

                for (int x = 0; x < _markers.Length; x++)
                {
                    _markers[x].IsHighlighted = highlight && _markers[x].id == markerObject.id;
                    if (_markers[x].id == markerObject.id)
                        continue;
                    _markers[x].transform.parent = prevParents[x];
                }
                _targetTransform.parent = prevParents[_markers.Length];
            }
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
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Handles.Label(transform.position, "Fiducial FPS: " + _fiducialFPS);

            if (_positions != null)
            {
                Vector3[] forwards = new Vector3[_positions.Length];
                for (int i = 0; i < _positions.Length; i++)
                {
                    forwards[i] = _positions[i] + (_forwards[i] * 1.0f);

                    Gizmos.color = GetRandomColor();
                    Gizmos.DrawSphere(_positions[i], 0.01f);
                    Gizmos.DrawSphere(forwards[i], 0.01f);
                    Gizmos.DrawLine(_positions[i], forwards[i]);
                }

                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(GetAverageVector3(_positions), 0.1f);
                Gizmos.DrawWireSphere(GetAverageVector3(forwards), 0.1f);
            }
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

        Vector3 GetAverageVector3(Vector3[] v)
        {
            Vector3 sum = Vector3.zero;
            for (int i = 0; i < v.Length; i++)
                sum += v[i];
            return sum / v.Length;
        }

        Color GetRandomColor()
        {
            float red = UnityEngine.Random.Range(0f, 1f);
            float green = UnityEngine.Random.Range(0f, 1f);
            float blue = UnityEngine.Random.Range(0f, 1f);
            return new Color(red, green, blue);
        }

        #endregion
    }
}