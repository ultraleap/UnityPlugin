using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Leap.Unity
{
    public class NamedListAttribute : PropertyAttribute
    {
        public readonly string[] names;
        public NamedListAttribute(string[] names) { this.names = names; }
    }

    [CustomPropertyDrawer(typeof(NamedListAttribute))]
    public class NamedArrayDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect rect, SerializedProperty property, GUIContent label)
        {
            
            try
            {
                int pos = int.Parse(property.propertyPath.Split('[', ']')[1]);
                EditorGUI.PropertyField(rect, property, new GUIContent(((NamedListAttribute)attribute).names[pos]), true);
            }
            catch
            {
                EditorGUI.PropertyField(rect, property, label, true);
            }
        }
    }

    [CreateAssetMenu(fileName = "HandPose", menuName = "ScriptableObjects/HandPose")]
    public class HandPoseScriptableObject : ScriptableObject
    {
        public bool DetectThumb = true;
        public bool DetectIndex = true;
        public bool DetectMiddle = true;
        public bool DetectRing = true;
        public bool DetectPinkie = true;

        [SerializeField] int thumbBonesToMatch = 3;
        [SerializeField] int indexBonesToMatch = 3;
        [SerializeField] int middleBonesToMatch = 3;
        [SerializeField] int ringBonesToMatch = 3;
        [SerializeField] int pinkieBonesToMatch = 3;

        private List<int> fingerIndexesToCheck = new List<int>();
        private List<int> bonesPerFingerToMatch = new List<int>();

        public List<int> GetFingerIndexesToCheck()
        {
            ApplyFingersToUse();
            return fingerIndexesToCheck;
        }
        public List<int> GetNumBonesForMatch()
        {
            ApplyNumBonesToMatch();
            return bonesPerFingerToMatch;
        }
        public int GetNumBonesForMatch(int fingerNum)
        {
            ApplyNumBonesToMatch();
            return bonesPerFingerToMatch[fingerNum];
        }

        [SerializeField]
        private Hand serialisedHand;
        public Hand GetSerializedHand()
        {
            return serialisedHand;
        }

        #region Finger Thresholds

        private static Vector2 defaultRotation = new Vector2(15,15);
        public List<Vector2>[] fingerRotationThresholds = new List<Vector2>[5];

        [Header("Finger Rotational Thresholds")]
        [SerializeField]
        [NamedListAttribute(new string[] { "Metacarpal", "Proximal", "Intermediate", "Distal" })]
        private List<Vector2> ThumbJointRotation = new List<Vector2>() { defaultRotation, defaultRotation, defaultRotation, defaultRotation };

        [SerializeField]
        [NamedListAttribute(new string[] { "Metacarpal", "Proximal", "Intermediate", "Distal" })]
        private List<Vector2> IndexJointRotataion = new List<Vector2>() { defaultRotation, defaultRotation, defaultRotation, defaultRotation };

        [SerializeField]
        [NamedListAttribute(new string[] { "Metacarpal", "Proximal", "Intermediate", "Distal" })]
        private List<Vector2> MiddleJointRotaion = new List<Vector2>() { defaultRotation, defaultRotation, defaultRotation, defaultRotation };

        [SerializeField]
        [NamedListAttribute(new string[] { "Metacarpal", "Proximal", "Intermediate", "Distal" })]
        private List<Vector2> RingJointRotaion = new List<Vector2>() { defaultRotation, defaultRotation, defaultRotation, defaultRotation };

        [SerializeField]
        [NamedListAttribute(new string[] { "Metacarpal", "Proximal", "Intermediate", "Distal" })]
        private List<Vector2> PinkieJointRotaion = new List<Vector2>() { defaultRotation, defaultRotation, defaultRotation, defaultRotation };

        #endregion

        public void SaveHandPose(Hand handToSerialise)
        {
            serialisedHand = handToSerialise;

            ApplyThresholds();
        }

        public Vector2 GetBoneRotationthreshold(int fingerNum, int boneNum)
        {
            ApplyThresholds();

            if (fingerRotationThresholds.Count() > 0)
            {
                return fingerRotationThresholds[fingerNum].ElementAt(boneNum);
                
            }
            else
            {
                return new Vector3 ( 0f, 0f );
            }
        }
        private void ApplyThresholds()
        {
            fingerRotationThresholds[0] = ThumbJointRotation;
            fingerRotationThresholds[1] = IndexJointRotataion;
            fingerRotationThresholds[2] = MiddleJointRotaion;
            fingerRotationThresholds[3] = RingJointRotaion;
            fingerRotationThresholds[4] = PinkieJointRotaion;
        }

        private void OnValidate()
        {
            ApplyFingersToUse();
            ApplyThresholds();
            ApplyNumBonesToMatch();
        }

        private void ApplyFingersToUse()
        {
            fingerIndexesToCheck.Clear();
            if (DetectThumb) { fingerIndexesToCheck.Add(0); }
            if (DetectIndex) { fingerIndexesToCheck.Add(1); }
            if (DetectMiddle) { fingerIndexesToCheck.Add(2); }
            if (DetectRing) { fingerIndexesToCheck.Add(3); }
            if (DetectPinkie) { fingerIndexesToCheck.Add(4); }

        }

        private void ApplyNumBonesToMatch()
        {
            bonesPerFingerToMatch.Clear();
            bonesPerFingerToMatch.Add(thumbBonesToMatch);
            bonesPerFingerToMatch.Add(indexBonesToMatch);
            bonesPerFingerToMatch.Add(middleBonesToMatch);
            bonesPerFingerToMatch.Add(ringBonesToMatch);
            bonesPerFingerToMatch.Add(pinkieBonesToMatch);
        }

        public void SetAllBoneThresholds(float threshold)
        {
            Vector2 thresholdVector = new Vector2(threshold, threshold);

            ThumbJointRotation = new List<Vector2>() { thresholdVector, thresholdVector, thresholdVector, thresholdVector };

            IndexJointRotataion = new List<Vector2>() { thresholdVector, thresholdVector, thresholdVector, thresholdVector };

            MiddleJointRotaion = new List<Vector2>() { thresholdVector, thresholdVector, thresholdVector, thresholdVector };

            RingJointRotaion = new List<Vector2>() { thresholdVector, thresholdVector, thresholdVector, thresholdVector };

            PinkieJointRotaion = new List<Vector2>() { thresholdVector, thresholdVector, thresholdVector, thresholdVector };

            ApplyThresholds();
        }
    }
}
