using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;


namespace Leap.Unity
{
    public class HandPoseRecorder : MonoBehaviour
    {

        [SerializeField]
        string _handPoseName = "New hand pose";
        [SerializeField]
        HandModelBase handToCapture = null;

        [SerializeField]
        Hand hand = null;

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

        void createScriptableObject(string handPoseName, Hand handData)
        {
            HandPoseScriptableObject newItem = ScriptableObject.CreateInstance<HandPoseScriptableObject>();
            newItem.name = handPoseName;
            newItem.SaveHandPose(handData);
            AssetDatabase.CreateAsset(newItem, "Assets/HandPoses/"+ handPoseName + ".asset");
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
    }
}
