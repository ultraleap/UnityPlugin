using System.IO;

using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Leap.Unity
{
    public class HandPoseRecorder : MonoBehaviour
    {
        /// <summary>
        /// The name that the pose should have when it is serialised. E.g. "Thumbs Up Left"
        /// </summary>
        [SerializeField]
        private string handPoseName = "New hand pose";

        /// <summary>
        /// Which hand should be captured?
        /// </summary>
        public Chirality handToRecord = Chirality.Left;

        /// <summary>
        /// Specify a leap provider. If none is selected, the script will automatically find one in the scene.
        /// </summary>
        [SerializeField]
        private LeapProvider leapProvider = null;

        /// <summary>
        /// Where should the save path be? this will always be in "Assets/..."
        /// When saved, this will create the folder if one does not exist.
        /// </summary>
        [HideInInspector]
        public string savePath = "HandPoses/";

        private Hand hand = new Hand();

        public System.Action<HandPoseScriptableObject> OnPoseSaved;

        private void Start()
        {
            if (leapProvider == null)
            {
                leapProvider = Hands.Provider;
            }
        }

        private HandPoseScriptableObject CreateScriptableObject(string handPoseName, Hand handData)
        {
#if UNITY_EDITOR
            HandPoseScriptableObject newItem = ScriptableObject.CreateInstance<HandPoseScriptableObject>();
            newItem.name = handPoseName;
            newItem.SaveHandPose(handData);

            if (!Directory.Exists("Assets/" + savePath))
            {
                Directory.CreateDirectory("Assets/" + savePath);
            }

            string fullPath = "Assets/" + savePath + handPoseName + ".asset";

            int fileIterator = 1;
            while (File.Exists(fullPath))
            {
                fullPath = "Assets/" + savePath + handPoseName + " (" + fileIterator + ")" + ".asset";
                fileIterator++;
            }

            AssetDatabase.CreateAsset(newItem, fullPath);
            AssetDatabase.Refresh();

            OnPoseSaved?.Invoke(newItem);

            Debug.Log("New pose saved to: " + fullPath, AssetDatabase.LoadMainAssetAtPath(fullPath));
            return newItem;
#else
            Debug.LogError("Error saving Hand Pose: You can not save Hand Poses in a built application.");
            return null;
#endif
        }

        public HandPoseScriptableObject SaveCurrentHandPose()
        {
            Hand handToCapture = leapProvider.CurrentFrame.GetHand(handToRecord);

            if (handToCapture == null)
            {
                Debug.Log("There is no Ultraleap hand in the scene to capture");
                return null;
            }

            hand = hand.CopyFrom(handToCapture);

            HandPoseScriptableObject savedScriptable = CreateScriptableObject(handPoseName, hand);
            return savedScriptable;
        }
    }
}