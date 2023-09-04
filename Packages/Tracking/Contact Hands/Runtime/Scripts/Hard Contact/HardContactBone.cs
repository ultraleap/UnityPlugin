using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.ContactHands
{
    public class HardContactBone : ContactBone
    {
        private ArticulationBody articulationBody;

        internal override void PostFixedUpdateBone()
        {
        }

        internal override void UpdateBone(Bone bone)
        {
            
        }

        internal override void UpdatePalmBone(Hand hand)
        {
        }

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
        }
    }
}