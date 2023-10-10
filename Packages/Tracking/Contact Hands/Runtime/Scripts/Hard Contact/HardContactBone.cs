using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.ContactHands
{
    public class HardContactBone : ContactBone
    {
        private HardContactParent hardContactParent => contactParent as HardContactParent;
        private HardContactHand hardContactHand => contactHand as HardContactHand;

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

        #region Setup
        internal void SetupBoneBody()
        {
            Collider.material = ((HardContactParent)contactHand.contactParent).PhysicsMaterial;
            articulation = gameObject.AddComponent<ArticulationBody>();
            if (isPalm)
            {
                SetupPalmArticulation();
            }
            else
            {
                SetupBoneArticulation();
                if (joint == 0)
                {
                    SetupKnuckleDrives(0, 0);
                }
                else
                {
                    SetupBoneDrives(0, 0);
                }
            }
        }

        private void SetupPalmArticulation()
        {
            articulation.immovable = false;
            articulation.matchAnchors = false;

            articulation.mass = hardContactParent.boneMass * 3f;

            UpdateIterations();

            articulation.angularDamping = 50f;
            articulation.linearDamping = 0f;
            articulation.useGravity = false;
            articulation.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            articulation.maxDepenetrationVelocity = 0.001f;
        }

        private void SetupBoneArticulation()
        {
            articulation.anchorPosition = Vector3.zero;
            articulation.anchorRotation = Quaternion.identity;
            articulation.matchAnchors = false;

            articulation.mass = hardContactParent.boneMass;

            UpdateIterations();

            articulation.maxAngularVelocity = 1.75f;
            articulation.maxDepenetrationVelocity = 3f;
            articulation.useGravity = false;
            articulation.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            articulation.maxDepenetrationVelocity = 0.001f;
            articulation.linearDamping = 0f;
        }

        private void SetupKnuckleDrives(float stiffness, float forceLimit)
        {
            articulation.jointType = ArticulationJointType.SphericalJoint;
            articulation.twistLock = ArticulationDofLock.LimitedMotion;
            articulation.swingYLock = ArticulationDofLock.LimitedMotion;
            articulation.swingZLock = ArticulationDofLock.LimitedMotion;

            ArticulationDrive xDrive = new ArticulationDrive()
            {
                stiffness = stiffness,
                forceLimit = forceLimit * Time.fixedDeltaTime,
                damping = 1f,
                lowerLimit = -30f,
                upperLimit = 80f
            };

            if (finger == 0)
            {
                xDrive.lowerLimit = -45f;
                xDrive.upperLimit = 45f;
            }

            articulation.xDrive = xDrive;
            _originalXDriveLower = xDrive.lowerLimit;
            _originalXDriveUpper = xDrive.upperLimit;

            ArticulationDrive yDrive = new ArticulationDrive()
            {
                stiffness = stiffness,
                forceLimit = forceLimit * Time.fixedDeltaTime,
                damping = 2f,
                lowerLimit = -15f,
                upperLimit = 15f
            };

            if (finger == 0)
            {
                yDrive.lowerLimit = contactHand.handedness == Chirality.Left ? -10f : -50f;
                yDrive.upperLimit = contactHand.handedness == Chirality.Left ? 50f : 10f;
            }

            articulation.yDrive = yDrive;

            // Set Z limits to 0, locking them causes insane jittering
            yDrive.lowerLimit = 0f;
            yDrive.upperLimit = 0f;
            articulation.zDrive = yDrive;
        }

        private void SetupBoneDrives(float stiffness, float forceLimit)
        {
            articulation.jointType = ArticulationJointType.RevoluteJoint;
            articulation.twistLock = ArticulationDofLock.LimitedMotion;

            ArticulationDrive xDrive = new ArticulationDrive()
            {
                stiffness = stiffness,
                forceLimit = forceLimit * Time.fixedDeltaTime,
                damping = 1f,
                lowerLimit = -10f,
                upperLimit = 89f
            };

            articulation.xDrive = xDrive;
            _originalXDriveLower = xDrive.lowerLimit;
            _originalXDriveUpper = xDrive.upperLimit;
        }
        #endregion

        #region Updating
        internal override void PostFixedUpdateBone()
        {
            UpdateBoneWorldSpace();
        }

        internal override void UpdateBone(Bone prevBone, Bone bone)
        {
            UpdateBoneSizes(prevBone, bone);

            UpdateBoneAngle(prevBone, bone);

            UpdateBoneWorldSpace();
        }

        internal override void UpdatePalmBone(Hand hand)
        {
            // Update the palm collider with the distance between knuckles to wrist + palm width
            if (!IsBoneContacting)
            {
                ContactUtils.InterpolatePalmBones(palmCollider, palmEdgeColliders, hand, hardContactHand.currentResetLerp);
            }

            if (contactHand.isContacting || contactHand.isGrabbing)
            {
                // Reduce the hand velocity if we're pushing through an object
                hardContactHand.currentPalmVelocityInterp = Mathf.InverseLerp(hardContactParent.teleportDistance * 0.2f, hardContactParent.teleportDistance * 0.95f, hardContactHand.computedHandDistance).EaseOut();
                hardContactHand.currentPalmVelocity = Mathf.Lerp(hardContactHand.currentPalmVelocity,
                    Mathf.Lerp(hardContactParent.maxPalmVelocity, hardContactParent.minPalmVelocity, hardContactHand.currentPalmVelocityInterp),
                    Time.fixedDeltaTime * (1.0f / 0.025f));
                hardContactHand.currentPalmAngularVelocity = Mathf.Lerp(hardContactHand.currentPalmAngularVelocity,
                    Mathf.Lerp(hardContactParent.maxPalmAngularVelocity, hardContactParent.minPalmAngularVelocity, hardContactHand.currentPalmVelocityInterp),
                    Time.fixedDeltaTime * (1.0f / 0.025f));
            }
            else
            {
                hardContactHand.currentPalmVelocity = Mathf.Lerp(hardContactHand.currentPalmVelocity, hardContactParent.maxPalmVelocity, Time.fixedDeltaTime * (1.0f / 0.025f));
                hardContactHand.currentPalmAngularVelocity = Mathf.Lerp(hardContactHand.currentPalmAngularVelocity, hardContactParent.maxPalmAngularVelocity, Time.fixedDeltaTime * (1.0f / 0.025f));
                hardContactHand.currentPalmVelocityInterp = 0f;
            }

            if (contactHand.isGrabbing)
            {
                // Reduce the overall delta amount when the weight is heigher
                hardContactHand.currentPalmWeightInterp = Mathf.InverseLerp(Mathf.Min(hardContactParent.maxWeight * 0.1f, 1f), hardContactParent.maxWeight, hardContactHand.graspingWeight).EaseOut();
            }
            else if (hardContactHand.FingerContactDisplacement > 0.8f)
            {
                hardContactHand.currentPalmWeightInterp = Mathf.InverseLerp(0.8f, 8f, hardContactHand.FingerContactDisplacement).EaseOut();
            }
            else
            {
                hardContactHand.currentPalmWeightInterp = 0f;
            }

            if (hardContactHand.currentPalmWeightInterp > hardContactHand.currentPalmWeight)
            {
                hardContactHand.currentPalmWeight = Mathf.Lerp(hardContactHand.currentPalmWeight, hardContactHand.currentPalmWeightInterp, Time.fixedDeltaTime * (1.0f / 0.025f));
            }
            else
            {
                hardContactHand.currentPalmWeight = Mathf.Lerp(hardContactHand.currentPalmWeight, hardContactHand.currentPalmWeightInterp, Time.fixedDeltaTime * (1.0f / 0.075f));
            }

            Vector3 delta = hand.PalmPosition - transform.position;

            articulation.velocity = Vector3.ClampMagnitude(Vector3.MoveTowards(articulation.velocity, delta * Mathf.Lerp(1.0f, 0.05f, hardContactHand.currentPalmWeight) / Time.fixedDeltaTime, 15f), hardContactHand.currentPalmVelocity * Time.fixedDeltaTime);

            Quaternion rotationDelta = Quaternion.Normalize(Quaternion.Slerp(Quaternion.identity, hand.Rotation * Quaternion.Inverse(transform.rotation), Mathf.Lerp(1.0f, 0.1f, hardContactHand.currentPalmWeight)));

            rotationDelta.ToAngleAxis(out float angleInDeg, out Vector3 rotationAxis);

            Vector3 angularVelocity = Vector3.ClampMagnitude((rotationAxis * angleInDeg * Mathf.Deg2Rad) / Time.fixedDeltaTime, hardContactHand.currentPalmAngularVelocity * Time.fixedDeltaTime);

            if (angularVelocity.IsValid())
            {
                articulation.angularVelocity = angularVelocity;
            }

            UpdateBoneWorldSpace();
        }

        private void UpdateBoneSizes(Bone prevBone, Bone bone, bool forceUpdate = false)
        {
            if (!forceUpdate && !Time.inFixedTimeStep)
            {
                return;
            }

            if (!IsBoneContacting)
            {
                if (joint > 0)
                {
                    InterpolateBoneSize(prevBone.PrevJoint, prevBone.Rotation, bone.PrevJoint,
                        bone.Width, bone.Length, Mathf.Lerp(Time.fixedDeltaTime * 10f, Time.fixedDeltaTime, hardContactHand.currentResetLerp));
                }
                else
                {
                    InterpolateKnucklePosition(prevBone, contactHand.dataHand, hardContactHand.currentResetLerp);
                    InterpolateBoneSize(contactHand.dataHand.PalmPosition, contactHand.dataHand.Rotation, finger == 0 ? prevBone.PrevJoint : prevBone.NextJoint,
                        bone.Width, bone.Length, Mathf.Lerp(Time.fixedDeltaTime * 10f, Time.fixedDeltaTime, hardContactHand.currentResetLerp));
                }
            }
        }

        private void UpdateBoneAngle(Bone prevBone, Bone bone, bool forceUpdate = false)
        {
            if(!forceUpdate && !Time.inFixedTimeStep)
            {
                return;
            }

            _xTargetAngle = CalculateXJointAngle(prevBone.Rotation, bone.Direction);

            _xDampening = Mathf.Lerp(_xDampening, 2f, Time.fixedDeltaTime * (1.0f / 0.1f));

            if (hardContactHand.grabbingFingerDistances[finger] != 1 && hardContactHand.grabbingFingerDistances[finger] > (finger == 0 ? hardContactParent.contactThumbEnterDistance : hardContactParent.contactEnterDistance) && _xTargetAngle > _grabbingXDrive)
            {
                _grabbingXDrive = Mathf.Clamp(Mathf.Lerp(_grabbingXDrive, _xTargetAngle, Time.fixedDeltaTime * (1.0f / 0.25f)),
                    _originalXDriveLower, _originalXDriveUpper);
            }

            if (hardContactHand.currentPalmVelocityInterp > 0)
            {
                _xForceLimit = Mathf.Lerp(_xForceLimit,
                    Mathf.Lerp(hardContactParent.maxFingerVelocity, hardContactParent.minFingerVelocity, hardContactHand.currentPalmVelocityInterp),
                    Time.fixedDeltaTime * (1.0f / 0.05f));
            }
            else
            {
                _xForceLimit = Mathf.Lerp(_xForceLimit, hardContactParent.maxFingerVelocity, Time.fixedDeltaTime * (1.0f / 0.5f));
            }

            ArticulationDrive xDrive = articulation.xDrive;
            xDrive.stiffness = hardContactParent.boneStiffness;
            xDrive.damping = _xDampening;
            xDrive.forceLimit = _xForceLimit * Time.fixedDeltaTime;
            xDrive.upperLimit = hardContactHand.grabbingFingers[finger] >= joint ? _grabbingXDrive : _originalXDriveUpper;
            xDrive.target = _wasBoneGrabbing ? Mathf.Clamp(_xTargetAngle, articulation.xDrive.lowerLimit, _grabbingXDrive) : _xTargetAngle;
            articulation.xDrive = xDrive;

            if (joint == 0)
            {
                _yTargetAngle = CalculateYJointAngle(prevBone.Rotation, bone.Rotation);

                ArticulationDrive yDrive = articulation.yDrive;
                yDrive.damping = _xDampening * .75f;
                yDrive.stiffness = hardContactParent.boneStiffness;
                yDrive.forceLimit = hardContactParent.maxPalmVelocity * Time.fixedDeltaTime;
                yDrive.target = _yTargetAngle;
                articulation.yDrive = yDrive;
            }
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

        internal void UpdateBoneDisplacement(Bone bone = null)
        {
            _displacementDistance = 0f;
            _displacementRotation = 0f;

            if (bone == null && Finger != 5)
            {
                return;
            }

            Vector3 bonePos, position;
            // Palm
            if (isPalm)
            {
                bonePos = contactHand.dataHand.PalmPosition;
                position = transform.position;
                _displacementRotation = Quaternion.Angle(transform.rotation, contactHand.dataHand.Rotation);
            }
            // Fingers
            else
            {
                bonePos = bone.NextJoint;
                boneCollider.ToWorldSpaceCapsule(out Vector3 tip, out Vector3 temp, out float rad);
                position = tip;

                if (articulation.dofCount > 0)
                {
                    _displacementRotation = Mathf.Abs(articulation.xDrive.target - articulation.jointPosition[0] * Mathf.Rad2Deg);
                }
            }

            _displacementDistance = Vector3.Distance(position, bonePos);

            // We want the rotation displacement to be more powerful than the distance
            DisplacementAmount = ((Mathf.InverseLerp(0.01f, hardContactParent.teleportDistance, _displacementDistance) * 0.75f) + (Mathf.InverseLerp(5f, 35f, _displacementRotation) * 1.25f)) * (1 + (Joint * 0.5f));
        }

        internal bool CalculateGrabbingLimits(bool hasFingerGrabbed)
        {
            if (articulation.jointPosition.dofCount > 0)
            {
                _currentXDrive = articulation.jointPosition[0] * Mathf.Rad2Deg;
            }

            if (_grabbingFrames > 0)
            {
                _grabbingFrames--;
                if (_grabbingFrames == 0)
                {
                    _grabbingXDrive = _currentXDrive;
                }
                else
                {
                    _grabbingXDrive = Mathf.Lerp(_grabbingXDrive, articulation.xDrive.target, 1f / 3f);
                }
            }

            float distanceCheck;
            if (_wasBoneGrabbing)
            {
                distanceCheck = finger == 0 ? hardContactParent.contactThumbExitDistance : hardContactParent.contactThumbExitDistance;
            }
            else
            {
                distanceCheck = finger == 0 ? hardContactParent.contactThumbEnterDistance : hardContactParent.contactEnterDistance;
            }

            // If we haven't grasped the other joints then we're not going to successfully with the 0th.
            if (hardContactHand.grabbingFingers[finger] == -1 && joint == 0)
            {
                if (!hasFingerGrabbed)
                {
                    _wasBoneGrabbing = false;
                }
                return hasFingerGrabbed;
            }

            if (hardContactHand.grabbingFingers[finger] != -1)
            {
                if (!_wasBoneGrabbing)
                {
                    _grabbingXDrive = _originalXDriveUpper;
                    _wasBoneGrabbing = true;
                    _grabbingFrames = 3;
                    hardContactHand.fingerStiffness[finger] = 0f;
                    _xDampening = 10f;
                }
            }
            else if (IsBoneHovering && ObjectDistance < distanceCheck)
            {
                if (IsBoneGrabbing)
                {
                    if (!_wasBoneGrabbing)
                    {
                        _grabbingXDrive = _originalXDriveUpper;
                        _grabbingFrames = 3;
                        hardContactHand.fingerStiffness[finger] = 0f;
                        _xDampening = 10f;
                    }
                    hardContactHand.grabbingFingers[finger] = joint;
                    _wasBoneGrabbing = true;
                }
                else if (_wasBoneGrabbing)
                {
                    hardContactHand.grabbingFingers[finger] = joint;
                }
            }
            else
            {
                _wasBoneGrabbing = false;
            }
            if (_wasBoneGrabbing)
            {
                hasFingerGrabbed = true;
                if (IsBoneHovering && (ObjectDistance < hardContactHand.grabbingFingerDistances[finger] || (hardContactHand.grabbingFingerDistances[finger] == 1 && joint == 0)))
                {
                    hardContactHand.grabbingFingerDistances[finger] = ObjectDistance;
                }
            }
            return hasFingerGrabbed;
        }
        #endregion

        #region Resetting
        internal void ResetPalm()
        {
            articulation.immovable = false;
            transform.position = contactHand.dataHand.PalmPosition;
            transform.rotation = contactHand.dataHand.Rotation;
            articulation.TeleportRoot(transform.position, transform.rotation);
            ContactUtils.SetupPalmCollider(palmCollider, palmEdgeColliders, contactHand.dataHand);
            articulation.WakeUp();
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

            // Move the anchor positions to account for hand sizes
            if (joint == 0)
            {
                transform.position = finger == 0 ? prevBone.PrevJoint : prevBone.NextJoint;
                transform.rotation = prevBone.Rotation;

                articulation.parentAnchorPosition = ContactUtils.InverseTransformPoint(contactHand.dataHand.PalmPosition, contactHand.dataHand.Rotation, prevBone.NextJoint);
                if (finger == 0)
                {
                    articulation.parentAnchorRotation = Quaternion.Euler(0,
                        contactHand.dataHand.IsLeft ? ContactUtils.HAND_ROTATION_OFFSET_Y : -ContactUtils.HAND_ROTATION_OFFSET_Y,
                        contactHand.dataHand.IsLeft ? ContactUtils.HAND_ROTATION_OFFSET_Z : -ContactUtils.HAND_ROTATION_OFFSET_Z);
                }
            }
            else
            {
                transform.localPosition = transform.InverseTransformPoint(prevBone.PrevJoint);
                transform.localRotation = Quaternion.identity;

                articulation.parentAnchorPosition = ContactUtils.InverseTransformPoint(prevBone.PrevJoint, prevBone.Rotation, bone.PrevJoint);
                articulation.parentAnchorRotation = Quaternion.identity;
            }

            UpdateBoneSizes(prevBone, bone, true);

            UpdateBoneAngle(prevBone, bone, true);

            articulation.WakeUp();
        }

        internal void UpdateIterations()
        {
            articulation.solverIterations = hardContactParent.useProjectPhysicsIterations ? Physics.defaultSolverIterations : hardContactParent.handSolverIterations;
            articulation.solverVelocityIterations = hardContactParent.useProjectPhysicsIterations ? Physics.defaultSolverVelocityIterations : hardContactParent.handSolverVelocityIterations;
        }

        internal bool BoneOverRotationCheck(float eulerThreshold = 20f)
        {
            float angle = Mathf.Repeat(articulation.transform.localRotation.eulerAngles.x + 180, 360) - 180;
            if (angle < articulation.xDrive.lowerLimit - eulerThreshold)
            {
                return true;
            }

            if (angle > articulation.xDrive.upperLimit + eulerThreshold)
            {
                return true;
            }

            // If the overlapBone's meant to be pretty flat
            float delta = Mathf.DeltaAngle(angle, articulation.xDrive.target);
            if (Mathf.Abs(articulation.xDrive.target) < eulerThreshold / 2f && delta > eulerThreshold)
            {
                // We are over rotated, add a frame to the frame count
                _overRotationCount++;
                if (_overRotationCount > 10)
                {
                    return true;
                }
            }
            else
            {
                // We were over rotated, but no longer are, remove a frame from the frame count
                _overRotationCount = Mathf.Clamp(_overRotationCount - 1, 0, 10);
            }
            return false;
        }
        #endregion

        #region Utils
        public static float CalculateXJointAngle(Quaternion previous, Vector3 direction)
        {
            return AngleOffAroundAxis(
                        previous * Vector3.forward,
                        direction,
                        previous * Vector3.right);
        }

        public static float CalculateYJointAngle(Quaternion previous, Quaternion current)
        {
            return AngleOffAroundAxis(
                            previous * Vector3.right,
                            current * Vector3.right,
                            previous * Vector3.up);
        }

        /// <summary>
        /// Find some projected angle measure off some forward around some axis.
        /// </summary>
        public static float AngleOffAroundAxis(Vector3 forward, Vector3 v, Vector3 axis, bool clockwise = false)
        {
            Vector3 right;
            if (clockwise)
            {
                right = Vector3.Cross(forward, axis);
                forward = Vector3.Cross(axis, right);
            }
            else
            {
                right = Vector3.Cross(axis, forward);
                forward = Vector3.Cross(right, axis);
            }
            return Mathf.Atan2(Vector3.Dot(v, right), Vector3.Dot(v, forward)) * Mathf.Rad2Deg;
        }

        private void InterpolateKnucklePosition(Bone knuckleBone, Leap.Hand leapHand, float deltaTime)
        {
            if (ContactObjects.Count > 0)
            {
                // Stop bones sizing if they're touching things
                // Has the benefit of stopping small objects falling through bones
                return;
            }
            articulation.parentAnchorPosition = Vector3.Lerp(articulation.parentAnchorPosition,
                ContactUtils.InverseTransformPoint(leapHand.PalmPosition, leapHand.Rotation, finger == 0 ? knuckleBone.PrevJoint : knuckleBone.NextJoint),
                deltaTime);
        }

        private void InterpolateBoneSize(Vector3 parentPosition, Quaternion parentRotation, Vector3 childPosition, float width, float length, float deltaTime)
        {
            if (ContactObjects.Count > 0)
            {
                // Stop bones sizing if they're touching things
                // Has the benefit of stopping small objects falling through bones
                return;
            }

            articulation.parentAnchorPosition = Vector3.Lerp(articulation.parentAnchorPosition,
                ContactUtils.InverseTransformPoint(parentPosition, parentRotation, childPosition),
                deltaTime);

            boneCollider.radius = Mathf.Lerp(boneCollider.radius, width * 0.5f, deltaTime);
            boneCollider.height = Mathf.Lerp(boneCollider.height, length + width, deltaTime);
            boneCollider.center = Vector3.Lerp(boneCollider.center, new Vector3(0, 0, length / 2f), deltaTime);
        }
        #endregion
    }
}