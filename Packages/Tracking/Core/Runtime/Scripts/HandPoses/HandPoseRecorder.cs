using System.Collections;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;


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
        private Hand _handToCapture = null;
        /// <summary>
        /// Which hand should be captured?
        /// </summary>
        [SerializeField]
        private Chirality _chirality = Chirality.Left;
        /// <summary>
        /// OPTIONAL. Specify a particular leap provider. If none is selected, the script will automatically find one in the scene.
        /// </summary>
        [SerializeField]
        private LeapProvider _leapProvider = null;
        /// <summary>
        /// Where should the save path be? this will always be in "Assets/..."
        /// When saved, this will create the folder is one does not exist.
        /// </summary>
        [SerializeField]
        private string _savePath = "HandPoses/";


        private Hand hand = new Hand();

        public void SaveCurrentHandPose()
        {
            _handToCapture = _leapProvider.CurrentFrame.GetHand(_chirality);
            if(_handToCapture == null )
            {
                Debug.Log("There is no Ultraleap hand in the scene to capture");
                return;
            }
            hand = hand.CopyFrom(_handToCapture);
            if (hand != null)
            {
                if(hand != null)
                {
                    CreateScriptableObject(_handPoseName, hand);
                }
            }
            else
            {
                Debug.Log("There is no Ultraleap hand in the scene to capture");
            }
        }

        private void Start()
        {
            if (_leapProvider == null)
            {
                _leapProvider = FindObjectOfType<LeapProvider>();
            }
        }

        private void CreateScriptableObject(string handPoseName, Hand handData)
        {
            HandPoseScriptableObject newItem = ScriptableObject.CreateInstance<HandPoseScriptableObject>();
            newItem.name = handPoseName;
            newItem.SaveHandPose(handData);
            if (!Directory.Exists("Assets/" + _savePath))
            {
                Directory.CreateDirectory("Assets/" + _savePath);
            }
            AssetDatabase.CreateAsset(newItem, "Assets/" + _savePath + handPoseName + ".asset");
            AssetDatabase.Refresh();
        }

        [SerializeField]
        Text countdownText = null;

        /// <summary>
        /// How long after pressing the record pose button will the recordere wait before saving the pose (in seconds)
        /// </summary>
        [SerializeField]
        private int countdownInSeconds = 3;

        public void StartSaveCountdown()
        {
            StartCoroutine(Countdown());
        }

        private float currCountdownValue;
        private IEnumerator Countdown()
        {
            currCountdownValue = countdownInSeconds;
            while (currCountdownValue > 0)
            {
                yield return new WaitForSeconds(1.0f);
                currCountdownValue--;
                countdownText.text = currCountdownValue.ToString();
            }

            SaveCurrentHandPose();
            countdownText.text = "Pose Recorded in \n Assets/" + _savePath;
        }

    }
}
