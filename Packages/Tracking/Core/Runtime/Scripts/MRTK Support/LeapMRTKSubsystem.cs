using MixedReality.Toolkit;
using MixedReality.Toolkit.Input;
using MixedReality.Toolkit.Subsystems;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Scripting;
using UnityEngine.XR;

namespace Leap.Unity.MRTK
{
    [Preserve]
    [MRTKSubsystem(
        Name = "com.ultraleap.mrtkhands",
        DisplayName = "Subsystem for Ultraleap Hands API (Non-OpenXR)",
        Author = "Ultraleap",
        ProviderType = typeof(LeapMRTKProvider),
        SubsystemTypeOverride = typeof(LeapMRTKSubsystem),
        ConfigType = typeof(BaseSubsystemConfig))]
    public class LeapMRTKSubsystem : HandsSubsystem
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Register()
        {
            bool subsystemEnabled = MRTKProfile.Instance.LoadedSubsystems.Contains(typeof(LeapMRTKSubsystem));

            if (!subsystemEnabled)
            {
                return;
            }

            // Fetch subsystem metadata from the attribute.
            var cinfo = XRSubsystemHelpers.ConstructCinfo<LeapMRTKSubsystem, HandsSubsystemCinfo>();

            // Populate remaining cinfo field.
            cinfo.IsPhysicalData = true;

            if (!Register(cinfo))
            {
                Debug.LogError($"Failed to register the {cinfo.Name} subsystem.");
            }
        }

        private class LeapHandContainer : HandDataContainer
        {
            public LeapHandContainer(XRNode handNode) : base(handNode)
            {
                handChirality = handNode == XRNode.LeftHand ? Chirality.Left : Chirality.Right;
            }

            Chirality handChirality;
            public LeapProvider provider;

            public override bool TryGetEntireHand(out IReadOnlyList<HandJointPose> joints)
            {
                if (!AlreadyFullQueried)
                {
                    TryCalculateEntireHand();
                }

                joints = HandJoints;
                return FullQueryValid;
            }

            public override bool TryGetJoint(TrackedHandJoint joint, out HandJointPose pose)
            {
                bool thisQueryValid = false;
                int index = (int)joint;

                // If we happened to have already queried the entire
                // hand data this frame, we don't need to re-query for
                // just the joint. If we haven't, we do still need to
                // query for the single joint.
                if (!AlreadyFullQueried)
                {
                    Leap.Hand currentHand = provider?.GetHand(handChirality);

                    if (currentHand == null)
                    {
                        pose = HandJoints[index];
                        return false;
                    }

                    Pose jointPose = GetJointFromLeapHand(joint, currentHand);
                    UpdateJoint(index, jointPose);
                    thisQueryValid = true;
                }
                else
                {
                    // If we've already run a full-hand query, this single joint query
                    // is just as valid as the full query.
                    thisQueryValid = FullQueryValid;
                }

                pose = HandJoints[index];
                return thisQueryValid;
            }

            private void TryCalculateEntireHand()
            {
                Leap.Hand currentHand = provider?.GetHand(handChirality);

                if (currentHand == null)
                {
                    FullQueryValid = false;
                    AlreadyFullQueried = true;
                    return;
                }

                for (int i = 0; i < (int)TrackedHandJoint.TotalJoints - 1; i++)
                {
                    Pose jointPose = GetJointFromLeapHand((TrackedHandJoint)i, currentHand);

                    UpdateJoint(i, jointPose);
                }

                // Mark this hand as having been fully queried this frame.
                // If any joint is queried again this frame, we'll reuse the
                // information to avoid extra work.
                FullQueryValid = true;
                AlreadyFullQueried = true;
            }

            private void UpdateJoint(int jointIndex, Pose handJointLocation)
            {
                HandJoints[jointIndex] = new HandJointPose(
                   handJointLocation.position,
                   handJointLocation.rotation,
                    0.005f);
            }

            Pose GetJointFromLeapHand(TrackedHandJoint joint, Leap.Hand hand)
            {
                switch (joint)
                {
                    case TrackedHandJoint.Palm:
                        return new Pose(hand.PalmPosition, hand.Rotation);
                    case TrackedHandJoint.Wrist:
                        return new Pose(hand.WristPosition, hand.Arm.Rotation);
                    case TrackedHandJoint.ThumbMetacarpal:
                        return new Pose(hand.GetThumb().Bone(Leap.Bone.BoneType.TYPE_PROXIMAL).PrevJoint, hand.GetThumb().Bone(Leap.Bone.BoneType.TYPE_PROXIMAL).Rotation);
                    case TrackedHandJoint.ThumbProximal:
                        return new Pose(hand.GetThumb().Bone(Leap.Bone.BoneType.TYPE_INTERMEDIATE).PrevJoint, hand.GetThumb().Bone(Leap.Bone.BoneType.TYPE_INTERMEDIATE).Rotation);
                    case TrackedHandJoint.ThumbDistal:
                        return new Pose(hand.GetThumb().Bone(Leap.Bone.BoneType.TYPE_DISTAL).PrevJoint, hand.GetThumb().Bone(Leap.Bone.BoneType.TYPE_DISTAL).Rotation);
                    case TrackedHandJoint.ThumbTip:
                        return new Pose(hand.GetThumb().Bone(Leap.Bone.BoneType.TYPE_DISTAL).NextJoint, hand.GetThumb().Bone(Leap.Bone.BoneType.TYPE_DISTAL).Rotation);
                    case TrackedHandJoint.IndexMetacarpal:
                        return new Pose(hand.GetIndex().Bone(Leap.Bone.BoneType.TYPE_METACARPAL).PrevJoint, hand.GetIndex().Bone(Leap.Bone.BoneType.TYPE_METACARPAL).Rotation);
                    case TrackedHandJoint.IndexProximal:
                        return new Pose(hand.GetIndex().Bone(Leap.Bone.BoneType.TYPE_PROXIMAL).PrevJoint, hand.GetIndex().Bone(Leap.Bone.BoneType.TYPE_PROXIMAL).Rotation);
                    case TrackedHandJoint.IndexIntermediate:
                        return new Pose(hand.GetIndex().Bone(Leap.Bone.BoneType.TYPE_INTERMEDIATE).PrevJoint, hand.GetIndex().Bone(Leap.Bone.BoneType.TYPE_INTERMEDIATE).Rotation);
                    case TrackedHandJoint.IndexDistal:
                        return new Pose(hand.GetIndex().Bone(Leap.Bone.BoneType.TYPE_DISTAL).PrevJoint, hand.GetIndex().Bone(Leap.Bone.BoneType.TYPE_DISTAL).Rotation);
                    case TrackedHandJoint.IndexTip:
                        return new Pose(hand.GetIndex().Bone(Leap.Bone.BoneType.TYPE_DISTAL).NextJoint, hand.GetIndex().Bone(Leap.Bone.BoneType.TYPE_DISTAL).Rotation);
                    case TrackedHandJoint.MiddleMetacarpal:
                        return new Pose(hand.GetMiddle().Bone(Leap.Bone.BoneType.TYPE_METACARPAL).PrevJoint, hand.GetMiddle().Bone(Leap.Bone.BoneType.TYPE_METACARPAL).Rotation);
                    case TrackedHandJoint.MiddleProximal:
                        return new Pose(hand.GetMiddle().Bone(Leap.Bone.BoneType.TYPE_PROXIMAL).PrevJoint, hand.GetMiddle().Bone(Leap.Bone.BoneType.TYPE_PROXIMAL).Rotation);
                    case TrackedHandJoint.MiddleIntermediate:
                        return new Pose(hand.GetMiddle().Bone(Leap.Bone.BoneType.TYPE_INTERMEDIATE).PrevJoint, hand.GetMiddle().Bone(Leap.Bone.BoneType.TYPE_INTERMEDIATE).Rotation);
                    case TrackedHandJoint.MiddleDistal:
                        return new Pose(hand.GetMiddle().Bone(Leap.Bone.BoneType.TYPE_DISTAL).PrevJoint, hand.GetMiddle().Bone(Leap.Bone.BoneType.TYPE_DISTAL).Rotation);
                    case TrackedHandJoint.MiddleTip:
                        return new Pose(hand.GetMiddle().Bone(Leap.Bone.BoneType.TYPE_DISTAL).NextJoint, hand.GetMiddle().Bone(Leap.Bone.BoneType.TYPE_DISTAL).Rotation);
                    case TrackedHandJoint.RingMetacarpal:
                        return new Pose(hand.GetRing().Bone(Leap.Bone.BoneType.TYPE_METACARPAL).PrevJoint, hand.GetRing().Bone(Leap.Bone.BoneType.TYPE_METACARPAL).Rotation);
                    case TrackedHandJoint.RingProximal:
                        return new Pose(hand.GetRing().Bone(Leap.Bone.BoneType.TYPE_PROXIMAL).PrevJoint, hand.GetRing().Bone(Leap.Bone.BoneType.TYPE_PROXIMAL).Rotation);
                    case TrackedHandJoint.RingIntermediate:
                        return new Pose(hand.GetRing().Bone(Leap.Bone.BoneType.TYPE_INTERMEDIATE).PrevJoint, hand.GetRing().Bone(Leap.Bone.BoneType.TYPE_INTERMEDIATE).Rotation);
                    case TrackedHandJoint.RingDistal:
                        return new Pose(hand.GetRing().Bone(Leap.Bone.BoneType.TYPE_DISTAL).PrevJoint, hand.GetRing().Bone(Leap.Bone.BoneType.TYPE_DISTAL).Rotation);
                    case TrackedHandJoint.RingTip:
                        return new Pose(hand.GetRing().Bone(Leap.Bone.BoneType.TYPE_DISTAL).NextJoint, hand.GetRing().Bone(Leap.Bone.BoneType.TYPE_DISTAL).Rotation);
                    case TrackedHandJoint.LittleMetacarpal:
                        return new Pose(hand.GetPinky().Bone(Leap.Bone.BoneType.TYPE_METACARPAL).PrevJoint, hand.GetPinky().Bone(Leap.Bone.BoneType.TYPE_METACARPAL).Rotation);
                    case TrackedHandJoint.LittleProximal:
                        return new Pose(hand.GetPinky().Bone(Leap.Bone.BoneType.TYPE_PROXIMAL).PrevJoint, hand.GetPinky().Bone(Leap.Bone.BoneType.TYPE_PROXIMAL).Rotation);
                    case TrackedHandJoint.LittleIntermediate:
                        return new Pose(hand.GetPinky().Bone(Leap.Bone.BoneType.TYPE_INTERMEDIATE).PrevJoint, hand.GetPinky().Bone(Leap.Bone.BoneType.TYPE_INTERMEDIATE).Rotation);
                    case TrackedHandJoint.LittleDistal:
                        return new Pose(hand.GetPinky().Bone(Leap.Bone.BoneType.TYPE_DISTAL).PrevJoint, hand.GetPinky().Bone(Leap.Bone.BoneType.TYPE_DISTAL).Rotation);
                    case TrackedHandJoint.LittleTip:
                        return new Pose(hand.GetPinky().Bone(Leap.Bone.BoneType.TYPE_DISTAL).NextJoint, hand.GetPinky().Bone(Leap.Bone.BoneType.TYPE_DISTAL).Rotation);
                }

                return new Pose(hand.PalmPosition, hand.Rotation);
            }
        }

        /// <summary>
        /// A hand subsystem that extends the <see cref="MixedReality.Toolkit.Subsystems.HandsSubsystem.Provider">Provider</see> class, and 
        /// obtains hand joint poses from the <see cref="LeapHandContainer"/> class.
        /// </summary>
        [Preserve]
        private class LeapMRTKProvider : Provider, IHandsSubsystem
        {
            private static Dictionary<XRNode, LeapHandContainer> hands = null;

            /// <inheritdoc/>
            public override void Start()
            {
                base.Start();

                hands ??= new Dictionary<XRNode, LeapHandContainer>
                {
                    { XRNode.LeftHand, new LeapHandContainer(XRNode.LeftHand) },
                    { XRNode.RightHand, new LeapHandContainer(XRNode.RightHand) }
                };

                InputSystem.onBeforeUpdate += ResetHands;
            }

            public override void Stop()
            {
                ResetHands();
                InputSystem.onBeforeUpdate -= ResetHands;
                base.Stop();
            }

            [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
            private static void RunAfterSceneLoad()
            {
                LeapMRTKSubsystem leapMRTKSubsystem = XRSubsystemHelpers.GetFirstRunningSubsystem<LeapMRTKSubsystem>();

                if (leapMRTKSubsystem == null)
                {
                    return;
                }

                LeapProvider leapProvider = GameObject.FindAnyObjectByType<LeapXRServiceProvider>();

                // If there is no leap provider in the scene
                if (leapProvider == null)
                {
                    Debug.Log("There are no LeapXRServiceProviders in the scene, automatically assigning one for use with Ultraleap Subsystem for MRTK");


                    GameObject leapProviderGO = new GameObject("LeapXRServiceProvider");
                    LeapXRServiceProvider leapXRServiceProvider = leapProviderGO.AddComponent<LeapXRServiceProvider>();
                    leapXRServiceProvider.PositionDeviceRelativeToMainCamera = true;
                    leapProvider = (LeapProvider)leapXRServiceProvider;
                    GameObject.DontDestroyOnLoad(leapProviderGO);
                }
                else
                {
                    Debug.Log("Ultraleap Subsystem for MRTK is using the existing LeapXRServiceProvider found in the current scene");
                }

                foreach (var hand in hands)
                {
                    hand.Value.provider = leapProvider;
                }
            }


            private void ResetHands()
            {
                hands[XRNode.LeftHand].Reset();
                hands[XRNode.RightHand].Reset();
            }

            #region IHandsSubsystem implementation

            /// <inheritdoc/>
            public override bool TryGetEntireHand(XRNode handNode, out IReadOnlyList<HandJointPose> jointPoses)
            {
                Debug.Assert(handNode == XRNode.LeftHand || handNode == XRNode.RightHand, "Non-hand XRNode used in TryGetEntireHand query");

                return hands[handNode].TryGetEntireHand(out jointPoses);
            }

            /// <inheritdoc/>
            public override bool TryGetJoint(TrackedHandJoint joint, XRNode handNode, out HandJointPose jointPose)
            {
                Debug.Assert(handNode == XRNode.LeftHand || handNode == XRNode.RightHand, "Non-hand XRNode used in TryGetJoint query");

                return hands[handNode].TryGetJoint(joint, out jointPose);
            }

            #endregion IHandsSubsystem implementation
        }
    }
}