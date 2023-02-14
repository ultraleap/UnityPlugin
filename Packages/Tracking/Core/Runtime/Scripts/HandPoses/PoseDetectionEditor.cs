using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;

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
                PosedHand.SetTransform(new Vector3(0, 0, 0), PosedHand.Rotation);
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
            float lineThickness = 4;

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
                        Gizmos.matrix = Matrix4x4.identity;

                        Handles.color = gizmoColours[i];


                        DrawWireCone(handPoseScriptableObject.GetBoneRotationthreshold(j, i), 0.02f,
                        bone.Direction.normalized,
                        bone.PrevJoint, bone.NextJoint, gizmoColours[i], lineThickness);
                    }
                }
            }
        }


        private void DrawWireCone(float angle, float coneLength, Vector3 coneDirection, Vector3 pointLocation, Vector3 baseCentrePoint, Color color, float ringThickness)
        {

            float circleRadius = Mathf.Tan((angle * (float)(Math.PI / 180)) * (Vector3.Distance(pointLocation, pointLocation + (coneDirection * coneLength))));
            Vector3 normal = Vector3.Normalize(baseCentrePoint - pointLocation);
            Vector3 circleCentrePoint = pointLocation + (coneDirection * coneLength);

            var leftPoint = (Vector3.Cross(normal, Vector3.forward).normalized) * circleRadius;
            var rightPoint = (Vector3.Cross(normal, -Vector3.forward).normalized) * circleRadius;
            var upPoint = (Vector3.Cross(normal, Vector3.up).normalized) * circleRadius;
            var downPoint = (Vector3.Cross(normal, -Vector3.up).normalized) * circleRadius;

            Debug.DrawLine(pointLocation, leftPoint + circleCentrePoint, color);
            Debug.DrawLine(pointLocation, rightPoint + circleCentrePoint, color);
            Debug.DrawLine(pointLocation, upPoint + circleCentrePoint, color);
            Debug.DrawLine(pointLocation, downPoint + circleCentrePoint, color);


            Handles.DrawWireDisc(circleCentrePoint, normal, circleRadius, ringThickness);


        }
    }
}
