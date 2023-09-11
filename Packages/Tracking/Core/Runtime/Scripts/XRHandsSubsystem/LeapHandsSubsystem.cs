/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2023.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using System.Collections.Generic;

using Unity.Collections;

using UnityEngine;
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
            if (TrackingProvider == null)
            {
                if (trackingProviderAvailableLastFrame)
                {
                    Debug.LogWarning("Leap XRHands Tracking Provider has been lost. Without a LeapProvider in your scene, you will not receive Leap XRHands.");
                    trackingProviderAvailableLastFrame = false;
                }

                return XRHandSubsystem.UpdateSuccessFlags.None;
            }

            Frame currentFrame = GetLatestTrackingFrameCopy();

            XRHandSubsystem.UpdateSuccessFlags updateSuccessFlags = XRHandSubsystem.UpdateSuccessFlags.None;

            if (PopulateXRHandFromLeap(currentFrame.GetHand(Chirality.Left), ref leftHandRootPose, ref leftHandJoints, updateType))
            {
                updateSuccessFlags |= XRHandSubsystem.UpdateSuccessFlags.LeftHandRootPose;
                updateSuccessFlags |= XRHandSubsystem.UpdateSuccessFlags.LeftHandJoints;
            }

            if (PopulateXRHandFromLeap(currentFrame.GetHand(Chirality.Right), ref rightHandRootPose, ref rightHandJoints, updateType))
            {
                updateSuccessFlags |= XRHandSubsystem.UpdateSuccessFlags.RightHandRootPose;
                updateSuccessFlags |= XRHandSubsystem.UpdateSuccessFlags.RightHandJoints;
            }

            trackingProviderAvailableLastFrame = true;

            return updateSuccessFlags;
        }

        /// <summary>
        /// Get the latest tracking frame from the TrackingProvider.
        /// This will be in local space to the camera's parent if it is available
        /// 
        /// Also ensures the Frame is a copy if it is transformed to avoid transforming the original Frame
        /// </summary>
        Frame GetLatestTrackingFrameCopy()
        {
            Frame currentFrame = TrackingProvider.CurrentFrame;

            if (currentFrame != null &&
                    Camera.main != null &&
                    Camera.main.transform.parent != null)
            {
                Vector3 camPos = Camera.main.transform.parent.position;
                Quaternion camRot = Camera.main.transform.parent.rotation;

                // Move first to ensure correct order of transformation
                currentFrame = currentFrame.TransformedCopy(-camPos, Quaternion.identity);
                currentFrame.Transform(Vector3.zero, Quaternion.Inverse(camRot));
            }

            return currentFrame;
        }

        bool PopulateXRHandFromLeap(Hand leapHand, ref Pose rootPose, ref NativeArray<XRHandJoint> handJoints, XRHandSubsystem.UpdateType updateType)
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
                        XRHandJointIDUtility.FromIndex(0),
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
            Vector3 wristUp = leapHand.GetMiddle().Bone(Bone.BoneType.TYPE_METACARPAL).Rotation * Vector3.up;
            Vector3 wristForward = leapHand.GetMiddle().Bone(Bone.BoneType.TYPE_METACARPAL).PrevJoint - leapHand.WristPosition;

            Pose wristPose = new Pose();
            wristPose.position = leapHand.WristPosition;
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

            if (ultraleapSettings == null || ultraleapSettings.leapSubsystemEnabled == false || DoesDescriptorExist())
            {
                return;
            }

            var handsSubsystemCinfo = new XRHandSubsystemDescriptor.Cinfo
            {
                id = "UL XR Hands",
                providerType = typeof(LeapXRHandProvider),
                subsystemTypeOverride = typeof(LeapHandsSubsystem)
            };

            XRHandSubsystemDescriptor.Register(handsSubsystemCinfo);
        }

        static bool DoesDescriptorExist()
        {
            List<XRHandSubsystemDescriptor> descriptors = new List<XRHandSubsystemDescriptor>();
            SubsystemManager.GetSubsystemDescriptors(descriptors);
            foreach (var descriptor in descriptors)
            {
                if (descriptor.id == "UL XR Hands")
                {
                    return true;
                }
            }

            return false;
        }
    }
}

public class LeapHandsSubsystem : XRHandSubsystem
{

}