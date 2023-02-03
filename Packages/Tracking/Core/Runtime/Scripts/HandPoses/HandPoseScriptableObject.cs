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

        /// <summary>
        /// Which fingers should be checked when doing pose detection?
        /// </summary>
        public bool CheckThumb = true;
        public bool CheckIndex = true;
        public bool CheckMiddle = true;
        public bool CheckRing = true;
        public bool CheckPinkie = true;

        [SerializeField]
        private List<int> fingerIndexesToCheck = new();

        public List<int> GetFingerIndexesToCheck()
        {
            ApplyFingersToUse();
            return fingerIndexesToCheck;
        }

        [SerializeField]
        private Hand serialisedHand;

        public Hand GetSerializedHand()
        {
            return serialisedHand;
        }

        private static float JointRot = 15;

        #region Finger Thresholds
        public List<float>[] fingerRotationThresholds = new List<float>[5];

        [Header("Finger Rotational Thresholds")]
        [SerializeField]
        [NamedListAttribute(new string[] { "Metacarpal", "Proximal", "Intermediate", "Distal" })]
        private List<float> ThumbJointRotation = new List<float>() { JointRot, JointRot, JointRot, JointRot };
        [SerializeField]
        [NamedListAttribute(new string[] { "Metacarpal", "Proximal", "Intermediate", "Distal" })]
        private List<float> IndexJointRotataion = new List<float>() { JointRot, JointRot, JointRot, JointRot };
        [SerializeField]
        [NamedListAttribute(new string[] { "Metacarpal", "Proximal", "Intermediate", "Distal" })]
        private List<float> MiddleJointRotaion = new List<float>() { JointRot, JointRot, JointRot, JointRot };
        [SerializeField]
        [NamedListAttribute(new string[] { "Metacarpal", "Proximal", "Intermediate", "Distal" })]
        private List<float> RingJointRotaion = new List<float>() { JointRot, JointRot, JointRot, JointRot };
        [SerializeField]
        [NamedListAttribute(new string[] { "Metacarpal", "Proximal", "Intermediate", "Distal" })]
        private List<float> PinkieJointRotaion = new List<float>() { JointRot, JointRot, JointRot, JointRot };
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
    }
}
