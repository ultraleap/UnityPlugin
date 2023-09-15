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

        private float xTargetAngle, xForceLimit, xDampening, grabbingXDrive;
        private float yTargetAngle;
        private float originalXDriveLower, originalXDriveUpper;
        private float overRotationCount;
        private bool wasGrabbingBone;

        #region Setup
        internal void SetupBoneBody()
        {
            Collider.material = ((SoftContactParent)contactHand.contactParent).PhysicsMaterial;
            articulationBody = gameObject.AddComponent<ArticulationBody>();
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
            articulationBody.immovable = false;
            articulationBody.matchAnchors = false;

            articulationBody.mass = softContactParent.boneMass * 3f;
            articulationBody.solverIterations = softContactParent.useProjectPhysicsIterations ? Physics.defaultSolverIterations : softContactParent.handSolverIterations;
            articulationBody.solverVelocityIterations = softContactParent.useProjectPhysicsIterations ? Physics.defaultSolverVelocityIterations : softContactParent.handSolverVelocityIterations;

            articulationBody.angularDamping = 50f;
            articulationBody.linearDamping = 0f;
            articulationBody.useGravity = false;
            articulationBody.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            articulationBody.maxDepenetrationVelocity = 0.001f;
        }

        private void SetupBoneArticulation()
        {
            articulationBody.anchorPosition = Vector3.zero;
            articulationBody.anchorRotation = Quaternion.identity;
            articulationBody.matchAnchors = false;

            articulationBody.mass = softContactParent.boneMass;
            articulationBody.solverIterations = softContactParent.useProjectPhysicsIterations ? Physics.defaultSolverIterations : softContactParent.handSolverIterations;
            articulationBody.solverVelocityIterations = softContactParent.useProjectPhysicsIterations ? Physics.defaultSolverVelocityIterations : softContactParent.handSolverVelocityIterations;

            articulationBody.maxAngularVelocity = 1.75f;
            articulationBody.maxDepenetrationVelocity = 3f;
            articulationBody.useGravity = false;
            articulationBody.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            articulationBody.maxDepenetrationVelocity = 0.001f;
            articulationBody.linearDamping = 0f;
        }

        private void SetupKnuckleDrives(float stiffness, float forceLimit)
        {
            articulationBody.jointType = ArticulationJointType.SphericalJoint;
            articulationBody.twistLock = ArticulationDofLock.LimitedMotion;
            articulationBody.swingYLock = ArticulationDofLock.LimitedMotion;
            articulationBody.swingZLock = ArticulationDofLock.LimitedMotion;

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

            articulationBody.xDrive = xDrive;
            originalXDriveLower = xDrive.lowerLimit;
            originalXDriveUpper = xDrive.upperLimit;

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

            articulationBody.yDrive = yDrive;

            // Set Z limits to 0, locking them causes insane jittering
            yDrive.lowerLimit = 0f;
            yDrive.upperLimit = 0f;
            articulationBody.zDrive = yDrive;
        }

        private void SetupBoneDrives(float stiffness, float forceLimit)
        {
            articulationBody.jointType = ArticulationJointType.RevoluteJoint;
            articulationBody.twistLock = ArticulationDofLock.LimitedMotion;

            ArticulationDrive xDrive = new ArticulationDrive()
            {
                stiffness = stiffness,
                forceLimit = forceLimit * Time.fixedDeltaTime,
                damping = 1f,
                lowerLimit = -10f,
                upperLimit = 89f
            };

            articulationBody.xDrive = xDrive;
            originalXDriveLower = xDrive.lowerLimit;
            originalXDriveUpper = xDrive.upperLimit;
        }
        #endregion

        #region Updating
        internal override void PostFixedUpdateBone()
        {
        }

        internal override void UpdateBone(Bone prevBone, Bone bone)
        {
            UpdateBoneSizes(prevBone, bone);

            UpdateBoneAngle(prevBone, bone);
        }

        internal override void UpdatePalmBone(Hand hand)
        {
            if (contactHand.isGrabbing)
            {
                // Reduce the hand velocity if we're pushing through an object
                softContactHand.currentPalmVelocityInterp = Mathf.InverseLerp(softContactParent.teleportDistance * 0.2f, softContactParent.teleportDistance * 0.95f, softContactHand.computedHandDistance).EaseOut();
                softContactHand.currentPalmVelocity = Mathf.Lerp(softContactHand.currentPalmVelocity,
                    Mathf.Lerp(softContactParent.maxPalmVelocity, softContactParent.minPalmVelocity, softContactHand.currentPalmVelocityInterp),
                    Time.fixedDeltaTime * (1.0f / 0.025f));
                softContactHand.currentPalmAngularVelocity = Mathf.Lerp(softContactHand.currentPalmAngularVelocity,
                    Mathf.Lerp(softContactParent.maxPalmAngularVelocity, softContactParent.minPalmAngularVelocity, softContactHand.currentPalmVelocityInterp),
                    Time.fixedDeltaTime * (1.0f / 0.025f));
            }
            else
            {
                softContactHand.currentPalmVelocity = Mathf.Lerp(softContactHand.currentPalmVelocity, softContactParent.maxPalmVelocity, Time.fixedDeltaTime * (1.0f / 0.025f));
                softContactHand.currentPalmAngularVelocity = Mathf.Lerp(softContactHand.currentPalmAngularVelocity, softContactParent.maxPalmAngularVelocity, Time.fixedDeltaTime * (1.0f / 0.025f));
                softContactHand.currentPalmVelocityInterp = 0f;
            }

            if (contactHand.isGrabbing)
            {
                // Reduce the overall delta amount when the weight is heigher
                softContactHand.currentPalmWeightInterp = Mathf.InverseLerp(Mathf.Min(softContactParent.maxWeight * 0.1f, 1f), softContactParent.maxWeight, softContactHand.graspingWeight).EaseOut();
            }
            else if (softContactHand.fingerDisplacement > 0.8f)
            {
                softContactHand.currentPalmWeightInterp = Mathf.InverseLerp(0.8f, 8f, softContactHand.fingerDisplacement).EaseOut();
            }
            else
            {
                softContactHand.currentPalmWeightInterp = 0f;
            }

            if (softContactHand.currentPalmWeightInterp > softContactHand.currentPalmWeight)
            {
                softContactHand.currentPalmWeight = Mathf.Lerp(softContactHand.currentPalmWeight, softContactHand.currentPalmWeightInterp, Time.fixedDeltaTime * (1.0f / 0.025f));
            }
            else
            {
                softContactHand.currentPalmWeight = Mathf.Lerp(softContactHand.currentPalmWeight, softContactHand.currentPalmWeightInterp, Time.fixedDeltaTime * (1.0f / 0.075f));
            }

            Vector3 delta = hand.PalmPosition - contactHand.transform.position;

            articulationBody.velocity = Vector3.ClampMagnitude(Vector3.MoveTowards(articulationBody.velocity, delta * Mathf.Lerp(1.0f, 0.05f, softContactHand.currentPalmWeight) / Time.fixedDeltaTime, 15f), softContactHand.currentPalmVelocity * Time.fixedDeltaTime);

            Quaternion rotationDelta = Quaternion.Normalize(Quaternion.Slerp(Quaternion.identity, hand.Rotation * Quaternion.Inverse(contactHand.transform.rotation), Mathf.Lerp(1.0f, 0.1f, softContactHand.currentPalmWeight)));

            rotationDelta.ToAngleAxis(out float angleInDeg, out Vector3 rotationAxis);

            Vector3 angularVelocity = Vector3.ClampMagnitude((rotationAxis * angleInDeg * Mathf.Deg2Rad) / Time.fixedDeltaTime, softContactHand.currentPalmAngularVelocity * Time.fixedDeltaTime);

            if (angularVelocity.IsValid())
            {
                articulationBody.angularVelocity = angularVelocity;
            }
        }

        private void UpdateBoneSizes(Bone prevBone, Bone bone)
        {
            if (IsBoneContacting)
            {
                if (joint > 0)
                {
                    InterpolateBoneSize(prevBone.PrevJoint, prevBone.Rotation, bone.PrevJoint,
                        bone.Width, bone.Length, Mathf.Lerp(Time.fixedDeltaTime * 10f, Time.fixedDeltaTime, softContactHand.currentResetLerp));
                }
                else
                {
                    InterpolateKnucklePosition(prevBone, contactHand.dataHand, softContactHand.currentResetLerp);
                    InterpolateBoneSize(contactHand.dataHand.PalmPosition, contactHand.dataHand.Rotation, finger == 0 ? prevBone.PrevJoint : prevBone.NextJoint,
                        bone.Width, bone.Length, Mathf.Lerp(Time.fixedDeltaTime * 10f, Time.fixedDeltaTime, softContactHand.currentResetLerp));
                }
            }
        }

        private void UpdateBoneAngle(Bone prevBone, Bone bone)
        {
            xTargetAngle = CalculateXJointAngle(prevBone.Rotation, bone.Direction);

            xDampening = Mathf.Lerp(xDampening, 2f, Time.fixedDeltaTime * (1.0f / 0.1f));

            if (softContactHand.grabbingFingerDistances[finger] != 1 && softContactHand.grabbingFingerDistances[finger] > (finger == 0 ? softContactParent.contactThumbEnterDistance : softContactParent.contactEnterDistance) && xTargetAngle > grabbingXDrive)
            {
                grabbingXDrive = Mathf.Clamp(Mathf.Lerp(grabbingXDrive, xTargetAngle, Time.fixedDeltaTime * (1.0f / 0.25f)),
                    originalXDriveLower, originalXDriveUpper);
            }

            if (softContactHand.currentPalmVelocityInterp > 0)
            {
                xForceLimit = Mathf.Lerp(xForceLimit,
                    Mathf.Lerp(softContactParent.maxFingerVelocity, softContactParent.minFingerVelocity, softContactHand.currentPalmVelocityInterp),
                    Time.fixedDeltaTime * (1.0f / 0.05f));
            }
            else
            {
                xForceLimit = Mathf.Lerp(xForceLimit, softContactParent.maxFingerVelocity, Time.fixedDeltaTime * (1.0f / 0.5f));
            }

            ArticulationDrive xDrive = articulationBody.xDrive;
            xDrive.stiffness = softContactParent.boneStiffness;
            xDrive.damping = xDampening;
            xDrive.forceLimit = xForceLimit * Time.fixedDeltaTime;
            xDrive.upperLimit = softContactHand.grabbingFingers[finger] >= joint ? grabbingXDrive : originalXDriveUpper;
            xDrive.target = wasGrabbingBone ? Mathf.Clamp(xTargetAngle, articulationBody.xDrive.lowerLimit, grabbingXDrive) : xTargetAngle;
            articulationBody.xDrive = xDrive;

            if (joint == 0)
            {
                yTargetAngle = CalculateYJointAngle(prevBone.Rotation, bone.Rotation);

                ArticulationDrive yDrive = articulationBody.yDrive;
                yDrive.damping = xDampening * .75f;
                yDrive.stiffness = softContactParent.boneStiffness;
                yDrive.forceLimit = softContactParent.maxPalmVelocity * Time.fixedDeltaTime;
                yDrive.target = yTargetAngle;
                articulationBody.yDrive = yDrive;
            }
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
            articulationBody.parentAnchorPosition = Vector3.Lerp(articulationBody.parentAnchorPosition,
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

            articulationBody.parentAnchorPosition = Vector3.Lerp(articulationBody.parentAnchorPosition,
                ContactUtils.InverseTransformPoint(parentPosition, parentRotation, childPosition),
                deltaTime);

            boneCollider.radius = Mathf.Lerp(boneCollider.radius, width * 0.5f, deltaTime);
            boneCollider.height = Mathf.Lerp(boneCollider.height, length + width, deltaTime);
            boneCollider.center = Vector3.Lerp(boneCollider.center, new Vector3(0, 0, length / 2f), deltaTime);
        }
        #endregion
    }
}
