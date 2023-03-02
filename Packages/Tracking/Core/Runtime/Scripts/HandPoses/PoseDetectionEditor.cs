using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;

namespace Leap.Unity
{
    [ExecuteInEditMode]
    public class PoseDetectionEditor : LeapProvider
    {
        [HideInInspector]
        public override Frame CurrentFrame
        { get
            { 
                List<Hand> hands = new List<Hand>();
                foreach(var hand in currentHandsAndPosedObjects)
                {
                    hands.Add(hand.Item1);
                }

                return new Frame(0, (long)Time.realtimeSinceStartup, 90, hands);
            }
        }
        [HideInInspector]
        public override Frame CurrentFixedFrame => new Frame();
        /// <summary>
        /// Pose to apply to the left hand
        /// </summary>
        [SerializeField]
        HandPoseScriptableObject leftHandPoseObject;
        /// <summary>
        /// Pose to apply to the right hand
        /// </summary>
        [SerializeField]
        HandPoseScriptableObject rightHandPoseObject;

        private List<Tuple<Hand, HandPoseScriptableObject>> currentHandsAndPosedObjects = new List<Tuple<Hand, HandPoseScriptableObject>>();

        [SerializeField]
        private Color[] gizmoColours = new Color[2] { Color.red.WithAlpha(0.3f), Color.blue.WithAlpha(0.3f) };

        private const float lineThickness = 4;

        // Update is called once per frame
        private void OnValidate()
        {
            Hand PosedHand;
            currentHandsAndPosedObjects.Clear();

            if (leftHandPoseObject != null)
            {
                PosedHand = leftHandPoseObject.GetSerializedHand();
                PosedHand.SetTransform(new Vector3(0, 0, 0), PosedHand.Rotation);
                currentHandsAndPosedObjects.Add(new Tuple<Hand, HandPoseScriptableObject>(PosedHand, leftHandPoseObject));
            }
            if(rightHandPoseObject != null)
            {
                PosedHand = rightHandPoseObject.GetSerializedHand();
                PosedHand.SetTransform(new Vector3(0.5f, 0, 0), PosedHand.Rotation);
                currentHandsAndPosedObjects.Add(new Tuple<Hand, HandPoseScriptableObject>(PosedHand, rightHandPoseObject));
            }
            DispatchUpdateFrameEvent(CurrentFrame);
        }

        private void OnDrawGizmos()
        {
            foreach(var hand in currentHandsAndPosedObjects)
            {
                ShowEditorGizmos(hand.Item1, hand.Item2);
            }
        }

        private void ShowEditorGizmos(Hand hand, HandPoseScriptableObject handPoseScriptableObject)
        {
            if(handPoseScriptableObject == null || hand == null)
            {
                return;
            }

            for (int j = 0; j < hand.Fingers.Count; j++)
            {
                var finger = hand.Fingers[j];
                var proximal = hand.Fingers[j].Bone(Bone.BoneType.TYPE_PROXIMAL);
                var intermediate = hand.Fingers[j].Bone(Bone.BoneType.TYPE_INTERMEDIATE);

                Plane fingerNormalPlane = new Plane(proximal.PrevJoint, proximal.NextJoint, intermediate.NextJoint);
                var normal = fingerNormalPlane.normal;
                
                    
                if (handPoseScriptableObject.GetFingerIndexesToCheck().Contains(j))
                {
                    for (int i = 0; i < finger.bones.Length; i++)
                    {
                        var bone = finger.bones[i];
                        Gizmos.color = gizmoColours[0];
                        Gizmos.matrix = Matrix4x4.identity;
                        Handles.color = gizmoColours[0];

                        //currently only uses x threshold
                        DrawWireCone(handPoseScriptableObject.GetBoneRotationthreshold(j, i).x,
                        bone.Direction.normalized,
                        bone.PrevJoint, normal);

                        if (finger.bones[i].Type == Bone.BoneType.TYPE_PROXIMAL) 
                        {
                            Gizmos.color = gizmoColours[1];
                            Handles.color = gizmoColours[1];
                            var proximalNormal = Quaternion.AngleAxis(90, bone.Direction.normalized) * normal;
                            DrawWireCone(handPoseScriptableObject.GetBoneRotationthreshold(j, i).y,
                            bone.Direction.normalized,
                            bone.PrevJoint, proximalNormal);
                        }
                    }
                }
            }
        }


        private void DrawWireCone(float angle, Vector3 coneDirection, Vector3 pointLocation, Vector3 normal)
        {
            float circleRadius = 0.02f;
            var startPoint = coneDirection.normalized * circleRadius;

            Handles.DrawSolidArc(pointLocation, normal, startPoint, angle, circleRadius);
            Handles.DrawSolidArc(pointLocation, -normal, startPoint, angle, circleRadius);

            //Quaternion.AngleAxis(angle, bone.Direction.normalized) * normal
            //Vector3 lineTo = RotatePointAroundPivot(startPoint, pointLocation, Vector3.up * angle);
            var lineTo1 = Quaternion.AngleAxis(angle, coneDirection.normalized) * normal.normalized;
            var lineTo2 = Quaternion.AngleAxis(angle, coneDirection.normalized) * -normal.normalized;

            //Gizmos.DrawLine(pointLocation, lineTo1);
            //Gizmos.DrawLine(pointLocation, lineTo2);
        }

        public Vector3 RotatePointAroundPivot(Vector3 point, Vector3 pivot, Vector3 angles)
        {
            Vector3 dir = point - pivot; // get point direction relative to pivot
            dir = Quaternion.Euler(angles) * dir; // rotate it
            point = dir + pivot; // calculate rotated point
            return point; // return it
        }
    }
}
