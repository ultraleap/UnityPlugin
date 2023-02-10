/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2023.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/


using System.Linq;
using UnityEngine;

namespace Leap.Unity.Interaction.PhysicsHands
{
    public static class PhysicsHandsUtils
    {
        #region Hand Generation
        // If you are running on Unity 2021.2 or higher, this can be done at run time.
        // If not you should be running this from the PhysicsProvider through the editor script, and having the scene store your hands.
        // We need to uncheck the "Compute Parent Anchors" checkbox in pre 2021.2 which can only be done automatically through editor scripts.
        // In 2021.2 and above it's called matchAnchors and can be called through code.
        public static PhysicsHand GenerateHand(Chirality handedness, float boneMass, float strength, float forceLimit, float stiffness, SingleLayer layer, GameObject parent = null)
        {
            GameObject rootObject = new GameObject($"{(handedness == Chirality.Left ? "Left" : "Right")} Hand", typeof(PhysicsHand));
            rootObject.layer = layer;
            if (parent != null)
            {
                rootObject.transform.SetParent(parent.transform);
            }

            GameObject palmGameObject = new GameObject($"{(handedness == Chirality.Left ? "Left" : "Right")} Palm", typeof(ArticulationBody), typeof(BoxCollider));
            palmGameObject.layer = rootObject.layer;
            palmGameObject.transform.SetParent(rootObject.transform);

            PhysicsHand physicsHandComponent = rootObject.GetComponent<PhysicsHand>();

            physicsHandComponent.Handedness = handedness;

            palmGameObject.name = $"{(handedness == Chirality.Left ? "Left" : "Right")} Palm";

            Leap.Hand leapHand = TestHandFactory.MakeTestHand(isLeft: handedness == Chirality.Left ? true : false, pose: TestHandFactory.TestHandPose.HeadMountedB);

            Transform palmTransform = palmGameObject.GetComponent<Transform>();
            palmTransform.position = leapHand.PalmPosition;
            palmTransform.rotation = leapHand.Rotation;

            PhysicsHand.Hand physicsHand = new PhysicsHand.Hand()
            {
                jointBones = new PhysicsBone[PhysicsHand.Hand.FINGERS * PhysicsHand.Hand.BONES],
                jointColliders = new CapsuleCollider[PhysicsHand.Hand.FINGERS * PhysicsHand.Hand.BONES],
                jointBodies = new ArticulationBody[PhysicsHand.Hand.FINGERS * PhysicsHand.Hand.BONES],
                defaultRotations = new Quaternion[PhysicsHand.Hand.FINGERS + 1],
                overRotationFrameCount = new int[PhysicsHand.Hand.FINGERS * PhysicsHand.Hand.BONES],
                gameObject = palmGameObject,
                transform = palmTransform,
                rootObject = rootObject,
                boneMass = boneMass,
                strength = strength,
                forceLimit = forceLimit,
                stiffness = stiffness,
                physicMaterial = CreateHandPhysicsMaterial()
            };

            if (palmTransform.parent != null)
            {
                palmTransform.localScale = new Vector3(
                    1f / palmTransform.parent.lossyScale.x,
                    1f / palmTransform.parent.lossyScale.y,
                    1f / palmTransform.parent.lossyScale.z);
            }

            physicsHand.palmCollider = palmGameObject.GetComponent<BoxCollider>();
            SetupPalmCollider(physicsHand.palmCollider, leapHand, physicsHand.physicMaterial);

            physicsHand.palmBody = palmGameObject.GetComponent<ArticulationBody>();
            SetupPalmBody(physicsHand.palmBody, boneMass * 3f);

            physicsHand.palmBone = palmGameObject.AddComponent<PhysicsBone>();
            physicsHand.palmBone.SetBoneIndexes(5, 0);

            physicsHand.defaultRotations[PhysicsHand.Hand.FINGERS] = leapHand.Rotation;

            for (int fingerIndex = 0; fingerIndex < PhysicsHand.Hand.FINGERS; fingerIndex++)
            {
                Transform lastTransform = palmTransform;
                Bone knuckleBone = leapHand.Fingers[fingerIndex].Bone((Bone.BoneType)(0));

                for (int jointIndex = 0; jointIndex < PhysicsHand.Hand.BONES; jointIndex++)
                {
                    Bone prevBone = leapHand.Fingers[fingerIndex].Bone((Bone.BoneType)(jointIndex));
                    Bone bone = leapHand.Fingers[fingerIndex].Bone((Bone.BoneType)(jointIndex + 1)); // +1 to skip first bone.

                    int boneArrayIndex = fingerIndex * PhysicsHand.Hand.BONES + jointIndex;

                    GameObject capsuleGameObject = new GameObject("", typeof(CapsuleCollider));
                    capsuleGameObject.name = $"{(handedness == Chirality.Left ? "Left" : "Right")} {IndexToFinger(fingerIndex)} Joint {jointIndex}";
                    if (parent != null)
                    {
                        capsuleGameObject.layer = parent.layer;
                    }

                    capsuleGameObject.transform.parent = lastTransform;

                    if (jointIndex == 0)
                    {
                        capsuleGameObject.transform.position = fingerIndex == 0 ? knuckleBone.PrevJoint : knuckleBone.NextJoint;
                    }
                    else
                    {
                        capsuleGameObject.transform.localPosition = Vector3.forward * prevBone.Length;
                    }

                    capsuleGameObject.transform.rotation = knuckleBone.Rotation;

                    if (capsuleGameObject.transform.parent != null)
                    {
                        capsuleGameObject.transform.localScale = new Vector3(
                            1f / capsuleGameObject.transform.parent.lossyScale.x,
                            1f / capsuleGameObject.transform.parent.lossyScale.y,
                            1f / capsuleGameObject.transform.parent.lossyScale.z);
                    }

                    physicsHand.jointColliders[boneArrayIndex] = capsuleGameObject.GetComponent<CapsuleCollider>();
                    SetupBoneCollider(physicsHand.jointColliders[boneArrayIndex], bone, physicsHand.physicMaterial);

                    physicsHand.jointBodies[boneArrayIndex] = capsuleGameObject.AddComponent<ArticulationBody>();
                    SetupBoneBody(physicsHand.jointBodies[boneArrayIndex], boneMass);

                    physicsHand.jointBones[boneArrayIndex] = capsuleGameObject.AddComponent<PhysicsBone>();
                    physicsHand.jointBones[boneArrayIndex].SetBoneIndexes(fingerIndex, jointIndex);

                    if (jointIndex == 0)
                    {
                        SetupKnuckleDrives(physicsHand.jointBodies[boneArrayIndex], leapHand.IsLeft, fingerIndex, stiffness, forceLimit, strength);

                        physicsHand.defaultRotations[fingerIndex] = knuckleBone.Rotation;
                    }
                    else
                    {
                        SetupBoneDrives(physicsHand.jointBodies[boneArrayIndex], stiffness, forceLimit, strength);
                    }
                    lastTransform = capsuleGameObject.transform;
                }
            }

            physicsHandComponent.SetPhysicsHand(physicsHand);

            return physicsHandComponent;
        }

        // Magic 0th thumb bone rotation offsets from LeapC
        public const float HAND_ROTATION_OFFSET_Y = 25.9f, HAND_ROTATION_OFFSET_Z = -63.45f;

        public static void SetupHand(PhysicsHand.Hand physicsHand, Leap.Hand leapHand, int solverIterations = 50, int solverVelocity = 20)
        {
            // A large amount of this function is done to reset the hand to the correct values if they have been changed in the editor
            // Move the root of the hand
            physicsHand.transform.position = leapHand.PalmPosition;
            physicsHand.transform.rotation = leapHand.Rotation;

            physicsHand.overRotationFrameCount = new int[PhysicsHand.Hand.FINGERS * PhysicsHand.Hand.BONES];

            if (physicsHand.physicMaterial == null)
            {
                physicsHand.physicMaterial = CreateHandPhysicsMaterial();
            }

            if (physicsHand.transform.parent != null)
            {
                physicsHand.gameObject.layer = physicsHand.transform.parent.gameObject.layer;
                physicsHand.transform.localScale = new Vector3(
                    1f / physicsHand.transform.parent.lossyScale.x,
                    1f / physicsHand.transform.parent.lossyScale.y,
                    1f / physicsHand.transform.parent.lossyScale.z);
            }

            SetupPalmCollider(physicsHand.palmCollider, leapHand, physicsHand.physicMaterial);

            SetupPalmBody(physicsHand.palmBody, physicsHand.boneMass * 3f, solverIterations: solverIterations, solverVelocity: solverVelocity);
            physicsHand.palmBone.SetBoneIndexes(5, 0);
            physicsHand.palmBody.WakeUp();

            for (int fingerIndex = 0; fingerIndex < PhysicsHand.Hand.FINGERS; fingerIndex++)
            {
                Transform lastTransform = physicsHand.transform;
                Bone knuckleBone = leapHand.Fingers[fingerIndex].Bone((Bone.BoneType)(0));

                for (int jointIndex = 0; jointIndex < PhysicsHand.Hand.BONES; jointIndex++)
                {
                    Bone prevBone = leapHand.Fingers[fingerIndex].Bone((Bone.BoneType)(jointIndex));
                    Bone bone = leapHand.Fingers[fingerIndex].Bone((Bone.BoneType)(jointIndex + 1)); // +1 to skip first bone.

                    int boneArrayIndex = fingerIndex * PhysicsHand.Hand.BONES + jointIndex;

                    GameObject capsuleGameObject = physicsHand.jointBones[boneArrayIndex].gameObject;
                    capsuleGameObject.layer = physicsHand.gameObject.layer;

                    capsuleGameObject.transform.parent = lastTransform;

                    if (jointIndex == 0)
                    {
                        capsuleGameObject.transform.position = fingerIndex == 0 ? knuckleBone.PrevJoint : knuckleBone.NextJoint;
                    }
                    else
                    {
                        capsuleGameObject.transform.localPosition = Vector3.forward * prevBone.Length;
                    }

                    capsuleGameObject.transform.rotation = knuckleBone.Rotation;

                    if (capsuleGameObject.transform.parent != null)
                    {
                        capsuleGameObject.transform.localScale = new Vector3(
                            1f / capsuleGameObject.transform.parent.lossyScale.x,
                            1f / capsuleGameObject.transform.parent.lossyScale.y,
                            1f / capsuleGameObject.transform.parent.lossyScale.z);
                    }

                    SetupBoneCollider(physicsHand.jointColliders[boneArrayIndex], bone, physicsHand.physicMaterial);

                    SetupBoneBody(physicsHand.jointBodies[boneArrayIndex], physicsHand.boneMass, solverIterations: solverIterations, solverVelocity: solverVelocity);

                    if (jointIndex == 0)
                    {
                        SetupKnuckleDrives(physicsHand.jointBodies[boneArrayIndex], leapHand.IsLeft, fingerIndex, physicsHand.stiffness, physicsHand.forceLimit, physicsHand.strength);

                        physicsHand.jointBodies[boneArrayIndex].parentAnchorPosition = InverseTransformPoint(leapHand.PalmPosition, leapHand.Rotation, knuckleBone.NextJoint);
                        if (fingerIndex == 0)
                        {
                            physicsHand.jointBodies[boneArrayIndex].parentAnchorRotation = Quaternion.Euler(0, leapHand.IsLeft ? HAND_ROTATION_OFFSET_Y : -HAND_ROTATION_OFFSET_Y, leapHand.IsLeft ? HAND_ROTATION_OFFSET_Z : -HAND_ROTATION_OFFSET_Z);
                        }
                    }
                    else
                    {
                        SetupBoneDrives(physicsHand.jointBodies[boneArrayIndex], physicsHand.stiffness, physicsHand.forceLimit, physicsHand.strength);

                        physicsHand.jointBodies[boneArrayIndex].parentAnchorPosition = InverseTransformPoint(prevBone.PrevJoint, prevBone.Rotation, bone.PrevJoint);
                        physicsHand.jointBodies[boneArrayIndex].parentAnchorRotation = Quaternion.identity;
                    }

                    physicsHand.jointBones[boneArrayIndex].SetBoneIndexes(fingerIndex, jointIndex);

                    physicsHand.jointBodies[boneArrayIndex].WakeUp();

                    lastTransform = capsuleGameObject.transform;
                }
            }

            // Set the colliders to ignore eachother
            foreach (var collider in physicsHand.jointColliders)
            {
                Physics.IgnoreCollision(physicsHand.palmCollider, collider);

                foreach (var collider2 in physicsHand.jointColliders)
                {
                    if (collider != collider2)
                    {
                        Physics.IgnoreCollision(collider, collider2);
                    }
                }
            }
        }

        public static PhysicMaterial CreateHandPhysicsMaterial()
        {
            PhysicMaterial material = new PhysicMaterial("HandPhysics");

            material.dynamicFriction = 1f;
            material.staticFriction = 1f;
            material.frictionCombine = PhysicMaterialCombine.Average;
            material.bounceCombine = PhysicMaterialCombine.Minimum;

            return material;
        }

        #endregion

        #region Physics Setup

        public static void SetupPalmBody(ArticulationBody palm, float boneMass = 1.8f, int solverIterations = 50, int solverVelocity = 20, float angularDamping = 50f)
        {
            palm.immovable = false;
#if UNITY_2021_2_OR_NEWER
            palm.matchAnchors = false;
#endif
            palm.mass = boneMass;
            palm.solverIterations = solverIterations;
            palm.solverVelocityIterations = solverVelocity;
            palm.linearDamping = 0f;
            palm.angularDamping = angularDamping;
            palm.useGravity = false;
            palm.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            palm.maxDepenetrationVelocity = 0.001f;
        }

        public static void SetupBoneBody(ArticulationBody bone, float boneMass = 0.6f, int solverIterations = 50, int solverVelocity = 20, float maxAngularVelocity = 1.75f, float maxDepenetrationVelocity = 3f)
        {
            bone.anchorPosition = new Vector3(0f, 0f, 0f);
            bone.anchorRotation = Quaternion.identity;
#if UNITY_2021_2_OR_NEWER
            bone.matchAnchors = false;
#endif
            bone.mass = boneMass;
            bone.solverIterations = solverIterations;
            bone.solverVelocityIterations = solverVelocity;
            bone.maxAngularVelocity = maxAngularVelocity;
            bone.maxDepenetrationVelocity = maxDepenetrationVelocity;
            bone.useGravity = false;
            bone.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            bone.maxDepenetrationVelocity = 0.001f;
            bone.linearDamping = 0f;
        }

        public static void SetupKnuckleDrives(ArticulationBody knuckle, bool isLeft, int fingerIndex, float stiffness, float forceLimit, float strength)
        {
            knuckle.jointType = ArticulationJointType.SphericalJoint;
            knuckle.twistLock = ArticulationDofLock.LimitedMotion;
            knuckle.swingYLock = ArticulationDofLock.LimitedMotion;
            knuckle.swingZLock = ArticulationDofLock.LimitedMotion;

            ArticulationDrive xDrive = new ArticulationDrive()
            {
                stiffness = stiffness * strength,
                forceLimit = forceLimit * strength / Time.fixedDeltaTime,
                damping = 1f,
                lowerLimit = -30f,
                upperLimit = 80f
            };

            if (fingerIndex == 0)
            {
                xDrive.lowerLimit = -45f;
                xDrive.upperLimit = 45f;
            }

            knuckle.xDrive = xDrive;

            ArticulationDrive yDrive = new ArticulationDrive()
            {
                stiffness = stiffness * strength,
                forceLimit = forceLimit * strength / Time.fixedDeltaTime,
                damping = 2f,
                lowerLimit = -15f,
                upperLimit = 15f
            };

            if (fingerIndex == 0)
            {
                yDrive.lowerLimit = isLeft ? -10f : -50f;
                yDrive.upperLimit = isLeft ? 50f : 10f;
            }

            knuckle.yDrive = yDrive;

            // Set Z limits to 0, locking them causes insane jittering
            yDrive.lowerLimit = 0f;
            yDrive.upperLimit = 0f;
            knuckle.zDrive = yDrive;
        }

        public static void SetupBoneDrives(ArticulationBody bone, float stiffness, float forceLimit, float strength)
        {
            bone.jointType = ArticulationJointType.SphericalJoint;
            bone.twistLock = ArticulationDofLock.LimitedMotion;
            bone.swingYLock = ArticulationDofLock.LimitedMotion;
            bone.swingZLock = ArticulationDofLock.LimitedMotion;

            ArticulationDrive xDrive = new ArticulationDrive()
            {
                stiffness = stiffness * strength,
                forceLimit = forceLimit * strength / Time.fixedDeltaTime,
                damping = 1f,
                lowerLimit = -10f,
                upperLimit = 89f
            };

            bone.xDrive = xDrive;

            ArticulationDrive yDrive = new ArticulationDrive()
            {
                stiffness = stiffness * strength,
                forceLimit = forceLimit * strength / Time.fixedDeltaTime,
                damping = 2f,
                lowerLimit = -8f,
                upperLimit = 8f
            };

            bone.yDrive = yDrive;

            // Set Z limits to 0, locking them causes insane jittering
            yDrive.lowerLimit = 0f;
            yDrive.upperLimit = 0f;
            bone.zDrive = yDrive;
        }

        public static void SetupPalmCollider(BoxCollider collider, Hand hand, PhysicMaterial material = null)
        {
            collider.center = new Vector3(0f, 0.0025f, -0.015f);
            collider.size = CalculatePalmSize(hand);
            if (material != null)
            {
                collider.material = material;
            }
        }

        public static void SetupBoneCollider(CapsuleCollider collider, Bone bone, PhysicMaterial material = null)
        {
            collider.direction = 2;
            collider.radius = bone.Width * 0.5f;
            collider.height = bone.Length + bone.Width;
            collider.center = new Vector3(0f, 0f, bone.Length / 2f);
            if (material != null)
            {
                collider.material = material;
            }
        }

        #endregion

        #region Hand Resizing

        public static void ResetPhysicsHandSizes(PhysicsHand.Hand physicsHand, Leap.Hand leapHand)
        {
            SetupPalmCollider(physicsHand.palmCollider, leapHand);
            physicsHand.overRotationFrameCount = new int[PhysicsHand.Hand.FINGERS * PhysicsHand.Hand.BONES];
            for (int fingerIndex = 0; fingerIndex < PhysicsHand.Hand.FINGERS; fingerIndex++)
            {
                Transform lastTransform = physicsHand.palmBone.transform;
                Bone knuckleBone = leapHand.Fingers[fingerIndex].Bone(0);

                for (int jointIndex = 0; jointIndex < PhysicsHand.Hand.BONES; jointIndex++)
                {
                    Bone prevBone = leapHand.Fingers[fingerIndex].Bone((Bone.BoneType)(jointIndex));
                    Bone bone = leapHand.Fingers[fingerIndex].Bone((Bone.BoneType)(jointIndex + 1)); // +1 to skip first bone.

                    int boneArrayIndex = fingerIndex * PhysicsHand.Hand.BONES + jointIndex;

                    GameObject capsuleGameObject = physicsHand.jointColliders[boneArrayIndex].gameObject;

                    capsuleGameObject.transform.parent = lastTransform;

                    if (jointIndex == 0)
                    {
                        capsuleGameObject.transform.position = fingerIndex == 0 ? knuckleBone.PrevJoint : knuckleBone.NextJoint;
                    }
                    else
                    {
                        capsuleGameObject.transform.localPosition = physicsHand.transform.InverseTransformPoint(prevBone.PrevJoint);
                    }

                    capsuleGameObject.transform.rotation = knuckleBone.Rotation;

                    if (capsuleGameObject.transform.parent != null)
                    {
                        capsuleGameObject.transform.localScale = new Vector3(
                            1f / capsuleGameObject.transform.parent.lossyScale.x,
                            1f / capsuleGameObject.transform.parent.lossyScale.y,
                            1f / capsuleGameObject.transform.parent.lossyScale.z);
                    }

                    SetupBoneCollider(physicsHand.jointColliders[boneArrayIndex], bone);

                    // Move the anchor positions to account for hand sizes
                    if (jointIndex > 0)
                    {
                        physicsHand.jointBodies[boneArrayIndex].parentAnchorPosition = InverseTransformPoint(prevBone.PrevJoint, prevBone.Rotation, bone.PrevJoint);
                        physicsHand.jointBodies[boneArrayIndex].parentAnchorRotation = Quaternion.identity;
                    }
                    else
                    {
                        physicsHand.jointBodies[boneArrayIndex].parentAnchorPosition = InverseTransformPoint(leapHand.PalmPosition, leapHand.Rotation, knuckleBone.NextJoint);
                        if (fingerIndex == 0)
                        {
                            physicsHand.jointBodies[boneArrayIndex].parentAnchorRotation = Quaternion.Euler(0, leapHand.IsLeft ? HAND_ROTATION_OFFSET_Y : -HAND_ROTATION_OFFSET_Y, leapHand.IsLeft ? HAND_ROTATION_OFFSET_Z : -HAND_ROTATION_OFFSET_Z);
                        }
                    }

                    lastTransform = capsuleGameObject.transform;
                }
            }
        }

        public static Vector3 CalculateAverageKnucklePosition(Hand hand)
        {
            return (hand.Fingers[1].bones[0].NextJoint + hand.Fingers[2].bones[0].NextJoint + hand.Fingers[3].bones[0].NextJoint + hand.Fingers[4].bones[0].NextJoint) / 4;
        }

        public static Vector3 CalculatePalmSize(Hand hand)
        {
            return new Vector3(hand.PalmWidth * 0.98f, 0.025f, Vector3.Distance(CalculateAverageKnucklePosition(hand), hand.WristPosition));
        }

        public static void InterpolateKnucklePosition(ArticulationBody body, PhysicsBone bone, Leap.Hand leapHand, float deltaTime)
        {
            if (bone != null && bone.ContactingObjects.Count > 0)
            {
                // Stop bones sizing if they're touching things
                // Has the benefit of stopping small objects falling through bones
                return;
            }
            Bone knuckleBone = leapHand.Fingers[bone.Finger].bones[bone.Joint];
            body.parentAnchorPosition = Vector3.Lerp(body.parentAnchorPosition, InverseTransformPoint(leapHand.PalmPosition, leapHand.Rotation, bone.Finger == 0 ? knuckleBone.PrevJoint : knuckleBone.NextJoint), deltaTime);
        }

        public static void InterpolateBoneSize(ArticulationBody body, PhysicsBone bone, CapsuleCollider collider, Vector3 parentPosition, Quaternion parentRotation, Vector3 childPosition, float width, float length, float deltaTime)
        {
            if (bone != null && bone.ContactingObjects.Count > 0)
            {
                // Stop bones sizing if they're touching things
                // Has the benefit of stopping small objects falling through bones
                return;
            }
            body.parentAnchorPosition = Vector3.Lerp(body.parentAnchorPosition, InverseTransformPoint(parentPosition, parentRotation, childPosition), deltaTime);
            collider.radius = Mathf.Lerp(collider.radius, width * 0.5f, deltaTime);
            collider.height = Mathf.Lerp(collider.height, length + width, deltaTime);
            collider.center = Vector3.Lerp(collider.center, new Vector3(0, 0, length / 2f), deltaTime);
        }

        #endregion

        #region Hand Updating

        public static void UpdateIterations(ref PhysicsHand.Hand physicsHand, int solverIterations, int velocityIterations)
        {
            physicsHand.palmBody.solverIterations = solverIterations;
            physicsHand.palmBody.solverVelocityIterations = velocityIterations;

            for (int i = 0; i < physicsHand.jointBodies.Length; i++)
            {
                physicsHand.jointBodies[i].solverIterations = solverIterations;
                physicsHand.jointBodies[i].solverVelocityIterations = velocityIterations;
            }
        }

        public static void UpdatePhysicsPalm(ref PhysicsHand.Hand physicsHand, Vector3 position, Quaternion rotation, float interpFactor = 0f, float distanceForceReduction = 0f)
        {
            // Apply tracking position velocity
            physicsHand.palmBody.velocity *= 0;

            Vector3 delta = position - physicsHand.transform.position;

            if (interpFactor > 0)
            {
                delta.x *= (1f - interpFactor);
                if (delta.y > 0)
                {
                    delta.y *= (1f - interpFactor);
                }
                delta.z *= (1f - interpFactor);
            }

            if (distanceForceReduction > 0)
            {
                delta *= 1f - distanceForceReduction;
            }

            physicsHand.palmBody.AddForce(Vector3.ClampMagnitude((delta) / Time.fixedDeltaTime / Time.fixedDeltaTime * physicsHand.palmBody.mass,
                1000f * physicsHand.strength));

            // Apply tracking rotation velocity
            // TODO: Compensate for phantom forces on strongly misrotated appendages
            Quaternion rotationDelta = rotation * Quaternion.Inverse(physicsHand.transform.rotation);
            rotationDelta = Quaternion.Slerp(Quaternion.identity, rotationDelta, 1f - interpFactor);
            physicsHand.palmBody.angularVelocity = Vector3.ClampMagnitude((new Vector3(
                Mathf.DeltaAngle(0, rotationDelta.eulerAngles.x),
                Mathf.DeltaAngle(0, rotationDelta.eulerAngles.y),
                Mathf.DeltaAngle(0, rotationDelta.eulerAngles.z)) / Time.fixedDeltaTime) * Mathf.Deg2Rad, 45f * physicsHand.strength);
        }

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

        #endregion

        #region Hand Conversion

        public static void ConvertPhysicsToLeapHand(PhysicsHand.Hand physicsHand, ref Leap.Hand leapHand, Leap.Hand originalHand, float delta)
        {
            leapHand.SetTransform(physicsHand.transform.position, physicsHand.transform.rotation);
            int boneInd = 0;
            Vector3 posA, posB;

            float r;
            for (int i = 0; i < leapHand.Fingers.Count; i++)
            {
                Bone b = leapHand.Fingers[i].bones[0];
                PhysExts.ToWorldSpaceCapsule(physicsHand.jointColliders[boneInd], out posA, out posB, out r);
                b.NextJoint = posB;

                for (int j = 1; j < leapHand.Fingers[i].bones.Length; j++)
                {
                    b = leapHand.Fingers[i].bones[j];
                    PhysExts.ToWorldSpaceCapsule(physicsHand.jointColliders[boneInd], out posA, out posB, out r);
                    b.PrevJoint = posB;
                    b.NextJoint = posA;
                    b.Width = r;
                    b.Center = (b.PrevJoint + b.NextJoint) / 2f;
                    b.Direction = b.PrevJoint - b.NextJoint;
                    b.Length = Vector3.Distance(posA, posB);
                    b.Rotation = physicsHand.jointColliders[boneInd].transform.rotation;
                    boneInd++;
                }
                leapHand.Fingers[i].TipPosition = physicsHand.GetTipPosition(i);
            }
            leapHand.Arm.CopyFrom(originalHand.Arm);

            leapHand.Arm.NextJoint = leapHand.WristPosition;
            leapHand.Arm.Direction = (leapHand.WristPosition - leapHand.Arm.PrevJoint).normalized;
            leapHand.Arm.Rotation = Quaternion.LookRotation(leapHand.Arm.Direction, -leapHand.PalmNormal);

            leapHand.PalmWidth = physicsHand.palmCollider.size.y;
            leapHand.Confidence = originalHand.Confidence;
            leapHand.Direction = originalHand.Direction;
            leapHand.FrameId = originalHand.FrameId;
            leapHand.GrabStrength = originalHand.GrabStrength;
            leapHand.Id = originalHand.Id;
            leapHand.PinchStrength = CalculatePinchStrength(leapHand, physicsHand.palmCollider.size.y);
            leapHand.PinchDistance = CalculatePinchDistance(leapHand);
            leapHand.PalmVelocity = (physicsHand.transform.position - physicsHand.oldPosition) / delta;
            leapHand.TimeVisible = originalHand.TimeVisible;
        }

        private static float CalculatePinchStrength(Hand hand, float palmWidth)
        {
            // Magic values taken from existing LeapC implementation (scaled to metres)
            float handScale = palmWidth / 0.08425f;
            float distanceZero = 0.0600f * handScale;
            float distanceOne = 0.0220f * handScale;

            // Get the thumb position.
            var thumbTipPosition = hand.GetThumb().TipPosition;

            // Compute the distance midpoints between the thumb and the each finger and find the smallest.
            var minDistanceSquared = float.MaxValue;
            foreach (var finger in hand.Fingers.Skip(1))
            {
                var distanceSquared = (finger.TipPosition - thumbTipPosition).sqrMagnitude;
                minDistanceSquared = Mathf.Min(distanceSquared, minDistanceSquared);
            }

            // Compute the pinch strength.
            return Mathf.Clamp01((Mathf.Sqrt(minDistanceSquared) - distanceZero) / (distanceOne - distanceZero));
        }

        private static float CalculateBoneDistanceSquared(Bone boneA, Bone boneB)
        {
            // Denormalize directions to bone length.
            var boneAJoint = boneA.PrevJoint;
            var boneBJoint = boneB.PrevJoint;
            var boneADirection = boneA.Direction * boneA.Length;
            var boneBDirection = boneB.Direction * boneB.Length;

            // Compute the minimum (squared) distance between two bones.
            var diff = boneBJoint - boneAJoint;
            var d1 = Vector3.Dot(boneADirection, diff);
            var d2 = Vector3.Dot(boneBDirection, diff);
            var a = boneADirection.sqrMagnitude;
            var b = Vector3.Dot(boneADirection, boneBDirection);
            var c = boneBDirection.sqrMagnitude;
            var det = b * b - a * c;
            var t1 = Mathf.Clamp01((b * d2 - c * d1) / det);
            var t2 = Mathf.Clamp01((a * d2 - b * d1) / det);
            var pa = boneAJoint + t1 * boneADirection;
            var pb = boneBJoint + t2 * boneBDirection;
            return (pa - pb).sqrMagnitude;
        }

        private static float CalculatePinchDistance(Hand hand)
        {
            // Get the farthest 2 segments of thumb and index finger, respectively, and compute distances.
            var minDistanceSquared = float.MaxValue;
            foreach (var thumbBone in hand.GetThumb().bones.Skip(2))
            {
                foreach (var indexBone in hand.GetIndex().bones.Skip(2))
                {
                    var distanceSquared = CalculateBoneDistanceSquared(thumbBone, indexBone);
                    minDistanceSquared = Mathf.Min(distanceSquared, minDistanceSquared);
                }
            }

            // Return the pinch distance, converted to millimeters to match other providers.
            return Mathf.Sqrt(minDistanceSquared) * 1000.0f;
        }

        public static Vector3 GetTipPosition(this PhysicsHand.Hand hand, int index)
        {
            if (index > 4)
            {
                return Vector3.zero;
            }
            PhysExts.ToWorldSpaceCapsule(hand.jointColliders[(PhysicsHand.Hand.BONES * index) + PhysicsHand.Hand.BONES - 1], out Vector3 outPos, out var outB, out var outRadius);
            outPos += (outPos - outB).normalized * outRadius;
            return outPos;
        }

        #endregion

        private static string IndexToFinger(int index)
        {
            switch (index)
            {
                case 0:
                    return "Thumb";
                case 1:
                    return "Index";
                case 2:
                    return "Middle";
                case 3:
                    return "Ring";
                case 4:
                    return "Pinky";
            }
            return "";
        }

        private static Vector3 InverseTransformPoint(Vector3 transformPos, Quaternion transformRotation, Vector3 transformScale, Vector3 pos)
        {
            Matrix4x4 matrix = Matrix4x4.TRS(transformPos, transformRotation, transformScale);
            Matrix4x4 inverse = matrix.inverse;
            return inverse.MultiplyPoint3x4(pos);
        }

        private static Vector3 InverseTransformPoint(Vector3 transformPos, Quaternion transformRotation, Vector3 pos)
        {
            return InverseTransformPoint(transformPos, transformRotation, Vector3.one, pos);
        }

        public static float EaseOut(this float input)
        {
            return input.Flip().Square().Flip();
        }

        public static float Square(this float input)
        {
            return input * input;
        }

        public static float Flip(this float input)
        {
            return 1 - input;
        }



    }
}