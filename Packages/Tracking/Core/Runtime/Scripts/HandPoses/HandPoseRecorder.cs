using System.Collections;
using System.Collections.Generic;
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
        private string _handPoseName = "New hand pose";
        /// <summary>
        /// The Leap hand which should be used to capture the pose. 
        /// This can be any Leap hand that inherits from hand model base.
        /// </summary>
        [SerializeField]
        private HandModelBase handToCapture = null;

        
        private Hand hand = new();

        public void SaveCurrentHandPose()
        {
            hand = hand.CopyFrom(handToCapture.GetLeapHand());
            if (hand != null)
            {
                if(hand != null)
                {
                    createScriptableObject(_handPoseName, hand);
                }
            }
            else
            {
                Debug.Log("There is no Ultraleap hand in the scene to capture");
            }
        }

        private void createScriptableObject(string handPoseName, Hand handData)
        {
            HandPoseScriptableObject newItem = ScriptableObject.CreateInstance<HandPoseScriptableObject>();
            newItem.name = handPoseName;
            newItem.SaveHandPose(handData);
            if (!Directory.Exists("Assets/HandPoses/"))
            {
                Directory.CreateDirectory("Assets/HandPoses/");
            }
            AssetDatabase.CreateAsset(newItem, "Assets/HandPoses/"+ handPoseName + ".asset");
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
    }
}
