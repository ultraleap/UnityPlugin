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
            //transform.position = bone.PrevJoint;
            //transform.rotation = bone.Rotation;
            tipPosition = bone.NextJoint;
            width = bone.Width;
            length = bone.Length;
            UpdateWithInteractionEngineLogic(bone.PrevJoint, bone.Rotation);
            ContactUtils.SetupBoneCollider(boneCollider, bone);
        }

        // TEST VARIABLES  DELETE THESE
        int _lastObjectTouchedAdjustedMass = 1;
        bool _softContactEnabled = false;
        Vector3 lastTargetPosition;
        private float _softContactDislocationDistance = 0.03F;
        protected float softContactDislocationDistance
        {
            get { return _softContactDislocationDistance; }
            set { _softContactDislocationDistance = value; }
        }

        protected const float DEAD_ZONE_FRACTION = 0.04F;
        public float scale { get { return this.transform.lossyScale.x; } }
        /// <summary>
        /// Returns the current velocity of this controller.
        /// </summary>
        Vector3 velocity { get; }
        //----------------------------------



        internal void UpdateWithInteractionEngineLogic(Vector3 targetPosition, Quaternion targetRotation)
        {



            ContactBone contactBone = this;
            Rigidbody body = contactBone.GetComponent<Rigidbody>();

            // Infer ahead if the Interaction Manager has a moving frame of reference.
            //manager.TransformAheadByFixedUpdate(targetPosition, targetRotation, out targetPosition, out targetRotation);

            // Set a fixed rotation for bones; otherwise most friction is lost
            // as any capsule or spherical bones will roll on contact.
            body.MoveRotation(targetRotation);

            // Calculate how far off its target the contact bone is.
            float errorDistance = 0f;
            float errorFraction = 0f;

            float boneWidth = contactBone.width;
            Vector3 lastTargetPositionTransformedAhead = lastTargetPosition;

            errorDistance = Vector3.Distance(lastTargetPositionTransformedAhead, body.position);
            errorFraction = errorDistance / boneWidth;

            // Adjust the mass of the contact bone based on the mass of
            // the object it is currently touching.
            float speed = 0f;
            speed = velocity.magnitude;
            float massScale = Mathf.Clamp(1.0F - (errorFraction * 2.0F), 0.1F, 1.0F)
                          * Mathf.Clamp(speed * 10F, 1F, 10F);
            if (massScale * _lastObjectTouchedAdjustedMass > 0)
            {
                body.mass = massScale * _lastObjectTouchedAdjustedMass;
            }

            // Potentially enable Soft Contact if our error is too large.
            if (!_softContactEnabled && errorDistance >= softContactDislocationDistance
              && speed < 1.5F
                /* && boneArrayIndex != NUM_FINGERS * BONES_PER_FINGER */)
            {
                //this.GetComponent<Collider>().isTrigger = true;
            }

            // Attempt to move the contact bone to its target position and rotation
            // by setting its target velocity and angular velocity. Include a "deadzone"
            // for position to avoid tiny vibrations.
            float deadzone = Mathf.Min(DEAD_ZONE_FRACTION * boneWidth, 0.01F * scale);
            Vector3 delta = (targetPosition - body.position);
            float deltaMag = delta.magnitude;
            if (deltaMag <= deadzone)
            {
                body.velocity = Vector3.zero;
                lastTargetPosition = body.position;
            }
            else
            {
                delta *= (deltaMag - deadzone) / deltaMag;
                lastTargetPosition = body.position + delta;

                Vector3 targetVelocity = delta / Time.fixedDeltaTime;
                float targetVelocityMag = targetVelocity.magnitude;
                body.velocity = (targetVelocity / targetVelocityMag)
                              * Mathf.Clamp(targetVelocityMag, 0F, 100F);
            }
            Quaternion deltaRot = targetRotation * Quaternion.Inverse(body.rotation);
            body.angularVelocity = ToAngularVelocity(deltaRot, Time.fixedDeltaTime);
        }



        public static Vector3 ToAngularVelocity(Quaternion deltaRotation, float deltaTime)
        {
            Vector3 deltaAxis;
            float deltaAngle;
            deltaRotation.ToAngleAxis(out deltaAngle, out deltaAxis);

            if (float.IsInfinity(deltaAxis.x))
            {
                deltaAxis = Vector3.zero;
                deltaAngle = 0;
            }

            if (deltaAngle > 180)
            {
                deltaAngle -= 360.0f;
            }

            return deltaAxis * deltaAngle * Mathf.Deg2Rad / deltaTime;
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
