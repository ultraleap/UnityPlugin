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
        const string THUMBS_UP_FRAME_FILE_NAME = "rightThumbsUp.dat";
        const string FIST_FRAME_FILE_NAME = "rightFist.dat";
        const string HORNS_FRAME_FILE_NAME = "rightHorns.dat";
        const string OK_FRAME_FILE_NAME = "rightOK.dat";
        const string OPEN_PALM_FRAME_FILE_NAME = "leftPalmUp.dat";
        const string POINT_FRAME_FILE_NAME = "rightPoint.dat";


        const string ROOT_FOLDER = "Assets/Samples/XR Hands/1.2.1/HandVisualizer/";
        const string SCENE_NAME = "HandVisualizer";

        [UnityTest]
        public IEnumerator XRHandSubsystemSpecificationWithEnumeratorPasses()
        {
            SetupScene();
            yield return new WaitForSeconds(10);

            List<XRHandSubsystem> xrHandSubsystems = new List<XRHandSubsystem>();
            SubsystemManager.GetSubsystems(xrHandSubsystems);
            XRHandSubsystem leapXRHandSubsystem = new XRHandSubsystem();
            foreach(XRHandSubsystem subsystem in xrHandSubsystems)
            {
                if("UL XR Hands".Equals(subsystem.subsystemDescriptor.id))
                {
                    leapXRHandSubsystem = subsystem;
                }
            }
            LeapXRHandProvider leapXRHandProvider = (LeapXRHandProvider)leapXRHandSubsystem.GetProvider();
            
            GameObject testLeapProviderGO = new GameObject("Test Leap Provider");
            TestLeapProvider testLeapProvider = testLeapProviderGO.AddComponent<TestLeapProvider>();
            LeapXRServiceProvider  leapXRServiceProvider= GameObject.FindAnyObjectByType<LeapXRServiceProvider>();
            leapXRServiceProvider.mainCamera = Camera.main;
            testLeapProvider.inputLeapProvider = leapXRServiceProvider;
            testLeapProvider.FrameFileRepository = FRAME_FILE_REPOSITORY;
            testLeapProvider.FrameFileName = OPEN_PALM_FRAME_FILE_NAME;
            
            leapXRHandProvider.TrackingProvider = testLeapProvider;
            yield return new WaitForSeconds(2);

            Hand leapLeftHand = testLeapProvider.CurrentFrame.GetHand(Chirality.Left);
            Debug.Log(leapLeftHand.Fingers[(int)Finger.FingerType.TYPE_INDEX].Bone(Bone.BoneType.TYPE_DISTAL).NextJoint.ToString());

            Pose xrHandLeftIndexTipPose = new Pose();
            leapXRHandSubsystem.leftHand.GetJoint(XRHandJointID.IndexTip).TryGetPose(out xrHandLeftIndexTipPose);
            Pose xrHandLeftIndexTipPoseTransformed = xrHandLeftIndexTipPose.GetTransformedBy(new Pose(Camera.main.transform.parent.position, Camera.main.transform.parent.transform.rotation));
            Debug.Log(xrHandLeftIndexTipPoseTransformed.position);


            Vector3 xrHandleftIndexTip = GameObject.Find("L_IndexTip").transform.position;
            Debug.Log(xrHandleftIndexTip.ToString());
            yield return new WaitForSeconds(10);
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
    }
}