using System.IO;
using UnityEditor;
using UnityEngine;

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
        [SerializeField]
        private Chirality handToRecord = Chirality.Left;

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
        public string SavePath = "HandPoses/";

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
            HandPoseScriptableObject newItem = ScriptableObject.CreateInstance<HandPoseScriptableObject>();
            newItem.name = handPoseName;
            newItem.SaveHandPose(handData);

            if (!Directory.Exists("Assets/" + SavePath))
            {
                Directory.CreateDirectory("Assets/" + SavePath);
            }

            string fullPath = "Assets/" + SavePath + handPoseName + ".asset";

            int fileIterator = 1;
            while (File.Exists(fullPath))
            {
                fullPath = "Assets/" + SavePath + handPoseName + " (" + fileIterator + ")" + ".asset";
                fileIterator++;
            }

            AssetDatabase.CreateAsset(newItem, fullPath);
            AssetDatabase.Refresh();

            OnPoseSaved?.Invoke(newItem);

            Debug.Log("New pose saved to: " + fullPath, AssetDatabase.LoadMainAssetAtPath(fullPath));
            return newItem;
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