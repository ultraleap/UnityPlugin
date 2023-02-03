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
        private Hand serialisedHand;
        public Hand GetSerializedHand()
        {
            return serialisedHand;
        }

        private static float JointRot = 15;
        private List<float>[] thresholdFingers = new List<float>[5];

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

        public void SaveHandPose(Hand handToSerialise)
        {
            serialisedHand = handToSerialise;

            ApplyThresholds();
        }

        private void ApplyThresholds()
        {
            thresholdFingers[0] = ThumbJointRotation;
            thresholdFingers[1] = IndexJointRotataion;
            thresholdFingers[2] = MiddleJointRotaion;
            thresholdFingers[3] = RingJointRotaion;
            thresholdFingers[4] = PinkieJointRotaion;
        }

        public float GetBoneRotation(int fingerNum, int boneNum)
        {
            ApplyThresholds();

            if (thresholdFingers.Count() > 0)
            {
                return thresholdFingers[fingerNum].ElementAt(boneNum);
            }
            else
            {
                return 0f;
            }
        }
    }
}
