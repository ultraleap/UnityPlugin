using Leap.Unity;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Testing
{
    public class HandPoseDetectorTestScene
    {
        const string POSE_DETECTOR_NAME = "Pose Detector";
        const string TEST_LEAP_PROVIDER_NAME = "Test Leap Provider";
        const string POSE_EVENT_LISTENER_NAME = "Pose Event Listener";

        private static HandPoseDetector _handPoseDetector;
        private static TestLeapProvider _testLeapProvider;
        private static PoseEventListener _poseEventListener;

        /// <summary>
        /// Sets the pose to detect on the scene
        /// </summary>
        public static HandPoseScriptableObject PoseToDetect
        {
            set
            {
                if (HandPoseDetector != null)
                {
                    HandPoseDetector.SetPosesToDetect(new List<HandPoseScriptableObject> { value });
                }
            }
        }

        /// <summary>
        /// Sets the  file name for the current tracking hand frame data. The FrameDataSource should be set to the folder that contains the file
        /// </summary>
        public static string CurrentHandFrame
        {
            set
            {
                if (FrameDataSource != null)
                {
                    TestLeapProvider.FrameFileName = value;
                }
            }
        }

        /// <summary>
        /// Sets the location for the frame data
        /// </summary>
        public static string FrameDataSource
        {
            get
            {
                if (TestLeapProvider != null)
                {
                    return TestLeapProvider.FrameFileRepository;
                }

                return string.Empty;
            }

            set
            {
                if (TestLeapProvider != null)
                {
                    TestLeapProvider.FrameFileRepository = value;
                }
            }
        }

        /// <summary>
        /// Returns the HandPoseDetector in the scene
        /// </summary>
        public static HandPoseDetector HandPoseDetector
        {
            get
            {
                if (_handPoseDetector == null)
                {
                    _handPoseDetector = GetComponentFromGameObject<HandPoseDetector>(POSE_DETECTOR_NAME);
                }

                return _handPoseDetector;
            }
        }

        /// <summary>
        /// Returns the TestLeapProvider in the scene
        /// </summary>
        public static TestLeapProvider TestLeapProvider
        {
            get
            {
                if (_testLeapProvider == null)
                {
                    _testLeapProvider = GetComponentFromGameObject<TestLeapProvider>(TEST_LEAP_PROVIDER_NAME);
                }

                return _testLeapProvider;
            }
        }

        /// <summary>
        /// Returns the PoseEventListener in the scene
        /// </summary>
        public static PoseEventListener PoseEventListener
        {
            get
            {
                if (_poseEventListener == null)
                {
                    _poseEventListener = GetComponentFromGameObject<PoseEventListener>(POSE_EVENT_LISTENER_NAME);
                }

                return _poseEventListener;
            }
        }

        private static T GetComponentFromGameObject<T>(string gameObjectName)
        {
            return GameObject.Find(gameObjectName).GetComponent<T>();
        }
    }
}
