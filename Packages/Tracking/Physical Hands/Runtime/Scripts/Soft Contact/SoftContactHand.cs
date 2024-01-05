using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

namespace Leap.Unity.PhysicalHands
{
    public class SoftContactHand : ContactHand
    {
        protected override void ProcessOutputHand(ref Hand modifiedHand)
        {
            modifiedHand.CopyFrom(dataHand);

            // return here to not send the collider positions
            return;

            #region Set modifiedHand to collider positions

            //modifiedHand.SetTransform(palmBone.transform.position, palmBone.transform.rotation);
            //int boneInd = 0;
            //Vector3 posA, posB;

            //float r;
            //for (int i = 0; i < modifiedHand.Fingers.Count; i++)
            //{
            //    Bone b = modifiedHand.Fingers[i].bones[0];
            //    PhysExts.ToWorldSpaceCapsule(bones[boneInd].boneCollider, out posA, out posB, out r);
            //    b.NextJoint = posB;

            //    for (int j = 1; j < modifiedHand.Fingers[i].bones.Length; j++)
            //    {
            //        b = modifiedHand.Fingers[i].bones[j];
            //        PhysExts.ToWorldSpaceCapsule(bones[boneInd].boneCollider, out posA, out posB, out r);
            //        b.PrevJoint = posB;
            //        b.NextJoint = posA;
            //        b.Width = r;
            //        b.Center = (b.PrevJoint + b.NextJoint) / 2f;
            //        b.Direction = (b.NextJoint - b.PrevJoint).normalized;
            //        b.Length = Vector3.Distance(posA, posB);
            //        b.Rotation = bones[boneInd].transform.rotation;
            //        boneInd++;
            //    }

            //    modifiedHand.Fingers[i].TipPosition = posA;
            //}

            //modifiedHand.WristPosition = palmBone.transform.position - (palmBone.transform.rotation * Quaternion.Inverse(dataHand.Rotation) * (dataHand.PalmPosition - dataHand.WristPosition));

            //Vector3 direction = Vector3.Lerp(modifiedHand.Arm.Direction, dataHand.Arm.Direction, Mathf.Lerp(1.0f, 0.1f, 0.05f));

            //modifiedHand.Arm.PrevJoint = modifiedHand.WristPosition + (-dataHand.Arm.Length * direction);
            //modifiedHand.Arm.NextJoint = modifiedHand.WristPosition;
            //modifiedHand.Arm.Center = (modifiedHand.Arm.PrevJoint + modifiedHand.Arm.NextJoint) / 2f;
            //modifiedHand.Arm.Length = Vector3.Distance(modifiedHand.Arm.PrevJoint, modifiedHand.Arm.NextJoint);
            //modifiedHand.Arm.Direction = (modifiedHand.WristPosition - modifiedHand.Arm.PrevJoint).normalized;
            //modifiedHand.Arm.Rotation = Quaternion.LookRotation(modifiedHand.Arm.Direction, -dataHand.PalmNormal);
            //modifiedHand.Arm.Width = dataHand.Arm.Width;

            //modifiedHand.PalmWidth = palmBone.palmCollider.size.y;
            //modifiedHand.Confidence = dataHand.Confidence;
            //modifiedHand.Direction = dataHand.Direction;
            //modifiedHand.FrameId = dataHand.FrameId;
            //modifiedHand.Id = dataHand.Id;
            //modifiedHand.GrabStrength = 0f;
            //modifiedHand.PinchStrength = 0f;
            //modifiedHand.PinchDistance = 0f;
            //modifiedHand.PalmVelocity = (palmBone.transform.position - _oldContactPosition) / Time.fixedDeltaTime;
            //modifiedHand.TimeVisible = dataHand.TimeVisible;

            #endregion
        }

        internal override void BeginHand(Hand hand)
        {
            gameObject.SetActive(true);
            _oldDataPosition = hand.PalmPosition;
            _oldDataRotation = hand.Rotation;
            _velocity = Vector3.zero;
            _angularVelocity = Vector3.zero;
            tracked = true;
        }

        internal override void FinishHand()
        {
            tracked = false;
            _velocity = Vector3.zero;
            _angularVelocity = Vector3.zero;
            gameObject.SetActive(false);
        }

        internal override void GenerateHandLogic()
        {
            GenerateHandObjects(typeof(SoftContactBone));

            ((SoftContactBone)palmBone).SetupBone();
            var rbody = palmBone.gameObject.AddComponent<Rigidbody>();
            rbody.useGravity = false;
            rbody.collisionDetectionMode = CollisionDetectionMode.Continuous;

            palmBone.rigid = rbody;

            // Set the colliders to ignore eachother
            foreach (var bone in bones)
            {
                ((SoftContactBone)bone).SetupBone();
                Physics.IgnoreCollision(palmBone.palmCollider, bone.boneCollider);

                rbody = bone.gameObject.AddComponent<Rigidbody>();
                rbody.useGravity = false;
                rbody.collisionDetectionMode = CollisionDetectionMode.Continuous;

                foreach (var bone2 in bones)
                {
                    if (bone != bone2)
                    {
                        Physics.IgnoreCollision(bone.boneCollider, bone2.boneCollider);
                    }
                }
            }

            ChangeHandLayer(physicalHandsManager.HandsLayer);
        }

        internal override void PostFixedUpdateHandLogic()
        {
            // Don't need to do anything here
        }

        protected override void UpdateHandLogic(Hand hand)
        {
            _velocity = ContactUtils.ToLinearVelocity(_oldDataPosition, hand.PalmPosition, Time.fixedDeltaTime);
            _angularVelocity = ContactUtils.ToAngularVelocity(_oldDataRotation, hand.Rotation, Time.fixedDeltaTime);
        }
    }
}
