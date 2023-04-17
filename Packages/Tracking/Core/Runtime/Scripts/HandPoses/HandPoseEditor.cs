using System;
using System.Collections.Generic;

using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Leap.Unity
{
    [ExecuteInEditMode]
    public class HandPoseEditor : LeapProvider
    {
        [HideInInspector]
        public override Frame CurrentFrame
        {
            get
            {
                List<Hand> hands = new List<Hand>();
                foreach (var hand in currentHandsAndPosedObjects)
                {
                    hands.Add(hand.Item1);
                }

                return new Frame(0, (long)Time.realtimeSinceStartup, 90, hands);
            }
        }

        [HideInInspector]
        public override Frame CurrentFixedFrame => new Frame();

        /// <summary>
        /// Pose to use
        /// </summary>
        public HandPoseScriptableObject handPose;

        public Transform handsLocation;

        private List<Tuple<Hand, HandPoseScriptableObject>> currentHandsAndPosedObjects = new List<Tuple<Hand, HandPoseScriptableObject>>();

        [SerializeField, Tooltip("Sets the colors of the gizmos that represent the rotation thresholds for each joint")]
        private Color[] gizmoColors = new Color[2] { Color.red, Color.blue };


        public void SetHandPose(HandPoseScriptableObject poseToSet)
        {
            handPose = poseToSet;
        }

        private void Update()
        {
            UpdateHands();
        }

        private void UpdateHands()
        {
            currentHandsAndPosedObjects.Clear();

            if (handPose == null)
            {
                return;
            }

            Hand posedHand = new Hand();
            Hand mirroredHand = new Hand();

            posedHand.CopyFrom(handPose.GetSerializedHand());
            mirroredHand.CopyFrom(handPose.GetMirroredHand());

            Vector3 handPosition = Camera.main.transform.position + (Camera.main.transform.forward * 0.5f);

            if (handsLocation != null)
            {
                handPosition = handsLocation.position;
            }

            if (posedHand.IsLeft)
            {
                posedHand.SetTransform((handPosition + new Vector3(-0.15f, 0, 0)), posedHand.Rotation);
                mirroredHand.SetTransform((handPosition + new Vector3(0.15f, 0, 0)), mirroredHand.Rotation);
            }
            else
            {
                posedHand.SetTransform((handPosition + new Vector3(0.15f, 0, 0)), posedHand.Rotation);
                mirroredHand.SetTransform((handPosition + new Vector3(-0.15f, 0, 0)), mirroredHand.Rotation);
            }

            currentHandsAndPosedObjects.Add(new Tuple<Hand, HandPoseScriptableObject>(posedHand, handPose));
            currentHandsAndPosedObjects.Add(new Tuple<Hand, HandPoseScriptableObject>(mirroredHand, handPose));

            DispatchUpdateFrameEvent(CurrentFrame);
        }

#if UNITY_EDITOR
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
                Finger finger = hand.Fingers[j];
                Bone proximal = finger.Bone(Bone.BoneType.TYPE_PROXIMAL);
                Bone intermediate = finger.Bone(Bone.BoneType.TYPE_INTERMEDIATE);

                Plane fingerNormalPlane = new Plane(proximal.PrevJoint, proximal.NextJoint, intermediate.NextJoint);
                Vector3 normal = fingerNormalPlane.normal;
                
                if (handPoseScriptableObject.GetFingerIndexesToCheck().Contains(j))
                {
                    for (int i = 1; i < finger.bones.Length; i++) // start i at 1 to ignore metacarpal
                    {
                        Bone bone = finger.bones[i];
                        Gizmos.matrix = Matrix4x4.identity;

                        //currently only uses x threshold
                        DrawThresholdGizmo(handPoseScriptableObject.GetBoneRotationthreshold(j, i - 1).x, // i-1 to ignore metacarpal
                        bone.Direction.normalized,
                        bone.PrevJoint, normal, gizmoColors[0], bone.Length);

                        if (finger.bones[i].Type == Bone.BoneType.TYPE_PROXIMAL) 
                        {
                            Vector3 proximalNormal = Quaternion.AngleAxis(90, bone.Direction.normalized) * normal;
                            DrawThresholdGizmo(handPoseScriptableObject.GetBoneRotationthreshold(j, i - 1).y, // i-1 to ignore metacarpal
                            bone.Direction.normalized,
                            bone.PrevJoint, proximalNormal, gizmoColors[1], bone.Length);
                        }
                    }
                }
            }
        }

        private void DrawThresholdGizmo(float angle, Vector3 direction, Vector3 pointLocation, Vector3 normal, Color color, float radius = 0.02f, float thickness = 2)
        {
            Gizmos.color = color;
            Handles.color = color;

            Vector3 pointDiff = direction.normalized * radius;

            Quaternion arcRotation = Quaternion.AngleAxis(angle, normal);
            Vector3 arcEnd = RotatePointAroundPivot(pointLocation + pointDiff, pointLocation, arcRotation);

            Handles.DrawWireArc(pointLocation, normal, pointDiff, angle, radius, thickness);
            Handles.DrawWireArc(pointLocation, -normal, pointDiff, angle, radius, thickness);

            Handles.DrawLine(pointLocation, arcEnd, thickness);

            arcRotation = Quaternion.AngleAxis(angle, -normal);
            arcEnd = RotatePointAroundPivot(pointLocation + pointDiff, pointLocation, arcRotation);

            Handles.DrawLine(pointLocation, arcEnd, thickness);
        }

        Vector3 RotatePointAroundPivot(Vector3 point, Vector3 pivot, Quaternion rotation)
        {
            Vector3 result = point - pivot; //the relative vector from pivot to point.
            result = rotation * result; //rotate
            result = pivot + result; //bring back to world space

            return result;
        }
#endif
    }
}