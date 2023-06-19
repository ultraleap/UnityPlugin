using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.SubsystemsImplementation.Extensions;
using UnityEngine.XR.Hands;
using UnityEngine.XR.Hands.ProviderImplementation;

namespace Leap.Unity
{
    public class LeapXRHandProvider : XRHandSubsystemProvider
    {
        private bool trackingProviderAvailableLastFrame = false;

        private LeapProvider trackingProvider;
        public LeapProvider TrackingProvider
        {
            get
            { 
                if (trackingProvider == null)
                {
                    trackingProvider = Hands.Provider;
                }

                return trackingProvider;
            }
            set
            {
                trackingProvider = value; 
            }
        }

        public override void Start()
        {
        }

        public override void Stop()
        {
        }

        public override void Destroy()
        {
        }

        public override void GetHandLayout(NativeArray<bool> handJointsInLayout)
        {
            handJointsInLayout[XRHandJointID.Palm.ToIndex()] = true;
            handJointsInLayout[XRHandJointID.Wrist.ToIndex()] = true;

            handJointsInLayout[XRHandJointID.ThumbMetacarpal.ToIndex()] = true;
            handJointsInLayout[XRHandJointID.ThumbProximal.ToIndex()] = true;
            handJointsInLayout[XRHandJointID.ThumbDistal.ToIndex()] = true;
            handJointsInLayout[XRHandJointID.ThumbTip.ToIndex()] = true;

            handJointsInLayout[XRHandJointID.IndexMetacarpal.ToIndex()] = true;
            handJointsInLayout[XRHandJointID.IndexProximal.ToIndex()] = true;
            handJointsInLayout[XRHandJointID.IndexIntermediate.ToIndex()] = true;
            handJointsInLayout[XRHandJointID.IndexDistal.ToIndex()] = true;
            handJointsInLayout[XRHandJointID.IndexTip.ToIndex()] = true;

            handJointsInLayout[XRHandJointID.MiddleMetacarpal.ToIndex()] = true;
            handJointsInLayout[XRHandJointID.MiddleProximal.ToIndex()] = true;
            handJointsInLayout[XRHandJointID.MiddleIntermediate.ToIndex()] = true;
            handJointsInLayout[XRHandJointID.MiddleDistal.ToIndex()] = true;
            handJointsInLayout[XRHandJointID.MiddleTip.ToIndex()] = true;

            handJointsInLayout[XRHandJointID.RingMetacarpal.ToIndex()] = true;
            handJointsInLayout[XRHandJointID.RingProximal.ToIndex()] = true;
            handJointsInLayout[XRHandJointID.RingIntermediate.ToIndex()] = true;
            handJointsInLayout[XRHandJointID.RingDistal.ToIndex()] = true;
            handJointsInLayout[XRHandJointID.RingTip.ToIndex()] = true;

            handJointsInLayout[XRHandJointID.LittleMetacarpal.ToIndex()] = true;
            handJointsInLayout[XRHandJointID.LittleProximal.ToIndex()] = true;
            handJointsInLayout[XRHandJointID.LittleIntermediate.ToIndex()] = true;
            handJointsInLayout[XRHandJointID.LittleDistal.ToIndex()] = true;
            handJointsInLayout[XRHandJointID.LittleTip.ToIndex()] = true;
        }

        public override XRHandSubsystem.UpdateSuccessFlags TryUpdateHands(
            XRHandSubsystem.UpdateType updateType, 
            ref Pose leftHandRootPose, 
            NativeArray<XRHandJoint> leftHandJoints, 
            ref Pose rightHandRootPose, 
            NativeArray<XRHandJoint> rightHandJoints)
        {
            if(TrackingProvider == null)
            {
                if(trackingProviderAvailableLastFrame)
                {
                    Debug.LogWarning("Leap XRHands Tracking Provider has been lost. Without a LeapProvider in your scene, you will not receive Leap XRHands.");
                    trackingProviderAvailableLastFrame = false;
                }

                return XRHandSubsystem.UpdateSuccessFlags.None;
            }

            Frame currentFrame = TrackingProvider.CurrentFrame;

            XRHandSubsystem.UpdateSuccessFlags updateSuccessFlags = XRHandSubsystem.UpdateSuccessFlags.None;

            // keep a safe copy of the arrays
            XRHandJoint[] leftHandJointsCopy = new XRHandJoint[leftHandJoints.Length];
            leftHandJoints.CopyTo(leftHandJointsCopy);
            XRHandJoint[] rightHandJointsCopy = new XRHandJoint[rightHandJoints.Length];
            rightHandJoints.CopyTo(rightHandJointsCopy);

            if (PopulateXRHandFromLeap(currentFrame.GetHand(Chirality.Left), ref leftHandRootPose, ref leftHandJointsCopy))
            {
                updateSuccessFlags |= XRHandSubsystem.UpdateSuccessFlags.LeftHandRootPose;
                updateSuccessFlags |= XRHandSubsystem.UpdateSuccessFlags.LeftHandJoints;
            }

            if (PopulateXRHandFromLeap(currentFrame.GetHand(Chirality.Right), ref rightHandRootPose, ref rightHandJointsCopy))
            {
                updateSuccessFlags |= XRHandSubsystem.UpdateSuccessFlags.RightHandRootPose;
                updateSuccessFlags |= XRHandSubsystem.UpdateSuccessFlags.RightHandJoints;
            }

            trackingProviderAvailableLastFrame = true;

            // Apply the modified data back to the source
            leftHandJoints.CopyFrom(leftHandJointsCopy);
            rightHandJoints.CopyFrom(rightHandJointsCopy);

            return updateSuccessFlags;
        }

        bool PopulateXRHandFromLeap(Hand leapHand, ref Pose rootPose, ref XRHandJoint[] handJoints)
        {
            if (leapHand == null)
            {
                return false;
            }

            Pose palmPose = CalculatePalmPose(leapHand);
            Pose wristPose = CalculateWristPose(leapHand);

            rootPose = wristPose;
            Handedness handedness = (Handedness)((int)leapHand.GetChirality() + 1); // +1 as unity has "invalid" handedness while we do not

            handJoints[0] = XRHandProviderUtility.CreateJoint(handedness,
                        XRHandJointTrackingState.Pose,
                        XRHandJointIDUtility.FromIndex(1),
                        wristPose);
            handJoints[1] = XRHandProviderUtility.CreateJoint(handedness,
                        XRHandJointTrackingState.Pose,
                        XRHandJointIDUtility.FromIndex(1),
                        palmPose);

            int jointIndex = 2;

            foreach (var finger in leapHand.Fingers)
            {
                if (finger.Type == Finger.FingerType.TYPE_THUMB)
                {
                    for (int i = 1; i < 4; i++)
                    {
                        var bone = finger.bones[i];

                        handJoints[jointIndex] = XRHandProviderUtility.CreateJoint(handedness,
                            XRHandJointTrackingState.Pose,
                            XRHandJointIDUtility.FromIndex(jointIndex),
                            new Pose(bone.PrevJoint, bone.Rotation));

                        jointIndex++;
                    }

                    var distal = finger.Bone(Bone.BoneType.TYPE_DISTAL);
                    handJoints[jointIndex] = XRHandProviderUtility.CreateJoint(handedness,
                        XRHandJointTrackingState.Pose,
                        XRHandJointIDUtility.FromIndex(jointIndex),
                        new Pose(distal.NextJoint, distal.Rotation));
                    jointIndex++;

                }
                else
                {
                    foreach (var bone in finger.bones)
                    {
                        handJoints[jointIndex] = XRHandProviderUtility.CreateJoint(handedness,
                            XRHandJointTrackingState.Pose,
                            XRHandJointIDUtility.FromIndex(jointIndex),
                            new Pose(bone.PrevJoint, bone.Rotation));
                        jointIndex++;
                    }

                    var distal = finger.Bone(Bone.BoneType.TYPE_DISTAL);
                    handJoints[jointIndex] = XRHandProviderUtility.CreateJoint(handedness,
                        XRHandJointTrackingState.Pose,
                        XRHandJointIDUtility.FromIndex(jointIndex),
                        new Pose(distal.NextJoint, distal.Rotation));
                    jointIndex++;
                }
            }

            return true;
        }

        Pose CalculatePalmPose(Hand leapHand)
        {
            Pose palmPose = new Pose();
            palmPose.position = Vector3.Lerp(leapHand.GetMiddle().Bone(Bone.BoneType.TYPE_METACARPAL).PrevJoint,
                                                leapHand.GetMiddle().Bone(Bone.BoneType.TYPE_PROXIMAL).PrevJoint, 0.5f);

            palmPose.rotation = leapHand.GetMiddle().Bone(Bone.BoneType.TYPE_METACARPAL).Rotation;

            return palmPose;
        }

        Pose CalculateWristPose(Hand leapHand)
        {
            Pose wristPose = new Pose();

            wristPose.position = leapHand.WristPosition;

            Vector3 wristUp = leapHand.GetMiddle().Bone(Bone.BoneType.TYPE_METACARPAL).Rotation * Vector3.up;
            Vector3 wristForward = leapHand.GetMiddle().Bone(Bone.BoneType.TYPE_METACARPAL).PrevJoint - leapHand.WristPosition;

            wristPose.rotation = Quaternion.LookRotation(wristForward, wristUp);

            return wristPose;
        }

        static internal string id { get; private set; }
        static LeapXRHandProvider() => id = "UL XR Hands";

        //This method registers the subsystem descriptor with the SubsystemManager
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void RegisterDescriptor()
        {
            UltraleapSettings ultraleapSettings = UltraleapSettings.Instance;

            if (ultraleapSettings == null || ultraleapSettings.leapSubsystemEnabled == false)
            {
                return;
            }

            var handsSubsystemCinfo = new XRHandSubsystemDescriptor.Cinfo
            {
                id = id,
                providerType = typeof(LeapXRHandProvider),
                subsystemTypeOverride = typeof(LeapHandSubsystem)
            };
            XRHandSubsystemDescriptor.Register(handsSubsystemCinfo);
        }

        public static void SetSubsystemTrackingProvider(LeapProvider leapProvider)
        {
            List<LeapHandSubsystem> subsystems = new List<LeapHandSubsystem>();
            SubsystemManager.GetSubsystems(subsystems);

            if(subsystems.Count > 0)
            {
                LeapXRHandProvider subsystemProvider = subsystems[0].GetProvider() as LeapXRHandProvider;
                subsystemProvider.TrackingProvider = leapProvider;
            }
            else
            {
                Debug.LogWarning("No LeapHandSubsystem found, LeapProvider could not be set.");
            }
        }
    }

    // This class defines a hand subsystem
    class LeapHandSubsystem : XRHandSubsystem
    {
    }
}