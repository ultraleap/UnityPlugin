using Unity.Collections;
using UnityEngine;
using UnityEngine.XR.Hands;
using UnityEngine.SubsystemsImplementation;
using UnityEngine.XR.Hands.ProviderImplementation;

using Leap;
using Leap.Unity;
using System;
using static UnityEngine.XR.Hands.XRHandSubsystem;
using UnityEngine.XR.Hands.OpenXR;

namespace Leap.Unity
{
    class LeapXRHandProvider : XRHandSubsystemProvider
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

        static internal string id { get; private set; }

        static LeapXRHandProvider() => id = "UL XR Hands";

        //public Action<XRHandSubsystemProvider, UpdateSuccessFlags, UpdateType> updatedHands;
        //public UpdateSuccessFlags updateSuccessFlags { get; protected set; }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Register()
        {

            ///This needs to be implemented here when we have the subsystem in place
            //var handsSubsystemCinfo = new XRHandSubsystemDescriptor.Cinfo
            //{
            //    id = id,
            //    providerType = typeof(OpenXRHandProvider)
            //};
            //XRHandSubsystemDescriptor.Register(handsSubsystemCinfo);
            
        }

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
                        Debug.Log(finger.Type + " hit. Index: " + jointIndex);

                        jointIndex++;

                    }

                    var distal = finger.Bone(Bone.BoneType.TYPE_DISTAL);
                    handJoints[jointIndex] = XRHandProviderUtility.CreateJoint(handedness,
                        XRHandJointTrackingState.Pose,
                        XRHandJointIDUtility.FromIndex(jointIndex),
                        new Pose(distal.NextJoint, distal.Rotation));
                    Debug.Log(finger.Type + " tip. Index: " + jointIndex);
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
                        Debug.Log(finger.Type + " hit. Index: " + jointIndex);
                        jointIndex++;
                    }

                    var distal = finger.Bone(Bone.BoneType.TYPE_DISTAL);
                    handJoints[jointIndex] = XRHandProviderUtility.CreateJoint(handedness,
                        XRHandJointTrackingState.Pose,
                        XRHandJointIDUtility.FromIndex(jointIndex),
                        new Pose(distal.NextJoint, distal.Rotation));
                    Debug.Log(finger.Type + " tip. Index: " + jointIndex);
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
                providerType = typeof(LeapXRHandProvider),
                subsystemTypeOverride = typeof(LeapHandSubsystem)
            };
            XRHandSubsystemDescriptor.Register(handsSubsystemCinfo);
        }

        LeapProvider provider;


        protected override void OnCreate()
        {
            base.OnCreate();
            provider = GameObject.FindObjectOfType<XRLeapProviderManager>();
            Debug.Log("started");

        }


        public override UpdateSuccessFlags TryUpdateHands(XRHandSubsystem.UpdateType updateType)
        {
            base.TryUpdateHands(updateType);

            Pose leftHandRootPose = new Pose();
            NativeArray<XRHandJoint> leftHandJoints = new NativeArray<XRHandJoint>(XRHandJointID.EndMarker.ToIndex(), Allocator.Persistent);
            Pose rightHandRootPose = new Pose();
            NativeArray< XRHandJoint > rightHandJoints = new NativeArray<XRHandJoint>(XRHandJointID.EndMarker.ToIndex(), Allocator.Persistent);


            Debug.Log("Subsystem updateHand");
            Frame currentFrame = provider.CurrentFrame;

            UpdateSuccessFlags updateSuccessFlags = UpdateSuccessFlags.None;

            if (PopulateXRHandFromLeap(currentFrame.GetHand(Chirality.Left), ref leftHandRootPose, ref leftHandJoints))
            {
                updateSuccessFlags |= UpdateSuccessFlags.LeftHandRootPose;
                updateSuccessFlags |= UpdateSuccessFlags.LeftHandJoints;
            }

            if (PopulateXRHandFromLeap(currentFrame.GetHand(Chirality.Right), ref rightHandRootPose, ref rightHandJoints))
            {
                updateSuccessFlags |= UpdateSuccessFlags.RightHandRootPose;
                updateSuccessFlags |= UpdateSuccessFlags.RightHandJoints;
            }

            if (updatedHands != null)
                updatedHands.Invoke(this, updateSuccessFlags, updateType);



            return updateSuccessFlags;
        }


        bool PopulateXRHandFromLeap(Hand leapHand, ref Pose rootPose, ref NativeArray<XRHandJoint> handJoints)
        {
            if (leapHand == null)
            {
                return false;
            }

            return true;
        }
    }
}