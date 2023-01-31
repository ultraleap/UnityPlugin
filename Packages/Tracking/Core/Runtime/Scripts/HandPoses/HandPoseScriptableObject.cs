using Leap;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
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
        //private BoundHand serialisedBoundHand;
        //public BoundHand GetSerializedBoundHand()
        //{
        //    return serialisedBoundHand;
        //}



        private static Vector3 JointPos = new Vector3(0, 0, 0);
        private static Quaternion JointRot = new Quaternion(0, 0, 0, 0);

        [Header("Finger Positional Thresholds")]

        [SerializeField]
        [NamedListAttribute(new string[] { "Metacarpal", "Proximal", "Intermediate", "Distal" })]
        private List<Vector3> ThumbJointPosition = new List<Vector3>() { JointPos, JointPos, JointPos, JointPos };
        [SerializeField]
        [NamedListAttribute(new string[] { "Metacarpal", "Proximal", "Intermediate", "Distal" })]
        private List<Vector3> IndexJointPosition = new List<Vector3>() { JointPos, JointPos, JointPos, JointPos };
        [SerializeField]
        [NamedListAttribute(new string[] { "Metacarpal", "Proximal", "Intermediate", "Distal" })]
        private List<Vector3> MiddleJointPosition = new List<Vector3>() { JointPos, JointPos, JointPos, JointPos };
        [SerializeField]
        [NamedListAttribute(new string[] { "Metacarpal", "Proximal", "Intermediate", "Distal" })]
        private List<Vector3> RingJointPosition = new List<Vector3>() { JointPos, JointPos, JointPos, JointPos };
        [SerializeField]
        [NamedListAttribute(new string[] { "Metacarpal", "Proximal", "Intermediate", "Distal" })]
        private List<Vector3> PinkieJointPosition = new List<Vector3>() { JointPos, JointPos, JointPos, JointPos };


        [Header("Finger Rotational Thresholds")]

        [SerializeField]
        [NamedListAttribute(new string[] { "Metacarpal", "Proximal", "Intermediate", "Distal" })]
        private List<Quaternion> ThumbJointRotation = new List<Quaternion>() { JointRot, JointRot, JointRot, JointRot };
        [SerializeField]
        [NamedListAttribute(new string[] { "Metacarpal", "Proximal", "Intermediate", "Distal" })]
        private List<Quaternion> IndexJointRotataion = new List<Quaternion>() { JointRot, JointRot, JointRot, JointRot };
        [SerializeField]
        [NamedListAttribute(new string[] { "Metacarpal", "Proximal", "Intermediate", "Distal" })]
        private List<Quaternion> MiddleJointRotaion = new List<Quaternion>() { JointRot, JointRot, JointRot, JointRot };
        [SerializeField]
        [NamedListAttribute(new string[] { "Metacarpal", "Proximal", "Intermediate", "Distal" })]
        private List<Quaternion> RingJointRotaion = new List<Quaternion>() { JointRot, JointRot, JointRot, JointRot };
        [SerializeField]
        [NamedListAttribute(new string[] { "Metacarpal", "Proximal", "Intermediate", "Distal" })]
        private List<Quaternion> PinkieJointRotaion = new List<Quaternion>() { JointRot, JointRot, JointRot, JointRot };


        public void SaveHandPose(Hand handToSerialise)
        {
            serialisedHand = handToSerialise;
        }
        //public void SaveHandPose(BoundHand handToSerialise)
        //{
        //    serialisedBoundHand = handToSerialise;
        //}
    }
}
