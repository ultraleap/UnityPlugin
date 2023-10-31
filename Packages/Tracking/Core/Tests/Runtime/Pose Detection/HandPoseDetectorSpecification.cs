using Leap.Unity;
using NUnit.Framework;
using System.Collections;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Leap.Testing
{
    /// <summary>
    /// Checks that hand pose detection works as expected, using reference (hand tracking) frame data provided by a TestLeapProvider setup in the scene
    /// </summary>
    public class HandPoseDetectorSpecification
    {
        #region Test settings
        const int RETRY_COUNT = 3;

        const string ROOT_FOLDER = "Packages/com.ultraleap.tracking/Core/Tests/Runtime/Pose Detection/";
        const string SCENE_NAME = "HandPoseDetectorTestScene";

        // Poses
        const string POSE_ASSET_DIR = "Packages/com.ultraleap.tracking/Core/Runtime/Prefabs/Pose Detection/HandPoses/";
        const string THUMBS_UP_ASSET_FILE_NAME = "Thumbs Up.asset";
        const string FIST_ASSET_FILE_NAME = "Fist.asset";
        const string HORNS_ASSET_FILE_NAME = "Horns.asset";
        const string OK_ASSET_FILE_NAME = "OK.asset";
        const string OPEN_PALM_ASSET_FILE_NAME = "Open Palm.asset";
        const string POINT_ASSET_FILE_NAME = "Point.asset";

        // Hand tracking test data
        const string FRAME_FILE_REPOSITORY = ROOT_FOLDER + "Resources/ReferenceHandFrames/";
        const string THUMBS_UP_FRAME_FILE_NAME = "rightThumbsUp.dat";
        const string FIST_FRAME_FILE_NAME = "rightFist.dat";
        const string HORNS_FRAME_FILE_NAME = "rightHorns.dat";
        const string OK_FRAME_FILE_NAME = "rightOK.dat";
        const string OPEN_PALM_FRAME_FILE_NAME = "leftPalmUp.dat";
        const string POINT_FRAME_FILE_NAME = "rightPoint.dat";

        /// <summary>
        /// A collection of (string) references to test poses and tracking frames that should trigger the pose detection
        /// </summary>
        static (string TargetPoseAsset, string MatchingHandFrame)[] PosesAndMatchingTestFrames = new (string TargetPoseAsset, string MatchingHandFrame)[]
        {
            (THUMBS_UP_ASSET_FILE_NAME, THUMBS_UP_FRAME_FILE_NAME),
            (FIST_ASSET_FILE_NAME, FIST_FRAME_FILE_NAME),
            (HORNS_ASSET_FILE_NAME, HORNS_FRAME_FILE_NAME),
            (OK_ASSET_FILE_NAME, OK_FRAME_FILE_NAME),
            (OPEN_PALM_ASSET_FILE_NAME, OPEN_PALM_FRAME_FILE_NAME),
            (POINT_ASSET_FILE_NAME, POINT_FRAME_FILE_NAME)
        };

        /// <summary>
        /// A collection of (string) references to test poses and tracking frames that should not trigger the pose detection
        /// </summary>
        static (string TargetPoseAsset, string NonMatchingHandFrame)[] PosesAndNotMatchingPoseTestFrames = new (string TargetPoseAsset, string NonMatchingHandFrame)[]
        {
            (THUMBS_UP_ASSET_FILE_NAME, OPEN_PALM_FRAME_FILE_NAME),
            (FIST_ASSET_FILE_NAME, OPEN_PALM_FRAME_FILE_NAME),
            (HORNS_ASSET_FILE_NAME, OPEN_PALM_FRAME_FILE_NAME),
            (OK_ASSET_FILE_NAME, OPEN_PALM_FRAME_FILE_NAME),
            (OPEN_PALM_ASSET_FILE_NAME, THUMBS_UP_FRAME_FILE_NAME),
            (POINT_ASSET_FILE_NAME, OPEN_PALM_FRAME_FILE_NAME)
        };

        /// <summary>
        /// A collection of (string) references to test poses, a tracking frame that will trigger the pose and a tracking frame that does not match the pose. Used to check pose detection changes state
        /// when tracking frames change
        /// </summary>
        static (string TargetPoseAsset, string MatchingHandFrame, string NonMatchingHandFrame)[] OnPoseLostTestData = new (string TargetPoseAsset, string MatchingHandFrame, string NonMatchingHandFrame)[]
        {
            (THUMBS_UP_ASSET_FILE_NAME, THUMBS_UP_FRAME_FILE_NAME, OPEN_PALM_FRAME_FILE_NAME)
        };

        #endregion

        [Test]
        public void GivenAPoseDetector_GetAxis_ReturnsExpectedAxisToFace()
        {
            GameObject gameObject = new GameObject();
            HandPoseDetector handPoseDetector = gameObject.AddComponent<HandPoseDetector>();

            Assert.AreEqual(handPoseDetector.GetAxis(HandPoseDetector.AxisToFace.Back), Vector3.back);
            Assert.AreEqual(handPoseDetector.GetAxis(HandPoseDetector.AxisToFace.Forward), Vector3.forward);
            Assert.AreEqual(handPoseDetector.GetAxis(HandPoseDetector.AxisToFace.Up), Vector3.up);
            Assert.AreEqual(handPoseDetector.GetAxis(HandPoseDetector.AxisToFace.Down), Vector3.down);
            Assert.AreEqual(handPoseDetector.GetAxis(HandPoseDetector.AxisToFace.Right), Vector3.right);
            Assert.AreEqual(handPoseDetector.GetAxis(HandPoseDetector.AxisToFace.Left), Vector3.left);
        }

        [UnityTest]
        [Retry(RETRY_COUNT)]
        public IEnumerator GivenAPoseListener_PoseToDetect_AndMatchingHandFrame_PoseDetected_ReturnsTrue(
            [ValueSource("PosesAndMatchingTestFrames")] (string TargetPoseAsset, string MatchingHandFrameForPose) testData)
        {
            LogTestData(testData);
            SetupScene();
            yield return new WaitForFixedUpdate();

            HandPoseDetectorTestScene.FrameDataSource = FRAME_FILE_REPOSITORY;
            HandPoseDetectorTestScene.PoseToDetect = LoadPoseTarget(testData.TargetPoseAsset);
            HandPoseDetectorTestScene.CurrentHandFrame = testData.MatchingHandFrameForPose;
            yield return new WaitForFixedUpdate();

            Assert.IsTrue(HandPoseDetectorTestScene.PoseEventListener.poseDetected);
        }

        [UnityTest]
        [Retry(RETRY_COUNT)]
        public IEnumerator WhenAHandFrameChangesAfterAPoseIsDetected_poseLostOnThePoseDetector_BecomesFalse(
            [ValueSource("OnPoseLostTestData")] (string TargetPoseAsset, string MatchingHandFrameForPose, string NonMatchingHandFrame) testData)
        {
            LogTestData(testData);
            SetupScene();
            yield return new WaitForFixedUpdate();

            HandPoseDetectorTestScene.FrameDataSource = FRAME_FILE_REPOSITORY;
            HandPoseDetectorTestScene.PoseToDetect = LoadPoseTarget(testData.TargetPoseAsset);
            HandPoseDetectorTestScene.CurrentHandFrame = testData.MatchingHandFrameForPose;
            yield return new WaitForFixedUpdate();

            Assert.IsFalse(HandPoseDetectorTestScene.PoseEventListener.poseLost);

            HandPoseDetectorTestScene.CurrentHandFrame = testData.NonMatchingHandFrame;
            yield return new WaitForFixedUpdate();

            Assert.IsTrue(HandPoseDetectorTestScene.PoseEventListener.poseLost);
        }

        [UnityTest]
        [Retry(RETRY_COUNT)]
        public IEnumerator GivenAPoseToDetectAndMatchingHandFrame_IsPoseCurrentlyDetected_ReturnsTrue(
            [ValueSource("PosesAndMatchingTestFrames")] (string TargetPoseAsset, string MatchingHandFrameForPose) testData)
        {
            LogTestData(testData);
            SetupScene();
            yield return new WaitForFixedUpdate();

            HandPoseDetectorTestScene.FrameDataSource = FRAME_FILE_REPOSITORY;
            HandPoseDetectorTestScene.PoseToDetect = LoadPoseTarget(testData.TargetPoseAsset);
            HandPoseDetectorTestScene.CurrentHandFrame = testData.MatchingHandFrameForPose;
            yield return new WaitForFixedUpdate();

            Assert.IsTrue(HandPoseDetectorTestScene.HandPoseDetector.IsPoseCurrentlyDetected());
        }

        [UnityTest]
        [Retry(RETRY_COUNT)]
        public IEnumerator GivenAPoseToDetectAndNonMatchingHandFrame_IsPoseCurrentlyDetected_ReturnsFalse(
           [ValueSource("PosesAndNotMatchingPoseTestFrames")] (string TargetPoseAsset, string NonMatchingHandFrameForPose) testData)
        {
            LogTestData(testData);
            SetupScene();
            yield return new WaitForFixedUpdate();

            HandPoseDetectorTestScene.FrameDataSource = FRAME_FILE_REPOSITORY;
            HandPoseDetectorTestScene.PoseToDetect = LoadPoseTarget(testData.TargetPoseAsset);
            HandPoseDetectorTestScene.CurrentHandFrame = testData.NonMatchingHandFrameForPose;
            yield return new WaitForFixedUpdate();

            Assert.IsFalse(HandPoseDetectorTestScene.HandPoseDetector.IsPoseCurrentlyDetected());
        }

        [UnityTest]
        [Retry(RETRY_COUNT)]
        public IEnumerator GivenAPoseDetector_WhenAHandFrameChangesAfterAPoseIsDetected_IsPoseCurrentlyDetected_BecomesFalse(
            [ValueSource("OnPoseLostTestData")] (string TargetPoseAsset, string MatchingHandFrameForPose, string NonMatchingHandFrame) testData)
        {
            LogTestData(testData);
            SetupScene();
            yield return new WaitForFixedUpdate();

            HandPoseDetectorTestScene.FrameDataSource = FRAME_FILE_REPOSITORY;
            HandPoseDetectorTestScene.PoseToDetect = LoadPoseTarget(testData.TargetPoseAsset);
            HandPoseDetectorTestScene.CurrentHandFrame = testData.MatchingHandFrameForPose;
            yield return new WaitForFixedUpdate();

            HandPoseDetectorTestScene.CurrentHandFrame = testData.NonMatchingHandFrame;
            yield return new WaitForFixedUpdate();

            Assert.IsFalse(HandPoseDetectorTestScene.HandPoseDetector.IsPoseCurrentlyDetected());
        }

        #region Helper methods

        /// <summary>
        /// Sets up the test scene for pose detection. We prefer explicitly calling this setup code over using [SetUp] attribute on this method
        /// </summary>
        public void SetupScene()
        {
            foreach (var buildSettingsScene in EditorBuildSettings.scenes)
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

        private HandPoseScriptableObject LoadPoseTarget(string poseDataPath)
        {
            return AssetDatabase.LoadAssetAtPath<HandPoseScriptableObject>(Path.Combine(POSE_ASSET_DIR + poseDataPath));
        }

        private void LogTestData((string TargetPoseAsset, string MatchingHandFrame) testData)
        {
            Debug.Log($"The target pose for the test is {testData.TargetPoseAsset}, and the current tracking (test) frame is {testData.MatchingHandFrame}");
        }

        private void LogTestData((string TargetPoseAsset, string MatchingHandFrame, string NonMatchingHandFrame) testData)
        {
            Debug.Log($"The target pose is {testData.TargetPoseAsset}. The test tracking frame will change from the matching frame {testData.MatchingHandFrame} to {testData.NonMatchingHandFrame}");
        }

        #endregion
    }
}