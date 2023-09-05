using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.ContactHands
{
    public class HardContactBone : ContactBone
    {
        internal ArticulationBody articulationBody;

        private HardContactParent hardContactParent => contactParent as HardContactParent;
        private HardContactHand hardContactHand => contactHand as HardContactHand;

        private float xTargetAngle, xForceLimit, xDampening, grabbingXDrive;
        private float yTargetAngle;
        private float originalXDriveLower, originalXDriveUpper;
        private float overRotationCount;
        private bool wasGrabbingBone;

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
            if (contactHand.isContacting || contactHand.isGrabbing)
            {
                // Reduce the hand velocity if we're pushing through an object
                hardContactHand.currentPalmVelocityInterp = Mathf.InverseLerp(hardContactParent.maximumDistance * 0.2f, hardContactParent.maximumDistance * 0.95f, hardContactHand.computedHandDistance).EaseOut();
                hardContactHand.currentPalmVelocity = Mathf.Lerp(hardContactHand.currentPalmVelocity,
                    Mathf.Lerp(hardContactParent.maximumPalmVelocity, hardContactParent.minimumPalmVelocity, hardContactHand.currentPalmVelocityInterp),
                    Time.fixedDeltaTime * (1.0f / 0.025f));
                hardContactHand.currentPalmAngularVelocity = Mathf.Lerp(hardContactHand.currentPalmAngularVelocity,
                    Mathf.Lerp(hardContactParent.maximumPalmAngularVelocity, hardContactParent.minimumPalmAngularVelocity, hardContactHand.currentPalmVelocityInterp),
                    Time.fixedDeltaTime * (1.0f / 0.025f));
            }
            else
            {
                hardContactHand.currentPalmVelocity = Mathf.Lerp(hardContactHand.currentPalmVelocity, hardContactParent.maximumPalmVelocity, Time.fixedDeltaTime * (1.0f / 0.025f));
                hardContactHand.currentPalmAngularVelocity = Mathf.Lerp(hardContactHand.currentPalmAngularVelocity, hardContactParent.maximumPalmAngularVelocity, Time.fixedDeltaTime * (1.0f / 0.025f));
                hardContactHand.currentPalmVelocityInterp = 0f;
            }

            if (contactHand.isGrabbing)
            {
                // Reduce the overall delta amount when the weight is heigher
                hardContactHand.currentPalmWeightInterp = Mathf.InverseLerp(Mathf.Min(hardContactParent.maximumWeight * 0.1f, 1f), hardContactParent.maximumWeight, hardContactHand.graspingWeight).EaseOut();
            }
            else if (hardContactHand.fingerDisplacement > 0.8f)
            {
                hardContactHand.currentPalmWeightInterp = Mathf.InverseLerp(0.8f, 8f, hardContactHand.fingerDisplacement).EaseOut();
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

            Vector3 delta = hand.PalmPosition - contactHand.transform.position;

            articulationBody.velocity = Vector3.ClampMagnitude(Vector3.MoveTowards(articulationBody.velocity, delta * Mathf.Lerp(1.0f, 0.05f, hardContactHand.currentPalmWeight) / Time.fixedDeltaTime, 15f), hardContactHand.currentPalmVelocity * Time.fixedDeltaTime);

            Quaternion rotationDelta = Quaternion.Normalize(Quaternion.Slerp(Quaternion.identity, hand.Rotation * Quaternion.Inverse(contactHand.transform.rotation), Mathf.Lerp(1.0f, 0.1f, hardContactHand.currentPalmWeight)));

            rotationDelta.ToAngleAxis(out float angleInDeg, out Vector3 rotationAxis);

            Vector3 angularVelocity = Vector3.ClampMagnitude((rotationAxis * angleInDeg * Mathf.Deg2Rad) / Time.fixedDeltaTime, hardContactHand.currentPalmAngularVelocity * Time.fixedDeltaTime);

            if (angularVelocity.IsValid())
            {
                articulationBody.angularVelocity = angularVelocity;
            }
        }

        #region Setup
        internal void SetupBoneBody()
        {
            Collider.material = ((HardContactParent)contactHand.contactParent).PhysicsMaterial;
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

            // TODO: replace below elements with dynamic settings
            articulationBody.mass = 1.8f;
            articulationBody.solverIterations = 50;
            articulationBody.solverVelocityIterations = 20;
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

            // TODO: replace below elements with dynamic settings
            articulationBody.mass = 0.6f;
            articulationBody.solverIterations = 50;
            articulationBody.solverVelocityIterations = 20;
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
        private void UpdateBoneSizes(Bone prevBone, Bone bone)
        {
            if (IsBoneContacting)
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

        private void UpdateBoneAngle(Bone prevBone, Bone bone)
        {
            xTargetAngle = CalculateXJointAngle(prevBone.Rotation, bone.Direction);

            xDampening = Mathf.Lerp(xDampening, 2f, Time.fixedDeltaTime * (1.0f / 0.1f));

            if (hardContactHand.grabbingFingerDistances[finger] != 1 && hardContactHand.grabbingFingerDistances[finger] > (finger == 0 ? HardContactParent.CONTACT_THUMB_ENTER_DISTANCE : HardContactParent.CONTACT_ENTER_DISTANCE) && xTargetAngle > grabbingXDrive)
            {
                grabbingXDrive = Mathf.Clamp(Mathf.Lerp(grabbingXDrive, xTargetAngle, Time.fixedDeltaTime * (1.0f / 0.25f)),
                    originalXDriveLower, originalXDriveUpper);
            }

            if (hardContactHand.currentPalmVelocityInterp > 0)
            {
                xForceLimit = Mathf.Lerp(xForceLimit,
                    Mathf.Lerp(hardContactParent.maximumFingerVelocity, hardContactParent.minimumFingerVelocity, hardContactHand.currentPalmVelocityInterp),
                    Time.fixedDeltaTime * (1.0f / 0.05f));
            }
            else
            {
                xForceLimit = Mathf.Lerp(xForceLimit, hardContactParent.maximumFingerVelocity, Time.fixedDeltaTime * (1.0f / 0.5f));
            }

            ArticulationDrive xDrive = articulationBody.xDrive;
            xDrive.stiffness = hardContactParent.boneStiffness;
            xDrive.damping = xDampening;
            xDrive.forceLimit = xForceLimit * Time.fixedDeltaTime;
            xDrive.upperLimit = hardContactHand.grabbingFingers[finger] >= joint ? grabbingXDrive : originalXDriveUpper;
            xDrive.target = wasGrabbingBone ? Mathf.Clamp(xTargetAngle, articulationBody.xDrive.lowerLimit, grabbingXDrive) : xTargetAngle;
            articulationBody.xDrive = xDrive;

            if (joint == 0)
            {
                float yTargetAngle = CalculateYJointAngle(prevBone.Rotation, bone.Rotation);

                ArticulationDrive yDrive = articulationBody.yDrive;
                yDrive.damping = xDampening * .75f;
                yDrive.stiffness = hardContactParent.boneStiffness;
                yDrive.forceLimit = hardContactParent.maximumPalmVelocity * Time.fixedDeltaTime;
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