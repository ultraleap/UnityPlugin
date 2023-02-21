using Leap;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Graphs;
using UnityEngine;
using UnityEngine.Rendering;

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
        [SerializeField]
        public bool _careAboutOrientation = false;

        public bool CheckThumb = true;
        public bool CheckIndex = true;
        public bool CheckMiddle = true;
        public bool CheckRing = true;
        public bool CheckPinkie = true;

        [SerializeField] int thumbBonesToMatch = 3;
        [SerializeField] int indexBonesToMatch = 3;
        [SerializeField] int middleBonesToMatch = 3;
        [SerializeField] int ringBonesToMatch = 3;
        [SerializeField] int pinkieBonesToMatch = 3;

        [SerializeField]
        private List<int> fingerIndexesToCheck = new();
        private List<int> bonesPerFingerToMatch = new();

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

        private static float defaultRotation = 15;
        public List<float>[] fingerRotationThresholds = new List<float>[5];

        [Header("Finger Rotational Thresholds")]
        [SerializeField]
        [NamedListAttribute(new string[] { "Metacarpal", "Proximal", "Intermediate", "Distal" })]
        private List<float> ThumbJointRotation = new List<float>() { defaultRotation, defaultRotation, defaultRotation, defaultRotation };
        [SerializeField]
        [NamedListAttribute(new string[] { "Metacarpal", "Proximal", "Intermediate", "Distal" })]
        private List<float> IndexJointRotataion = new List<float>() { defaultRotation, defaultRotation, defaultRotation, defaultRotation };
        [SerializeField]
        [NamedListAttribute(new string[] { "Metacarpal", "Proximal", "Intermediate", "Distal" })]
        private List<float> MiddleJointRotaion = new List<float>() { defaultRotation, defaultRotation, defaultRotation, defaultRotation };
        [SerializeField]
        [NamedListAttribute(new string[] { "Metacarpal", "Proximal", "Intermediate", "Distal" })]
        private List<float> RingJointRotaion = new List<float>() { defaultRotation, defaultRotation, defaultRotation, defaultRotation };
        [SerializeField]
        [NamedListAttribute(new string[] { "Metacarpal", "Proximal", "Intermediate", "Distal" })]
        private List<float> PinkieJointRotaion = new List<float>() { defaultRotation, defaultRotation, defaultRotation, defaultRotation };
        #endregion

        public void SaveHandPose(Hand handToSerialise)
        {
            serialisedHand = handToSerialise;

            ApplyThresholds();
        }

        public float GetBoneRotationthreshold(int fingerNum, int boneNum)
        {
            ApplyThresholds();

            if (fingerRotationThresholds.Count() > 0)
            {
                return fingerRotationThresholds[fingerNum].ElementAt(boneNum);
            }
            else
            {
                return 0f;
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
            if (CheckThumb) { fingerIndexesToCheck.Add(0); }
            if (CheckIndex) { fingerIndexesToCheck.Add(1); }
            if (CheckMiddle) { fingerIndexesToCheck.Add(2); }
            if (CheckRing) { fingerIndexesToCheck.Add(3); }
            if (CheckPinkie) { fingerIndexesToCheck.Add(4); }

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
            ThumbJointRotation = new List<float>() { threshold, threshold, threshold, threshold };
            IndexJointRotataion = new List<float>() { threshold, threshold, threshold, threshold };
            MiddleJointRotaion = new List<float>() { threshold, threshold, threshold, threshold };
            RingJointRotaion = new List<float>() { threshold, threshold, threshold, threshold };
            PinkieJointRotaion = new List<float>() { threshold, threshold, threshold, threshold };

            ApplyThresholds();
        }
    }
}
