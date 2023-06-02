using Unity.Collections;
using UnityEngine;
using UnityEngine.XR.Hands;
using UnityEngine.XR.Hands.ProviderImplementation;

namespace Leap.Unity
{
    class LeapXRHandProvider : XRHandSubsystemProvider
    {
        public LeapProvider provider;

        public override void Destroy()
        {

        }

        public override void GetHandLayout(NativeArray<bool> handJointsInLayout)
        {
        }

        public override void Start()
        {
            
            provider = Hands.Provider;
        }

        public override void Stop()
        {
        }

        static internal string id { get; private set; }
        static LeapXRHandProvider() => id = "UL XR Hands";

        // This method registers the subsystem descriptor with the SubsystemManager
        //[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        //static void RegisterDescriptor()
        //{
        //    var handsSubsystemCinfo = new XRHandSubsystemDescriptor.Cinfo
        //    {
        //        id = id,
        //        providerType = typeof(LeapXRHandProvider)
        //        //subsystemTypeOverride = typeof(LeapHandSubsystem)
        //    };
        //    XRHandSubsystemDescriptor.Register(handsSubsystemCinfo);
        //}

        

        public override XRHandSubsystem.UpdateSuccessFlags TryUpdateHands(
            XRHandSubsystem.UpdateType updateType, 
            ref Pose leftHandRootPose, 
            NativeArray<XRHandJoint> leftHandJoints, 
            ref Pose rightHandRootPose, 
            NativeArray<XRHandJoint> rightHandJoints)
        {

            Frame currentFrame = provider.CurrentFrame;

            XRHandSubsystem.UpdateSuccessFlags updateSuccessFlags = XRHandSubsystem.UpdateSuccessFlags.None;

            if (PopulateXRHandFromLeap(currentFrame.GetHand(Chirality.Left), ref leftHandRootPose, ref leftHandJoints))
            {
                updateSuccessFlags |= XRHandSubsystem.UpdateSuccessFlags.LeftHandRootPose;
                updateSuccessFlags |= XRHandSubsystem.UpdateSuccessFlags.LeftHandJoints;
            }

            if (PopulateXRHandFromLeap(currentFrame.GetHand(Chirality.Right), ref rightHandRootPose, ref rightHandJoints))
            {
                updateSuccessFlags |= XRHandSubsystem.UpdateSuccessFlags.RightHandRootPose;
                updateSuccessFlags |= XRHandSubsystem.UpdateSuccessFlags.RightHandJoints;
            }

            return updateSuccessFlags;
        }

        bool PopulateXRHandFromLeap(Hand leapHand, ref Pose rootPose, ref NativeArray<XRHandJoint> handJoints)
        {
            if (leapHand == null)
            {
                return false;
            }

            rootPose = new Pose(leapHand.WristPosition, leapHand.Rotation);
            Handedness handedness = (Handedness)((int)leapHand.GetChirality() + 1); // +1 as unity has "invalid" handedness while we do not 
            handJoints[0] = XRHandProviderUtility.CreateJoint(handedness,
                        XRHandJointTrackingState.Pose,
                        XRHandJointIDUtility.FromIndex(1),
                        new Pose(leapHand.WristPosition, leapHand.Rotation));
            handJoints[1] = XRHandProviderUtility.CreateJoint(handedness,
                        XRHandJointTrackingState.Pose,
                        XRHandJointIDUtility.FromIndex(1),
                        new Pose(leapHand.PalmPosition, leapHand.Rotation));
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
    }

    // This class defines a hand subsystem
    class LeapHandSubsystem : XRHandSubsystem
    {
        //This method registers the subsystem descriptor with the SubsystemManager
       [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void RegisterDescriptor()
        {
            var handsSubsystemCinfo = new XRHandSubsystemDescriptor.Cinfo
            {
                id = "UL XR Hands",
                providerType = typeof(LeapXRHandProvider),
                subsystemTypeOverride = typeof(LeapHandSubsystem)
            };
            XRHandSubsystemDescriptor.Register(handsSubsystemCinfo);
        }


    }
}