using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Leap.Unity
{
    [ExecuteInEditMode]
    public class PoseDetectionEditor : LeapProvider
    {
        public override Frame CurrentFrame
        { get
            { 
                return new Frame(0, (long)Time.realtimeSinceStartup, 90, currentHands);
            }
        }
        public Frame TestFrame = null;

        public override Frame CurrentFixedFrame => new Frame();

        [SerializeField]
        HandPoseScriptableObject leftHandPoseObject;
        [SerializeField]
        HandPoseScriptableObject rightHandPoseObject;

        List<Hand> currentHands = new List<Hand>();

        List<Color> gizmoColours = new List<Color>() { Color.green, Color.yellow, Color.red, Color.blue };

        // Update is called once per frame
        void OnValidate()
        {
            Hand PosedHand;
            currentHands.Clear();

            if (leftHandPoseObject != null)
            {
                PosedHand = leftHandPoseObject.GetSerializedHand();
                PosedHand.SetTransform(new Vector3(-0.5f, 0, 0), PosedHand.Rotation);
                leftHand = PosedHand;
                currentHands.Add(PosedHand);
            }
            if(rightHandPoseObject != null)
            {
                PosedHand = rightHandPoseObject.GetSerializedHand();
                PosedHand.SetTransform(new Vector3(0.5f, 0, 0), PosedHand.Rotation);
                rightHand = PosedHand;
                currentHands.Add(PosedHand);
            }

            TestFrame = CurrentFrame;
            DispatchUpdateFrameEvent(CurrentFrame);
        }

        Hand rightHand, leftHand;

        private void OnDrawGizmos()
        {
            ShowEditorGizmos(rightHand, rightHandPoseObject);
            ShowEditorGizmos(leftHand, leftHandPoseObject);
        }

        void ShowEditorGizmos(Hand hand, HandPoseScriptableObject handPoseScriptableObject)
        {
            //foreach (var finger in hand.Fingers)
            for (int j = 0; j < hand.Fingers.Count; j++)
            {
                var finger = hand.Fingers[j];
                if (handPoseScriptableObject.GetFingerIndexesToCheck().Contains(j))
                {
                    for (int i = 0; i < finger.bones.Length; i++)
                    {
                        var bone = finger.bones[i];
                        Gizmos.color = gizmoColours[i];
                        DrawWireArc(bone.PrevJoint, bone.Direction,
                            handPoseScriptableObject.GetBoneRotationthreshold((int)finger.Type, (int)bone.Type), 0.05f);
                    }
                }
            }
        }

        public static void DrawWireArc(Vector3 position, Vector3 dir, float anglesRange, float radius, float maxSteps = 20)
        {
            var srcAngles = GetAnglesFromDir(position, dir);
            var initialPos = position;
            var posA = initialPos;
            var stepAngles = anglesRange / maxSteps;
            var angle = srcAngles - anglesRange / 2;
            for (var i = 0; i <= maxSteps; i++)
            {
                var rad = Mathf.Deg2Rad * angle;
                var posB = initialPos;
                posB += new Vector3(radius * Mathf.Cos(rad), dir.y * radius, radius * Mathf.Sin(rad));

                Gizmos.DrawLine(posA, posB);

                angle += stepAngles;
                posA = posB;
            }
            Gizmos.DrawLine(posA, initialPos);
        }

        static float GetAnglesFromDir(Vector3 position, Vector3 dir)
        {
            var forwardLimitPos = position + dir;
            var srcAngles = Mathf.Rad2Deg * Mathf.Atan2(forwardLimitPos.z - position.z, forwardLimitPos.x - position.x);

            return srcAngles;
        }
    }
}
