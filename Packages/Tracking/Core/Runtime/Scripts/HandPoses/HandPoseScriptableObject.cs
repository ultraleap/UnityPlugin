using System.Collections.Generic;

using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Leap.Unity
{
#if UNITY_EDITOR
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
#endif

    //[CreateAssetMenu(fileName = "HandPose", menuName = "ScriptableObjects/HandPose")]
    public class HandPoseScriptableObject : ScriptableObject
    {
        [System.Serializable]
        public struct FingerJointThresholds
        {
            public Vector2[] jointThresholds;
        }

        [HideInInspector]
        public bool detectThumb = true;
        [HideInInspector]
        public bool detectIndex = true;
        [HideInInspector]
        public bool detectMiddle = true;
        [HideInInspector]
        public bool detectRing = true;
        [HideInInspector]
        public bool detectPinky = true;

        private List<int> fingerIndexesToCheck = new List<int>();

        public List<int> GetFingerIndexesToCheck()
        {
            ApplyFingersToUse();
            return fingerIndexesToCheck;
        }

        #region Finger Thresholds

        [HideInInspector]
        public float globalRotation = 30;

        [HideInInspector]
        public FingerJointThresholds[] fingerJointRotationThresholds = new FingerJointThresholds[5];

        #endregion

        [SerializeField, Attributes.Disable]
        private Hand serializedHand;
        public Hand GetSerializedHand()
        {
            return serializedHand;
        }

        [SerializeField, Attributes.Disable]
        private Hand mirroredHand;
        public Hand GetMirroredHand()
        {
            return mirroredHand;
        }

        [HideInInspector]
        public float hysteresisThreshold = 5;

        public float GetHysteresisThreshold()
        {
            return hysteresisThreshold;
        }

        public void SaveHandPose(Hand handToSerialise)
        {
            serializedHand = new Hand();
            serializedHand = serializedHand.CopyFrom(handToSerialise);
            MirrorHand(handToSerialise);
            SetAllBoneThresholds(globalRotation, true);
        }

        void MirrorHand(Hand handSource)
        {
            mirroredHand = new Hand();
            mirroredHand = mirroredHand.CopyFrom(handSource);
            LeapTransform leapTransform = new LeapTransform(Vector3.zero, Quaternion.Euler(Vector3.zero));
            leapTransform.MirrorX();
            mirroredHand.Transform(leapTransform);
            mirroredHand.IsLeft = !mirroredHand.IsLeft;
            return;
        }

        public Vector2 GetBoneRotationthreshold(int fingerNum, int boneNum)
        {
            if (fingerJointRotationThresholds.Length > 0)
            {
                return fingerJointRotationThresholds[fingerNum].jointThresholds[boneNum];
            }
            else
            {
                return Vector2.zero;
            }
        }

        private void OnValidate()
        {
            MirrorHand(serializedHand);
            ApplyFingersToUse();
        }

        private void ApplyFingersToUse()
        {
            fingerIndexesToCheck.Clear();
            if (detectThumb) { fingerIndexesToCheck.Add(0); }
            if (detectIndex) { fingerIndexesToCheck.Add(1); }
            if (detectMiddle) { fingerIndexesToCheck.Add(2); }
            if (detectRing) { fingerIndexesToCheck.Add(3); }
            if (detectPinky) { fingerIndexesToCheck.Add(4); }
        }

        public void SetAllBoneThresholds(float threshold, bool forceAll = false)
        {
            Vector2 newRotation = new Vector2(threshold, threshold);

            for (int fingerIndex = 0; fingerIndex < fingerJointRotationThresholds.Length; fingerIndex++)
            {
                if (forceAll)
                {
                    fingerJointRotationThresholds[fingerIndex].jointThresholds = new Vector2[] { newRotation, newRotation, newRotation };
                }
                else
                {
                    for (int jointIndex = 0; jointIndex < fingerJointRotationThresholds[fingerIndex].jointThresholds.Length; jointIndex++)
                    {
                        if (fingerJointRotationThresholds[fingerIndex].jointThresholds[jointIndex].x == globalRotation)
                        {
                            fingerJointRotationThresholds[fingerIndex].jointThresholds[jointIndex].x = threshold;
                        }

                        if (fingerJointRotationThresholds[fingerIndex].jointThresholds[jointIndex].y == globalRotation)
                        {
                            fingerJointRotationThresholds[fingerIndex].jointThresholds[jointIndex].y = threshold;
                        }
                    }
                }
            }

            globalRotation = threshold;
        }
    }
}