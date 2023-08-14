using Leap.Unity;
using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.SubsystemsImplementation.Extensions;
using UnityEngine.TestTools;
using UnityEngine.XR.Hands;

namespace Leap.Testing
{
    public class XRHandSubsystemSpecification
    {
        const string FRAME_ROOT_FOLDER = "Packages/com.ultraleap.tracking/Core/Tests/Runtime/Pose Detection/";

        // Hand tracking test data
        const string FRAME_FILE_REPOSITORY = FRAME_ROOT_FOLDER + "Resources/ReferenceHandFrames/";
        const string OPEN_PALM_FRAME_FILE_NAME = "leftPalmUp.dat";


        const string ROOT_FOLDER = "Assets/Samples/XR Hands/1.2.1/HandVisualizer/";
        const string SCENE_NAME = "HandVisualizer";

        private static TestLeapProvider _testLeapProvider;

        [UnityTest]
        public IEnumerator XRHandSubsystemSpecificationWithEnumeratorPasses()
        {
            SetupScene();
            yield return new WaitForFixedUpdate();
            LeapXRHandProvider leapXRHandProvider = (LeapXRHandProvider)GetXRHandSubsystem().GetProvider();
            
            GameObject testLeapProviderGO = new GameObject("Test Leap Provider");
            testLeapProviderGO.AddComponent<TestLeapProvider>();

            LeapXRServiceProvider  leapXRServiceProvider = GameObject.FindAnyObjectByType<LeapXRServiceProvider>();
            leapXRServiceProvider.mainCamera = Camera.main;

            TestLeapProvider.inputLeapProvider = leapXRServiceProvider;
            FrameDataSource = FRAME_FILE_REPOSITORY;
            CurrentHandFrame = OPEN_PALM_FRAME_FILE_NAME;
            
            leapXRHandProvider.TrackingProvider = TestLeapProvider;
            yield return new WaitForFixedUpdate();

            Hand leapLeftHand = TestLeapProvider.CurrentFrame.GetHand(Chirality.Left);
            Vector3 leapleapLeftHandIndexTip = leapLeftHand.Fingers[(int)Finger.FingerType.TYPE_INDEX].Bone(Bone.BoneType.TYPE_DISTAL).NextJoint;

            Pose xrHandLeftIndexTipPose = new Pose();
            GetXRHandSubsystem().leftHand.GetJoint(XRHandJointID.IndexTip).TryGetPose(out xrHandLeftIndexTipPose);
            Pose xrHandLeftIndexTipPoseTransformed = xrHandLeftIndexTipPose.GetTransformedBy(new Pose(Camera.main.transform.parent.position, Camera.main.transform.parent.transform.rotation));

            Vector3 xrHandleftIndexTip = GameObject.Find("L_IndexTip").transform.position;
            Assert.AreEqual(leapleapLeftHandIndexTip, xrHandLeftIndexTipPoseTransformed.position);
            yield return null;
        }

        /// <summary>
        /// Sets up the test scene for pose detection. We prefer explicitly calling this setup code over using [SetUp] attribute on this method
        /// </summary>
        public void SetupScene()
        {
            foreach (var buildSettingsScene in UnityEditor.EditorBuildSettings.scenes)
            {
                if (buildSettingsScene != null && buildSettingsScene.path.Contains(SCENE_NAME))
                {
                    // Scene is in build settings, so use that
                    SceneManager.LoadScene(SCENE_NAME);
                    return;
                }
            }

            // Scene is not in build settings, let's try to load it live
            string[] availableScenes = Directory.GetFiles(Path.GetFullPath(ROOT_FOLDER), "*.unity", SearchOption.AllDirectories);

            if (availableScenes != null)
            {
                foreach (var scenePath in availableScenes)
                {
                    if (scenePath.Contains(SCENE_NAME))
                    {
                        LoadSceneParameters loadSceneParams = new LoadSceneParameters(LoadSceneMode.Single);
                        EditorSceneManager.LoadSceneInPlayMode(scenePath, loadSceneParams);
                        return;
                    }
                }
            }
        }

        public XRHandSubsystem GetXRHandSubsystem()
        {
            List<XRHandSubsystem> xrHandSubsystems = new List<XRHandSubsystem>();
            SubsystemManager.GetSubsystems(xrHandSubsystems);
            XRHandSubsystem leapXRHandSubsystem = new XRHandSubsystem();
            foreach (XRHandSubsystem subsystem in xrHandSubsystems)
            {
                if ("UL XR Hands".Equals(subsystem.subsystemDescriptor.id))
                {
                    return subsystem;
                }
            }
            return null;
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
        /// Returns the TestLeapProvider in the scene
        /// </summary>
        public static TestLeapProvider TestLeapProvider
        {
            get
            {
                if (_testLeapProvider == null)
                {
                    _testLeapProvider = Object.FindObjectOfType<TestLeapProvider>();
                }

                return _testLeapProvider;
            }
        }
    }
}