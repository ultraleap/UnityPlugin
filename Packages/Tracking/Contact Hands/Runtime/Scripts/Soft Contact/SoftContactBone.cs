using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace Leap.Unity.ContactHands
{
    public class SoftContactBone : ContactBone
    {
        internal ArticulationBody articulationBody;

        private SoftContactParent softContactParent => contactParent as SoftContactParent;
        private SoftContactHand softContactHand => contactHand as SoftContactHand;

        private float _xTargetAngle, _xForceLimit, _xDampening, _currentXDrive, _grabbingXDrive;
        private float _yTargetAngle;
        private float _originalXDriveLower, _originalXDriveUpper;
        private float _overRotationCount;
        private int _grabbingFrames;
        private bool _wasBoneGrabbing;

        private float _displacementDistance = 0f;
        private float _displacementRotation = 0f;
        public float DisplacementAmount { get; private set; } = 0;
        public float DisplacementDistance => _displacementDistance;
        public float DisplacementRotation => _displacementRotation;


        internal void SetupBone()
        {
            Collider.material = ((SoftContactParent)contactHand.contactParent).PhysicsMaterial;
        }


        #region Updating
        internal override void UpdatePalmBone(Hand hand)
        {
            transform.position = hand.PalmPosition;
            transform.rotation = hand.Rotation;
            width = hand.PalmWidth;
            palmThickness = hand.Fingers[2].Bone(0).Width;
            tipPosition = hand.CalculateAverageKnucklePosition();
            wristPosition = hand.WristPosition;
            length = Vector3.Distance(tipPosition, wristPosition);
            ContactUtils.SetupPalmCollider(palmCollider, hand);
        }

        internal override void UpdateBone(Bone prevBone, Bone bone)
        {
            transform.position = bone.PrevJoint;
            transform.rotation = bone.Rotation;
            tipPosition = bone.NextJoint;
            width = bone.Width;
            length = bone.Length;
            ContactUtils.SetupBoneCollider(boneCollider, bone);
        }

        internal override void PostFixedUpdateBone()
        {
            UpdateBoneWorldSpace();
        }

        private void UpdateBoneWorldSpace()
        {
            if (isPalm)
            {
                PhysExts.ToWorldSpaceBox(palmCollider, out Vector3 center, out Vector3 halfExtents, out Quaternion orientation);
                this.center = center;
                this.tipPosition = center + (orientation * (Vector3.forward * halfExtents.z));
                this.palmThickness = halfExtents.y * 2f;
                this.wristPosition = transform.position - (transform.rotation * Quaternion.Inverse(contactHand.dataHand.Rotation) * (contactHand.dataHand.PalmPosition - contactHand.dataHand.WristPosition));
            }
            else
            {
                PhysExts.ToWorldSpaceCapsule(boneCollider, out Vector3 tip, out Vector3 bottom, out float radius);
                this.tipPosition = tip;
                this.width = radius;
                this.length = Vector3.Distance(bottom, tip);
                this.center = Vector3.Lerp(bottom, tip, 0.5f);
            }
        }

        #endregion

        internal void ResetPalm()
        {
            transform.position = contactHand.dataHand.PalmPosition;
            transform.rotation = contactHand.dataHand.Rotation;
            ContactUtils.SetupPalmCollider(palmCollider, contactHand.dataHand);
        }

        internal void ResetBone(Bone prevBone, Bone bone)
        {
            _overRotationCount = 0;

            _wasBoneGrabbing = false;
            _xDampening = 1f;

            if (transform.parent != null)
            {
                transform.localScale = new Vector3(
                    1f / transform.parent.lossyScale.x,
                    1f / transform.parent.lossyScale.y,
                    1f / transform.parent.lossyScale.z);
            }

            ContactUtils.SetupBoneCollider(boneCollider, bone);
        }


        private void OnDrawGizmos()
        {

            Debug.DrawLine(transform.position, tipPosition, Color.cyan);
        }
    }
}
