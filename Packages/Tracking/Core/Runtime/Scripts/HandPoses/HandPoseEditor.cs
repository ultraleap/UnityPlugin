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
                foreach (var hand in currentPosedHands)
                {
                    hands.Add(hand);
                }

                return new Frame(0, (long)Time.realtimeSinceStartup, 90, hands);
            }
        }

        [HideInInspector]
        public override Frame CurrentFixedFrame => new Frame();

        /// <summary>
        /// Pose to use
        /// </summary>
        [SerializeField]
        public HandPoseScriptableObject handPose;

        public Transform handsLocation;

        private List<Hand> currentPosedHands = new List<Hand>();

        [SerializeField, Tooltip("Sets the colors of the gizmos that represent the rotation thresholds for each joint")]
        private Color[] gizmoColors = new Color[2] { Color.red, Color.blue };

        public bool editingPoseJoints = false;

        public void SetHandPose(HandPoseScriptableObject poseToSet)
        {
            handPose = poseToSet;
        }

        private void Update()
        {
            UpdateHands();

            if(editingPoseJoints && Application.isPlaying)
            {
                Debug.LogWarning("Pose Editing was still active when beginning application. Unsaved changed will be discarded to avoid conflicting pose data." +
                    "\nTo avoid this, save your changes in the Pose Editor Component.", gameObject);
                EndJointEditing(false);
            }
        }

        private void UpdateHands()
        {
            currentPosedHands.Clear();

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

            GetEditingJointsForDisplay(ref posedHand);

            currentPosedHands.Add(posedHand);
            currentPosedHands.Add(mirroredHand);

            DispatchUpdateFrameEvent(CurrentFrame);
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            foreach(var hand in currentPosedHands)
            {
                ShowEditorGizmos(hand);
            }
        }

        private void ShowEditorGizmos(Hand hand)
        {
            if(handPose == null || hand == null)
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
                
                if (handPose.GetFingerIndexesToCheck().Contains(j))
                {
                    for (int i = 1; i < finger.bones.Length; i++) // start i at 1 to ignore metacarpal
                    {
                        Bone bone = finger.bones[i];
                        Gizmos.matrix = Matrix4x4.identity;

                        //currently only uses x threshold
                        DrawThresholdGizmo(handPose.GetBoneRotationthreshold(j, i - 1).x, // i-1 to ignore metacarpal
                        bone.Direction.normalized,
                        bone.PrevJoint, normal, gizmoColors[0], bone.Length);

                        if (finger.bones[i].Type == Bone.BoneType.TYPE_PROXIMAL) 
                        {
                            Vector3 proximalNormal = Quaternion.AngleAxis(90, bone.Direction.normalized) * normal;
                            DrawThresholdGizmo(handPose.GetBoneRotationthreshold(j, i - 1).y, // i-1 to ignore metacarpal
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

        #region Joint Editing

        public Transform[] handEditingJoints;

        public GameObject editHand;

        public void BeginJointEditing()
        {
            editingPoseJoints = true;
            handEditingJoints = new Transform[20];

            Hand hand = currentPosedHands[0];

            editHand = new GameObject("Pose Editor Hand");

            editHand.transform.position = hand.PalmPosition;
            editHand.transform.rotation = hand.Rotation;
            editHand.transform.SetParent(transform);

            for (int fingerID = 0; fingerID < 5; fingerID++)
            {
                Transform finger = new GameObject("Pose Editor Finger " + (Finger.FingerType)fingerID).transform;
                finger.position = hand.Fingers[fingerID].bones[0].PrevJoint;
                finger.rotation = hand.Fingers[fingerID].bones[0].Rotation;
                finger.SetParent(editHand.transform);

                Transform prevBoneTransform = finger;

                for (int boneID = 1; boneID < 4; boneID++)
                {
                    Transform newTransform = GameObject.CreatePrimitive(PrimitiveType.Sphere).transform;
                    newTransform.name = "Pose Editor Joint " + (Finger.FingerType)fingerID + " " + (Bone.BoneType)boneID;
                    newTransform.position = hand.Fingers[fingerID].bones[boneID].PrevJoint;
                    newTransform.rotation = hand.Fingers[fingerID].bones[boneID].Rotation;
                    newTransform.localScale = Vector3.one * 0.01f;
                    newTransform.SetParent(prevBoneTransform);
                    prevBoneTransform = newTransform;

                    handEditingJoints[(fingerID * 4) + boneID] = newTransform;
                }
            }
        }

        public void EndJointEditing(bool andSave)
        {
            if (andSave)
            {
                // Edit Pose hand
                Hand posedHand = new Hand();
                posedHand.CopyFrom(handPose.GetSerializedHand());
                Vector3 handPosition = Camera.main.transform.position + (Camera.main.transform.forward * 0.5f);

                if (handsLocation != null)
                {
                    handPosition = handsLocation.position;
                }

                if (posedHand.IsLeft)
                {
                    posedHand.SetTransform((handPosition + new Vector3(-0.15f, 0, 0)), posedHand.Rotation);
                }
                else
                {
                    posedHand.SetTransform((handPosition + new Vector3(0.15f, 0, 0)), posedHand.Rotation);
                }

                GetEditingJointsForDisplay(ref posedHand);

                Undo.RecordObject(handPose, "Hand Pose Edit");
                handPose.UpdateHandPose(posedHand);

                // Save changes
                EditorUtility.SetDirty(handPose);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }

            // Tidy up
            DestroyImmediate(editHand);

            editingPoseJoints = false;
        }

        void GetEditingJointsForDisplay(ref Hand handToChange)
        {
            if (!editingPoseJoints)
                return;

            for (int fingerID = 0; fingerID < 5; fingerID++)
            {
                for (int boneID = 1; boneID < 4; boneID++)
                {
                    handToChange.Fingers[fingerID].bones[boneID].SetTransform(handEditingJoints[(fingerID * 4) + boneID].position, handEditingJoints[(fingerID * 4) + boneID].rotation);
                }
            }
        }

        #endregion
#endif
    }
}