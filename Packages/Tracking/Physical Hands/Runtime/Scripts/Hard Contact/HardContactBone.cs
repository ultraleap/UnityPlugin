/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2024.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using UnityEngine;

namespace Leap.PhysicalHands
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
        internal float DisplacementAmount { get; private set; } = 0;
        internal float DisplacementDistance => _displacementDistance;
        internal float DisplacementRotation => _displacementRotation;

        private const float SIZE_UPDATE_INTERVAL = 0.1f;
        private float nextSizeUpdate = 0;
        private float prevSizeUpdate = 0;

        #region Setup
        internal void SetupBoneBody()
        {
            Collider.material = ((HardContactParent)contactHand.contactParent).physicsMaterial;
            articulation = gameObject.AddComponent<ArticulationBody>();

            if (isPalm)
            {
                SetupPalmArticulation();
            }
            else
            {
                SetupBoneArticulation();
            }

            UpdateIterations();
        }

        private void SetupPalmArticulation()
        {
            articulation.immovable = false;
            articulation.matchAnchors = false;

            articulation.mass = hardContactParent.boneMass * 3f;

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

            articulation.maxAngularVelocity = 1.75f;
            articulation.maxDepenetrationVelocity = 3f;
            articulation.useGravity = false;
            articulation.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            articulation.maxDepenetrationVelocity = 0.001f;
            articulation.linearDamping = 0f;

            if (joint == 0)
            {
                SetupKnuckleDrives();
            }
            else
            {
                SetupBoneDrives();
            }
        }

        private void SetupKnuckleDrives()
        {
            articulation.jointType = ArticulationJointType.SphericalJoint;
            articulation.twistLock = ArticulationDofLock.LimitedMotion;
            articulation.swingYLock = ArticulationDofLock.LimitedMotion;
            articulation.swingZLock = ArticulationDofLock.LimitedMotion;

            ArticulationDrive xDrive = new ArticulationDrive()
            {
                stiffness = 0f,
                forceLimit = 0f,
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
                stiffness = 0f,
                forceLimit = 0f,
                damping = 2f,
                lowerLimit = -15f,
                upperLimit = 15f
            };

            if (finger == 0) // This is the Thumb, it requires a different Y drive too
            {
                yDrive.lowerLimit = contactHand.Handedness == Chirality.Left ? -10f : -50f;
                yDrive.upperLimit = contactHand.Handedness == Chirality.Left ? 50f : 10f;
            }

            articulation.yDrive = yDrive;

            // Set Z limits to 0, locking them causes insane jittering, reuse the yDrive above to save on garbage
            yDrive.lowerLimit = 0f;
            yDrive.upperLimit = 0f;
            articulation.zDrive = yDrive;
        }

        private void SetupBoneDrives()
        {
            articulation.jointType = ArticulationJointType.RevoluteJoint;
            articulation.twistLock = ArticulationDofLock.LimitedMotion;

            ArticulationDrive xDrive = new ArticulationDrive()
            {
                stiffness = 0f,
                forceLimit = 0f,
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

        internal override void UpdateBone(Bone prevBone, Bone bone)
        {
            UpdateBoneSizes(prevBone, bone);

            UpdateBoneAngle(prevBone, bone);
        }

        internal override void UpdatePalmBone(Hand hand)
        {
            // Update the palm collider with the distance between knuckles to wrist + palm width
            if (!IsBoneContacting)
            {
                ContactUtils.InterpolatePalmBones(palmCollider, palmEdgeColliders, hand, hardContactHand.currentResetLerp);
            }

            // If we are contacting and in total, the fingers have displaced above a magic threshold,
            // reduce the force applied to the hardContactHand relative to how far it is from the tracking data
            if (contactHand.isContacting || contactHand.isGrabbing)
            {
                // Reduce the force applied to the hardContactHand relative to how far it is from the tracking data
                hardContactHand.contactForceModifier = hardContactHand.FingerContactDisplacement.Map(0.8f, 8f, 1f, 0.1f);
            }
            else
            {
                hardContactHand.contactForceModifier = 1;
            }

            Vector3 delta = hand.PalmPosition - transform.position;
            delta = delta / Time.fixedDeltaTime;
            delta = delta * hardContactHand.contactForceModifier;

#if UNITY_6000_0_OR_NEWER 
            articulation.linearVelocity = delta;
#else
            articulation.velocity = delta;
#endif 

            Quaternion rotationDelta = Quaternion.Normalize(Quaternion.Slerp(Quaternion.identity, hand.Rotation * Quaternion.Inverse(transform.rotation), hardContactHand.contactForceModifier));

            rotationDelta.ToAngleAxis(out float angleInDeg, out Vector3 rotationAxis);

            Vector3 angularVelocity = (rotationAxis * angleInDeg * Mathf.Deg2Rad) / Time.fixedDeltaTime;

            if (angularVelocity.IsValid())
            {
                articulation.angularVelocity = angularVelocity;
            }
        }

        /// <summary>
        /// Update the sizes of the colliders that represent the hand to align best with the tracked data
        /// Interpolates to the new size to avoid sudden jumps in size
        /// </summary>
        /// <param name="prevBone">The previous bone in the finger</param>
        /// <param name="bone">The bone to update</param>
        /// <param name="forceUpdate">Should this update always happen, or on a reduced frametime?</param>
        private void UpdateBoneSizes(Bone prevBone, Bone bone, bool forceUpdate = false)
        {
            // If it is not a forced update, and we aren't in fixed timestep
            // also ensure we aren't over-updating the size, it'snot necessary to change the colliders every update
            if (!forceUpdate &&
                (!Time.inFixedTimeStep ||
                Time.time < nextSizeUpdate))
            {
                return;
            }

            // Set the time that the next size update is allowed when not resetting
            // this avoids running this logic more often than necessary
            nextSizeUpdate = Time.time + SIZE_UPDATE_INTERVAL;

            float timeDelta = Time.time - prevSizeUpdate;
            prevSizeUpdate = Time.time;

            if (!IsBoneContacting)
            {
                if (joint > 0)
                {
                    InterpolateBoneSize(prevBone.PrevJoint, prevBone.Rotation, bone.PrevJoint,
                        bone.Width, bone.Length, Mathf.Lerp(timeDelta * 10f, timeDelta, hardContactHand.currentResetLerp));
                }
                else // This is the proximal, we should work out where the bone is relative to the top of the palm
                {
                    InterpolateKnucklePosition(prevBone, contactHand.dataHand, hardContactHand.currentResetLerp);
                    InterpolateBoneSize(contactHand.dataHand.PalmPosition, contactHand.dataHand.Rotation, finger == 0 ? prevBone.PrevJoint : prevBone.NextJoint,
                        bone.Width, bone.Length, Mathf.Lerp(timeDelta * 10f, timeDelta, hardContactHand.currentResetLerp));
                }
            }
        }

        private void UpdateBoneAngle(Bone prevBone, Bone bone, bool forceUpdate = false)
        {
            if (!forceUpdate && !Time.inFixedTimeStep)
            {
                return;
            }

            _xTargetAngle = CalculateXJointAngle(prevBone.Rotation, bone.Direction);

            _xDampening = Mathf.Lerp(_xDampening, hardContactParent.boneDamping, Time.fixedDeltaTime * 10);

            float thumbEnterDistance = finger == 0 ? hardContactParent.contactThumbEnterDistance : hardContactParent.contactEnterDistance;

            if (hardContactHand.grabbingFingerDistances[finger] != 1 &&
                hardContactHand.grabbingFingerDistances[finger] > thumbEnterDistance &&
                _xTargetAngle > _grabbingXDrive)
            {
                _grabbingXDrive = Mathf.Clamp(Mathf.Lerp(_grabbingXDrive, _xTargetAngle, Time.fixedDeltaTime * 4),
                    _originalXDriveLower, _originalXDriveUpper);
            }

            _xForceLimit = Mathf.Lerp(hardContactParent.minFingerVelocity, hardContactParent.maxFingerVelocity, hardContactHand.contactForceModifier);

            if (contactHand.ghosted)
            {
                SetDefaultArticulationDrives(prevBone, bone); // we are currenlty ghosted, so allow free movement
            }
            else
            {
                ArticulationDrive xDrive = articulation.xDrive;
                xDrive.stiffness = hardContactHand.fingerStiffness[finger];
                xDrive.damping = _xDampening;
                xDrive.forceLimit = _xForceLimit * Time.fixedDeltaTime;
                xDrive.upperLimit = _wasBoneGrabbing ? _grabbingXDrive : _originalXDriveUpper;
                xDrive.target = _wasBoneGrabbing ? Mathf.Clamp(_xTargetAngle, articulation.xDrive.lowerLimit, _grabbingXDrive) : _xTargetAngle;
                articulation.xDrive = xDrive;

                if (joint == 0) // Allow proximals to have some abduction too
                {
                    _yTargetAngle = CalculateYJointAngle(prevBone.Rotation, bone.Rotation);

                    ArticulationDrive yDrive = articulation.yDrive;
                    yDrive.stiffness = hardContactHand.fingerStiffness[finger];
                    yDrive.damping = _xDampening;
                    yDrive.forceLimit = hardContactParent.maxPalmVelocity * Time.fixedDeltaTime;
                    yDrive.target = _yTargetAngle;
                    articulation.yDrive = yDrive;
                }
            }
        }

        /// <summary>
        /// Set the default movement to follow the hand as closely as possible
        /// Usually call this when there will not be any collisions expected, e.g.When ghosted 
        /// </summary>
        private void SetDefaultArticulationDrives(Bone prevBone, Bone bone)
        {
            ArticulationDrive xDrive = articulation.xDrive;
            xDrive.stiffness = hardContactParent.boneStiffness;
            xDrive.damping = hardContactParent.boneDamping;
            xDrive.forceLimit = hardContactParent.maxFingerVelocity * Time.fixedDeltaTime;
            xDrive.upperLimit = _originalXDriveUpper;
            xDrive.target = _xTargetAngle;
            articulation.xDrive = xDrive;

            if (joint == 0)
            {
                _yTargetAngle = CalculateYJointAngle(prevBone.Rotation, bone.Rotation);

                ArticulationDrive yDrive = articulation.yDrive;
                yDrive.stiffness = hardContactParent.boneStiffness;
                yDrive.damping = hardContactParent.boneDamping;
                yDrive.forceLimit = hardContactParent.maxPalmVelocity * Time.fixedDeltaTime;
                yDrive.target = _yTargetAngle;
                articulation.yDrive = yDrive;
            }
        }

        /// <summary>
        /// Determine the displacement of the bone from the tracked hand, this will impact forces applied
        /// </summary>
        internal void UpdateBoneDisplacement(Bone bone = null)
        {
            _displacementDistance = 0f;
            _displacementRotation = 0f;

            if (bone == null && !isPalm)
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
                    hardContactHand.fingerStiffness[finger] = hardContactParent.grabbedBoneStiffness;
                    _xDampening = 10f;
                }
            }
            else if (IsBoneHovering && NearestObjectDistance < distanceCheck)
            {
                if (IsBoneGrabbing)
                {
                    if (!_wasBoneGrabbing)
                    {
                        _grabbingXDrive = _originalXDriveUpper;
                        _wasBoneGrabbing = true;
                        _grabbingFrames = 3;
                        hardContactHand.fingerStiffness[finger] = hardContactParent.grabbedBoneStiffness;
                        _xDampening = 10f;
                    }
                    hardContactHand.grabbingFingers[finger] = joint;

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
                if (IsBoneHovering && (NearestObjectDistance < hardContactHand.grabbingFingerDistances[finger] || (hardContactHand.grabbingFingerDistances[finger] == 1 && joint == 0)))
                {
                    hardContactHand.grabbingFingerDistances[finger] = NearestObjectDistance;
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
            _xDampening = hardContactParent.boneDamping;

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
        private static float CalculateXJointAngle(Quaternion previous, Vector3 direction)
        {
            return AngleOffAroundAxis(
                        previous * Vector3.forward,
                        direction,
                        previous * Vector3.right);
        }

        private static float CalculateYJointAngle(Quaternion previous, Quaternion current)
        {
            return AngleOffAroundAxis(
                            previous * Vector3.right,
                            current * Vector3.right,
                            previous * Vector3.up);
        }

        /// <summary>
        /// Find some projected angle measure off some forward around some axis.
        /// </summary>
        private static float AngleOffAroundAxis(Vector3 forward, Vector3 v, Vector3 axis, bool clockwise = false)
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
            if (IsBoneContacting)
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
            if (IsBoneContacting)
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