using Unity.Collections;
using UnityEngine;
using UnityEngine.XR.Hands;
using UnityEngine.XR.Hands.ProviderImplementation;

using Leap;
using Leap.Unity;

namespace Leap.Unity
{
    class LeapHandProvider : XRHandSubsystemProvider
    {
        LeapProvider provider;

        public override void Destroy()
        {
        }

        public override void GetHandLayout(NativeArray<bool> handJointsInLayout)
        {
        }

        public override void Start()
        {
            provider = GameObject.FindObjectOfType<XRLeapProviderManager>();
            Debug.Log("started");
        }

        public override void Stop()
        {
        }

        public override XRHandSubsystem.UpdateSuccessFlags TryUpdateHands(XRHandSubsystem.UpdateType updateType, ref Pose leftHandRootPose, NativeArray<XRHandJoint> leftHandJoints, ref Pose rightHandRootPose, NativeArray<XRHandJoint> rightHandJoints)
        {
            Debug.Log("updateHand");
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

            rootPose = new Pose(leapHand.PalmPosition, leapHand.Rotation);

            int jointIndex = 0;

            foreach (var finger in leapHand.Fingers)
            {
                foreach (var bone in finger.bones)
                {
                    handJoints[jointIndex] = XRHandProviderUtility.CreateJoint(
                    XRHandJointTrackingState.Pose,
                    XRHandJointIDUtility.FromIndex(jointIndex),
                    new Pose(bone.PrevJoint, bone.Rotation));

                    jointIndex++;
                }
            }

            return true;
        }
    }

    // This class defines a hand subsystem
    class LeapHandSubsystem : XRHandSubsystem
    {
        // This method registers the subsystem descriptor with the SubsystemManager
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void RegisterDescriptor()
        {
            var handsSubsystemCinfo = new XRHandSubsystemDescriptor.Cinfo
            {
                id = "UL XR Hands",
                providerType = typeof(LeapHandProvider),
                subsystemTypeOverride = typeof(LeapHandSubsystem)
            };
            XRHandSubsystemDescriptor.Register(handsSubsystemCinfo);
        }
    }
}