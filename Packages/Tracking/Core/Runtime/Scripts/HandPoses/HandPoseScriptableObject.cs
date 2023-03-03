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
        [HideInInspector]
        public bool DetectThumb = true;
        [HideInInspector]
        public bool DetectIndex = true;
        [HideInInspector]
        public bool DetectMiddle = true;
        [HideInInspector]
        public bool DetectRing = true;
        [HideInInspector]
        public bool DetectPinky = true;

        private List<int> fingerIndexesToCheck = new List<int>();

        public List<int> GetFingerIndexesToCheck()
        {
            ApplyFingersToUse();
            return fingerIndexesToCheck;
        }

        [SerializeField]
        private Hand serializedHand;
        public Hand GetSerializedHand()
        {
            return serializedHand;
        }

        [SerializeField]
        private Hand mirroredHand;
        public Hand GetMirroredHand()
        {
            if(mirroredHand == null || mirroredHand.PalmPosition == Vector3.zero)
            {
                mirroredHand = mirroredHand.CopyFrom(serializedHand);
                MirrorHand(ref mirroredHand);
            }

            return mirroredHand;
        }

        #region Finger Thresholds

        private static Vector2 defaultRotation = new Vector2(15, 15);

        [HideInInspector]
        public Vector2 globalRotation = new Vector2(15,15);
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
            serializedHand = handToSerialise;
            ApplyThresholds();
        }

        void MirrorHand(ref Hand hand)
        {
            LeapTransform leapTransform = new LeapTransform(hand.PalmPosition, hand.Rotation);
            leapTransform.MirrorX();
            hand.Transform(leapTransform);
            return;
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
        }

        private void ApplyFingersToUse()
        {
            fingerIndexesToCheck.Clear();
            if (DetectThumb) { fingerIndexesToCheck.Add(0); }
            if (DetectIndex) { fingerIndexesToCheck.Add(1); }
            if (DetectMiddle) { fingerIndexesToCheck.Add(2); }
            if (DetectRing) { fingerIndexesToCheck.Add(3); }
            if (DetectPinky) { fingerIndexesToCheck.Add(4); }
        }

        public void SetAllBoneThresholds(float threshold)
        {
            Vector2 newRotation = new Vector2(threshold, threshold);

            ThumbJointRotation = new List<Vector2>() { newRotation, newRotation, newRotation, newRotation };
            IndexJointRotataion = new List<Vector2>() { newRotation, newRotation, newRotation, newRotation };
            MiddleJointRotaion = new List<Vector2>() { newRotation, newRotation, newRotation, newRotation };
            RingJointRotaion = new List<Vector2>() { newRotation, newRotation, newRotation, newRotation };
            PinkieJointRotaion = new List<Vector2>() { newRotation, newRotation, newRotation, newRotation };

            ApplyThresholds();
        }
    }
}