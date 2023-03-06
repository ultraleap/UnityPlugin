using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;

namespace Leap.Unity
{
    [ExecuteInEditMode]
    public class HandPoseEditor : LeapProvider
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
        /// Pose to use
        /// </summary>
        /// 
        [HideInInspector]
        public HandPoseScriptableObject handPose;

        [SerializeField]
        Transform handsLocation;

        private List<Tuple<Hand, HandPoseScriptableObject>> currentHandsAndPosedObjects = new List<Tuple<Hand, HandPoseScriptableObject>>();

        [SerializeField, Tooltip("Sets the colors of the gizmos that represent the rotation thresholds for each joint")]
        private Color[] gizmoColors = new Color[2] { Color.red.WithAlpha(0.3f), Color.blue.WithAlpha(0.3f) };

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

            Hand posedHand = handPose.GetSerializedHand();
            Hand mirroredHand = handPose.GetMirroredHand();

            Vector3 handPosition = Camera.main.transform.position + (Camera.main.transform.forward * 0.5f);

            if (handsLocation != null)
            {
                handPosition = handsLocation.position;
            }

            if(posedHand.IsLeft)
            {
                posedHand.SetTransform((handPosition + new Vector3(-0.15f, 0, 0)), posedHand.Rotation);
                mirroredHand.SetTransform((handPosition + new Vector3(0.15f, 0, 0)), mirroredHand.Rotation);

                mirroredHand.IsLeft = false;
            }
            else
            {
                posedHand.SetTransform((handPosition + new Vector3(0.15f, 0, 0)), posedHand.Rotation);
                mirroredHand.SetTransform((handPosition + new Vector3(-0.15f, 0, 0)), mirroredHand.Rotation);

                mirroredHand.IsLeft = true;
            }

            currentHandsAndPosedObjects.Add(new Tuple<Hand, HandPoseScriptableObject>(posedHand, handPose));
            currentHandsAndPosedObjects.Add(new Tuple<Hand, HandPoseScriptableObject>(mirroredHand, handPose));

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
                var proximal = finger.Bone(Bone.BoneType.TYPE_PROXIMAL);
                var intermediate = finger.Bone(Bone.BoneType.TYPE_INTERMEDIATE);

                Plane fingerNormalPlane = new Plane(proximal.PrevJoint, proximal.NextJoint, intermediate.NextJoint);
                var normal = fingerNormalPlane.normal;
                
                if (handPoseScriptableObject.GetFingerIndexesToCheck().Contains(j))
                {
                    for (int i = 1; i < finger.bones.Length; i++)
                    {
                        var bone = finger.bones[i];
                        Gizmos.matrix = Matrix4x4.identity;

                        //currently only uses x threshold
                        DrawThresholdGizmo(handPoseScriptableObject.GetBoneRotationthreshold(j, i).x,
                        bone.Direction.normalized,
                        bone.PrevJoint, normal, gizmoColors[0], bone.Length);

                        if (finger.bones[i].Type == Bone.BoneType.TYPE_PROXIMAL) 
                        {
                            var proximalNormal = Quaternion.AngleAxis(90, bone.Direction.normalized) * normal;
                            DrawThresholdGizmo(handPoseScriptableObject.GetBoneRotationthreshold(j, i).y,
                            bone.Direction.normalized,
                            bone.PrevJoint, proximalNormal, gizmoColors[1], bone.Length);
                        }
                    }
                }
            }
        }


        private void DrawThresholdGizmo(float angle, Vector3 direction, Vector3 pointLocation, Vector3 normal, Color color, float radius = 0.02f)
        {
            Gizmos.color = color;
            Handles.color = color;

            var startPoint = direction.normalized * radius;

            Handles.DrawSolidArc(pointLocation, normal, startPoint, angle, radius);
            Handles.DrawSolidArc(pointLocation, -normal, startPoint, angle, radius);

            Gizmos.color = Color.white;
            Handles.color = Color.white;

           // Handles.DrawWireArc(pointLocation, normal, startPoint, angle, radius);
           // Handles.DrawWireArc(pointLocation, -normal, startPoint, angle, radius);

            if (radius > 0.02f)
            {
                Handles.DrawWireArc(pointLocation, normal, startPoint, angle, 0.02f);
                Handles.DrawWireArc(pointLocation, -normal, startPoint, angle, 0.02f);
            }
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
