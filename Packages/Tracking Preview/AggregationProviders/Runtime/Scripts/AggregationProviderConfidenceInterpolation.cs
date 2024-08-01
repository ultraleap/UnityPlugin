/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2024.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using Leap.Encoding;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Leap
{
    /// <summary>
    /// possible structure to implement our own aggregation code,
    /// calculates hand and joint confidences and interpolates linearly between hands based on their confidences,
    /// it uses hand confidences for the overall position and orientation of the hand (palmPos and PalmRot),
    /// and joint confidences for the per joint positions relative to the hand Pose
    /// </summary>
    public class AggregationProviderConfidenceInterpolation : LeapAggregatedProviderBase
    {
        [Tooltip("If true, the overall hand confidence is affected by the duration a new hand has been visible for. When a new hand is seen for the first time, its confidence is 0. After a hand has been visible for a second, its confidence is determined by the below palm factors and palm confidences")]
        public bool ignoreRecentNewHands = true;

        // factors that get multiplied to the corresponding confidence values to get an overall weighted confidence value
        [Tooltip("How much should the Palm position relative to the tracking camera influence the overall hand confidence? A confidence value is determined by whether the hand is within the optimal FOV of the tracking camera")]
        [Range(0f, 1f)]
        public float palmPosFactor = 0;
        [Tooltip("How much should the Palm orientation relative to the tracking camera influence the overall hand confidence? A confidence value is determined by looking at the angle between the palm normal and the direction from hand to camera.")]
        [Range(0f, 1f)]
        public float palmRotFactor = 0;
        [Tooltip("How much should the Palm velocity relative to the tracking camera influence the overall hand confidence?")]
        [Range(0f, 1f)]
        public float palmVelocityFactor = 0;

        [Tooltip("How much should the joint rotation relative to the tracking camera influence the overall hand confidence? A confidence value is determined for a joint by looking at the angle between the joint normal and the direction from hand to camera.")]
        [Range(0f, 1f)]
        public float jointRotFactor = 0;
        [Tooltip("How much should the joint rotation relative to the palm normal influence the overall hand confidence?")]
        [Range(0f, 1f)]
        public float jointRotToPalmFactor = 0;
        [Tooltip("How much should joint occlusion influence the overall hand confidence?")]
        [Range(0f, 1f)]
        public float jointOcclusionFactor = 0;


        public bool debugJointOrigins = false;
        [Tooltip("if the debug hand is not null, its joint colors are given by interpolating between the debugColors based on which hand the aggregated joint data came from (in order of the providers)")]
        public CapsuleHand debugHandLeft;
        [Tooltip("if the debug hand is not null, its joint colors are given by interpolating between the debugColors based on which hand the aggregated joint data came from (in order of the providers)")]
        public CapsuleHand debugHandRight;

        [Tooltip("The debug colors should have the same order and length as the provider list")]
        public Color[] debugColors = new Color[] { Color.red, Color.green };

        Dictionary<LeapProvider, HandPositionHistory> lastLeftHandPositions = new Dictionary<LeapProvider, HandPositionHistory>();
        Dictionary<LeapProvider, HandPositionHistory> lastRightHandPositions = new Dictionary<LeapProvider, HandPositionHistory>();

        Dictionary<LeapProvider, float> leftHandFirstVisible = new Dictionary<LeapProvider, float>();
        Dictionary<LeapProvider, float> rightHandFirstVisible = new Dictionary<LeapProvider, float>();

        List<JointOcclusion> jointOcclusions;

        Vector3[] mergedJointPositions = new Vector3[VectorHand.NUM_JOINT_POSITIONS];

        private float[][] jointConfidences;
        private float[][] confidences_jointRot;
        private float[][] confidences_jointPalmRot;
        private float[][] confidences_jointOcclusion;

        Dictionary<LeapProvider, JointConfidenceHistory> jointConfidenceHistoriesLeft = new Dictionary<LeapProvider, JointConfidenceHistory>();
        Dictionary<LeapProvider, JointConfidenceHistory> jointConfidenceHistoriesRight = new Dictionary<LeapProvider, JointConfidenceHistory>();

        Dictionary<LeapProvider, HandConfidenceHistory> handConfidenceHistoriesLeft = new Dictionary<LeapProvider, HandConfidenceHistory>();
        Dictionary<LeapProvider, HandConfidenceHistory> handConfidenceHistoriesRight = new Dictionary<LeapProvider, HandConfidenceHistory>();

        protected override Frame MergeFrames(Frame[] frames)
        {
            if (jointConfidences == null || confidences_jointRot == null || confidences_jointPalmRot == null || confidences_jointOcclusion == null)
            {
                jointConfidences = new float[providers.Length * 2][];
                confidences_jointRot = new float[providers.Length * 2][];
                confidences_jointPalmRot = new float[providers.Length * 2][];
                confidences_jointOcclusion = new float[providers.Length * 2][];
            }


            List<Hand> leftHands = new List<Hand>();
            List<Hand> rightHands = new List<Hand>();

            List<float> leftHandConfidences = new List<float>();
            List<float> rightHandConfidences = new List<float>();

            List<float[]> leftJointConfidences = new List<float[]>();
            List<float[]> rightJointConfidences = new List<float[]>();

            if (jointOcclusionFactor != 0)
            {
                SetupJointOcclusion();
            }

            // make lists of all left and right hands found in each frame and also make a list of their confidences
            for (int frame_idx = 0; frame_idx < frames.Length; frame_idx++)
            {
                Frame frame = frames[frame_idx];
                AddFrameToTimeVisibleDicts(frames, frame_idx);

                foreach (Hand hand in frame.Hands)
                {
                    if (hand.IsLeft)
                    {
                        leftHands.Add(hand);

                        float handConfidence = CalculateHandConfidence(frame_idx, hand);
                        float[] jointConfidences = CalculateJointConfidence(frame_idx, hand);

                        leftHandConfidences.Add(handConfidence);
                        leftJointConfidences.Add(jointConfidences);
                    }

                    else
                    {
                        rightHands.Add(hand);

                        float handConfidence = CalculateHandConfidence(frame_idx, hand);
                        float[] jointConfidences = CalculateJointConfidence(frame_idx, hand);

                        rightHandConfidences.Add(handConfidence);
                        rightJointConfidences.Add(jointConfidences);
                    }
                }
            }

            // normalize hand confidences:
            float sum = leftHandConfidences.Sum();
            if (sum != 0)
            {
                for (int hands_idx = 0; hands_idx < leftHandConfidences.Count; hands_idx++)
                {
                    leftHandConfidences[hands_idx] /= sum;
                }
            }
            else
            {
                for (int hands_idx = 0; hands_idx < leftHandConfidences.Count; hands_idx++)
                {
                    leftHandConfidences[hands_idx] = 1f / leftHandConfidences.Count;
                }
            }
            sum = rightHandConfidences.Sum();
            if (sum != 0)
            {
                for (int hands_idx = 0; hands_idx < rightHandConfidences.Count; hands_idx++)
                {
                    rightHandConfidences[hands_idx] /= sum;
                }
            }
            else
            {
                for (int hands_idx = 0; hands_idx < rightHandConfidences.Count; hands_idx++)
                {
                    rightHandConfidences[hands_idx] = 1f / rightHandConfidences.Count;
                }
            }

            // normalize joint confidences:
            for (int joint_idx = 0; joint_idx < VectorHand.NUM_JOINT_POSITIONS; joint_idx++)
            {
                sum = leftJointConfidences.Sum(x => x[joint_idx]);
                if (sum != 0)
                {
                    for (int hands_idx = 0; hands_idx < leftJointConfidences.Count; hands_idx++)
                    {
                        leftJointConfidences[hands_idx][joint_idx] /= sum;
                    }
                }
                else
                {
                    for (int hands_idx = 0; hands_idx < leftJointConfidences.Count; hands_idx++)
                    {
                        leftJointConfidences[hands_idx][joint_idx] = 1f / leftJointConfidences.Count;
                    }
                }

                sum = rightJointConfidences.Sum(x => x[joint_idx]);
                if (sum != 0)
                {
                    for (int hands_idx = 0; hands_idx < rightJointConfidences.Count; hands_idx++)
                    {
                        rightJointConfidences[hands_idx][joint_idx] /= sum;
                    }
                }
                else
                {
                    for (int hands_idx = 0; hands_idx < rightJointConfidences.Count; hands_idx++)
                    {
                        rightJointConfidences[hands_idx][joint_idx] = 1f / rightJointConfidences.Count;
                    }
                }
            }

            // combine hands using their confidences
            List<Hand> mergedHands = new List<Hand>();

            if (leftHands.Count > 0)
            {
                mergedHands.Add(MergeHands(leftHands, leftHandConfidences, leftJointConfidences));
            }

            if (rightHands.Count > 0)
            {
                mergedHands.Add(MergeHands(rightHands, rightHandConfidences, rightJointConfidences));
            }

            // create new frame and add merged hands to it
            Frame mergedFrame = new Frame();
            mergedFrame.Hands = mergedHands;

            return mergedFrame;
        }

        /// <summary>
        /// Merge hands based on hand confidences and joint confidences
        /// </summary>
        public Hand MergeHands(List<Hand> hands, List<float> handConfidences, List<float[]> jointConfidences)
        {
            bool isLeft = hands[0].IsLeft;
            Vector3 mergedPalmPos = hands[0].PalmPosition * handConfidences[0];
            Quaternion mergedPalmRot = hands[0].Rotation;

            Vector3 mergedArmPos = hands[0].Arm.Center * handConfidences[0];
            Vector3 mergedArmElbow = hands[0].Arm.ElbowPosition * handConfidences[0];
            Quaternion mergedArmRot = hands[0].Arm.Rotation;

            Vector3 mergedWristPos = hands[0].WristPosition * handConfidences[0];

            for (int hands_idx = 1; hands_idx < hands.Count; hands_idx++)
            {
                // position
                mergedPalmPos += hands[hands_idx].PalmPosition * handConfidences[hands_idx];
                mergedArmPos += hands[hands_idx].Arm.Center * handConfidences[hands_idx];
                mergedArmElbow += hands[hands_idx].Arm.ElbowPosition * handConfidences[hands_idx];
                mergedWristPos += hands[hands_idx].Arm.WristPosition * handConfidences[hands_idx];

                // rotation
                float lerpValue = handConfidences.Take(hands_idx).Sum() / handConfidences.Take(hands_idx + 1).Sum();
                mergedPalmRot = Quaternion.Lerp(hands[hands_idx].Rotation, mergedPalmRot, lerpValue);
                mergedArmRot = Quaternion.Lerp(hands[hands_idx].Arm.Rotation, mergedArmRot, lerpValue);
            }

            // joints
            Leap.Utils.Fill(mergedJointPositions, Vector3.zero);
            List<VectorHand> vectorHands = new List<VectorHand>();
            foreach (Hand hand in hands)
            {
                vectorHands.Add(new VectorHand(hand));
            }

            for (int hands_idx = 0; hands_idx < hands.Count; hands_idx++)
            {
                for (int joint_idx = 0; joint_idx < VectorHand.NUM_JOINT_POSITIONS; joint_idx++)
                {
                    mergedJointPositions[joint_idx] += vectorHands[hands_idx].jointPositions[joint_idx] * jointConfidences[hands_idx][joint_idx];
                }
            }

            // combine everything to a hand
            Hand mergedHand = new Hand();
            new VectorHand(isLeft, mergedPalmPos, mergedPalmRot, mergedJointPositions).Decode(mergedHand);

            mergedHand.WristPosition = mergedWristPos;

            mergedHand.Arm = new Arm(mergedArmElbow, mergedHand.WristPosition, mergedArmPos, mergedArmRot * Vector3.forward, hands[0].Arm.Length, hands[0].Arm.Width, mergedArmRot);

            // visualize the joint merge:
            if (debugJointOrigins && isLeft && debugHandLeft != null) VisualizeMergedJoints(debugHandLeft, jointConfidences);
            else if (debugJointOrigins && !isLeft && debugHandRight != null) VisualizeMergedJoints(debugHandRight, jointConfidences);

            return mergedHand;
        }

        /// <summary>
        /// combine different confidence functions to get an overall confidence for the given hand
        /// uses frame_idx to find the corresponding provider that saw this hand
        /// </summary>
        public float CalculateHandConfidence(int frame_idx, Hand hand)
        {
            float confidence = 0;

            Transform deviceOrigin = GetDeviceOrigin(providers[frame_idx]);

            confidence = palmPosFactor * Confidence_RelativeHandPos(providers[frame_idx], deviceOrigin, hand.PalmPosition);
            confidence += palmRotFactor * Confidence_RelativeHandRot(deviceOrigin, hand.PalmPosition, hand.PalmNormal);
            confidence += palmVelocityFactor * Confidence_RelativeHandVelocity(providers[frame_idx], deviceOrigin, hand.PalmPosition, hand.IsLeft);

            // if ignoreRecentNewHands is true, then
            // the confidence should be 0 when it is the first frame with the hand in it.
            if (ignoreRecentNewHands)
            {
                confidence = confidence * Confidence_TimeSinceHandFirstVisible(providers[frame_idx], hand.IsLeft);
            }

            // average out new hand confidence with that of the last few frames
            if (hand.IsLeft)
            {
                if (!handConfidenceHistoriesLeft.ContainsKey(providers[frame_idx]))
                {
                    handConfidenceHistoriesLeft.Add(providers[frame_idx], new HandConfidenceHistory());
                }
                handConfidenceHistoriesLeft[providers[frame_idx]].AddConfidence(confidence);
                confidence = handConfidenceHistoriesLeft[providers[frame_idx]].GetAveragedConfidence();
            }
            else
            {
                if (!handConfidenceHistoriesRight.ContainsKey(providers[frame_idx]))
                {
                    handConfidenceHistoriesRight.Add(providers[frame_idx], new HandConfidenceHistory());
                }
                handConfidenceHistoriesRight[providers[frame_idx]].AddConfidence(confidence);
                confidence = handConfidenceHistoriesRight[providers[frame_idx]].GetAveragedConfidence();
            }

            return confidence;
        }

        /// <summary>
        /// Combine different confidence functions to get an overall confidence for each joint in the given hand
        /// uses frame_idx to find the corresponding provider that saw this hand
        /// </summary>
        public float[] CalculateJointConfidence(int frame_idx, Hand hand)
        {
            // get index in confidence arrays
            int idx = frame_idx * 2 + (hand.IsLeft ? 0 : 1);

            if (jointConfidences[idx] == null || confidences_jointRot[idx] == null || confidences_jointPalmRot[idx] == null || confidences_jointOcclusion[idx] == null)
            {
                jointConfidences[idx] = new float[VectorHand.NUM_JOINT_POSITIONS];
                confidences_jointRot[idx] = new float[VectorHand.NUM_JOINT_POSITIONS];
                confidences_jointPalmRot[idx] = new float[VectorHand.NUM_JOINT_POSITIONS];
                confidences_jointOcclusion[idx] = new float[VectorHand.NUM_JOINT_POSITIONS];
            }

            Transform deviceOrigin = GetDeviceOrigin(providers[frame_idx]);

            if (jointRotFactor != 0)
            {
                confidences_jointRot[idx] = Confidence_RelativeJointRot(confidences_jointRot[idx], deviceOrigin, hand);
            }
            if (jointRotToPalmFactor != 0)
            {
                confidences_jointPalmRot[idx] = Confidence_relativeJointRotToPalmRot(confidences_jointPalmRot[idx], deviceOrigin, hand);
            }
            if (jointOcclusionFactor != 0)
            {
                confidences_jointOcclusion[idx] = jointOcclusions[frame_idx].Confidence_JointOcclusion(confidences_jointOcclusion[idx], deviceOrigin, hand);
            }

            for (int finger_idx = 0; finger_idx < 5; finger_idx++)
            {
                for (int bone_idx = 0; bone_idx < 5; bone_idx++)
                {
                    int key = finger_idx * 5 + bone_idx;
                    jointConfidences[idx][key] =
                                    jointRotFactor * confidences_jointRot[idx][key] +
                    jointRotToPalmFactor * confidences_jointPalmRot[idx][key] +
                   jointOcclusionFactor * confidences_jointOcclusion[idx][key];

                    if (bone_idx != 0)
                    {
                        // average with the confidence from the last joint on the same finger,
                        // so that outer joints jump around less. 
                        // eg. when a confidence is low on the knuckle of a finger, the finger tip confidence for the same finger
                        // should take that into account and be slightly lower too
                        jointConfidences[idx][key] += jointConfidences[idx][key - 1];
                        jointConfidences[idx][key] /= 2;
                    }
                }
            }

            // average out new joint confidence with that of the last few frames
            if (hand.IsLeft)
            {
                if (!jointConfidenceHistoriesLeft.ContainsKey(providers[frame_idx]))
                {
                    jointConfidenceHistoriesLeft.Add(providers[frame_idx], new JointConfidenceHistory());
                }
                jointConfidenceHistoriesLeft[providers[frame_idx]].AddConfidences(jointConfidences[idx]);
                jointConfidences[idx] = jointConfidenceHistoriesLeft[providers[frame_idx]].GetAveragedConfidences();
            }
            else
            {
                if (!jointConfidenceHistoriesRight.ContainsKey(providers[frame_idx]))
                {
                    jointConfidenceHistoriesRight.Add(providers[frame_idx], new JointConfidenceHistory());
                }
                jointConfidenceHistoriesRight[providers[frame_idx]].AddConfidences(jointConfidences[idx]);
                jointConfidences[idx] = jointConfidenceHistoriesRight[providers[frame_idx]].GetAveragedConfidences();
            }

            return jointConfidences[idx];
        }

        #region Hand Confidence Methods

        /// <summary>
        /// uses the hand pos relative to the device to calculate a confidence.
        /// using a 2d gauss with bigger spread when further away from the device
        /// and amplitude depending on the ideal depth of the specific device and 
        /// the distance from hand to device
        /// </summary>
        float Confidence_RelativeHandPos(LeapProvider provider, Transform deviceOrigin, Vector3 handPos)
        {
            Vector3 relativeHandPos = deviceOrigin.InverseTransformPoint(handPos);

            // 2d gauss

            // amplitude
            float a = 1 - relativeHandPos.y;
            // pos of center of peak
            float x0 = 0;
            float y0 = 0;
            // spread
            float sigmaX = relativeHandPos.y;
            float sigmaY = relativeHandPos.y;

            // input pos
            float x = relativeHandPos.x;
            float y = relativeHandPos.z;


            // if the frame is coming from a LeapServiceProvider, use different values depending on the type of device
            if (provider is LeapServiceProvider || provider.GetType().BaseType == typeof(LeapServiceProvider))
            {
                LeapServiceProvider serviceProvider = provider as LeapServiceProvider;
                Device.DeviceType deviceType = serviceProvider.CurrentDevice.Type;

                if (deviceType == Device.DeviceType.TYPE_RIGEL || deviceType == Device.DeviceType.TYPE_SIR170 || deviceType == Device.DeviceType.TYPE_3DI)
                {
                    // Depth: Between 10cm to 75cm preferred, up to 1m maximum
                    // Field Of View: 170 x 170 degrees typical (160 x 160 degrees minimum)
                    float currentDepth = relativeHandPos.y;

                    float requiredWidth = (currentDepth / 2) / Mathf.Sin(Mathf.Deg2Rad * 170 / 2);
                    sigmaX = 0.2f * requiredWidth;
                    sigmaY = 0.2f * requiredWidth;

                    // amplitude should be 1 within ideal depth and go 'smoothly' to zero on both sides of the ideal depth.
                    if (currentDepth > 0.1f && currentDepth < 0.75)
                    {
                        a = 1f;
                    }
                    else if (currentDepth < 0.1f)
                    {
                        a = 0.55f / (Mathf.PI / 2) * Mathf.Atan(100 * (currentDepth + 0.05f)) + 0.5f;
                    }
                    else if (currentDepth > 0.75f)
                    {
                        a = -0.55f / (Mathf.PI / 2) * Mathf.Atan(50 * (currentDepth - 0.875f)) + 0.5f;
                    }
                }
                else if (deviceType == Device.DeviceType.TYPE_PERIPHERAL)
                {
                    // Depth: Between 10cm to 60cm preferred, up to 80cm maximum
                    // Field Of View: 140 x 120 degrees typical
                    float currentDepth = relativeHandPos.y;
                    float requiredWidthX = (currentDepth / 2) / Mathf.Sin(Mathf.Deg2Rad * 120 / 2);
                    float requiredWidthY = (currentDepth / 2) / Mathf.Sin(Mathf.Deg2Rad * 140 / 2);
                    sigmaX = 0.2f * requiredWidthX;
                    sigmaY = 0.2f * requiredWidthY;

                    // amplitude should be 1 within ideal depth and go 'smoothly' to zero on both sides of the ideal depth.
                    if (currentDepth > 0.1f && currentDepth < 0.6f)
                    {
                        a = 1f;
                    }
                    else if (currentDepth < 0.1f)
                    {
                        a = 0.55f / (Mathf.PI / 2) * Mathf.Atan(100 * (currentDepth + 0.05f)) + 0.5f;
                    }
                    else if (currentDepth > 0.6f)
                    {
                        a = -0.55f / (Mathf.PI / 2) * Mathf.Atan(50 * (currentDepth - 0.7f)) + 0.5f;
                    }
                }
            }

            float confidence = a * Mathf.Exp(-(Mathf.Pow(x - x0, 2) / (2 * Mathf.Pow(sigmaX, 2)) + Mathf.Pow(y - y0, 2) / (2 * Mathf.Pow(sigmaY, 2))));

            if (confidence < 0) confidence = 0;

            return confidence;
        }

        /// <summary>
        /// uses the palm normal relative to the direction from hand to device to calculate a confidence
        /// </summary>
        float Confidence_RelativeHandRot(Transform deviceOrigin, Vector3 handPos, Vector3 palmNormal)
        {
            // angle between palm normal and the direction from hand pos to device origin
            float palmAngle = Vector3.Angle(palmNormal, deviceOrigin.position - handPos);

            // get confidence based on a cos where it should be 1 if the angle is 0 or 180 degrees,
            // and it should be 0 if it is 90 degrees
            float confidence = (Mathf.Cos(Mathf.Deg2Rad * 2 * palmAngle) + 1f) / 2;

            return confidence;
        }

        /// <summary>
        /// uses the hand velocity to calculate a confidence.
        /// returns a high confidence, if the velocity is low, and a low confidence otherwise.
        /// Returns 0, if the hand hasn't been consistently tracked for about the last 10 frames
        /// </summary>
        float Confidence_RelativeHandVelocity(LeapProvider provider, Transform deviceOrigin, Vector3 handPos, bool isLeft)
        {
            Vector3 oldPosition;
            float oldTime;

            bool positionsRecorded = isLeft ? lastLeftHandPositions[provider].GetOldestPosition(out oldPosition, out oldTime) : lastRightHandPositions[provider].GetOldestPosition(out oldPosition, out oldTime);

            // if we haven't recorded any positions yet, or the hand hasn't been present in the last 10 frames (oldest position is older than 10 * frame time), return 0
            if (!positionsRecorded || (Time.time - oldTime) > Time.deltaTime * 10)
            {
                return 0;
            }

            float velocity = Vector3.Distance(handPos, oldPosition) / (Time.time - oldTime);

            float confidence = 0;
            if (velocity < 2)
            {
                confidence = -0.5f * velocity + 1;
            }

            return confidence;
        }

        float Confidence_TimeSinceHandFirstVisible(LeapProvider provider, bool isLeft)
        {
            if ((isLeft ? leftHandFirstVisible[provider] : rightHandFirstVisible[provider]) == 0)
            {
                return 0;
            }

            float lengthVisible = Time.time - (isLeft ? leftHandFirstVisible[provider] : rightHandFirstVisible[provider]);

            float confidence = 1;
            if (lengthVisible < 1)
            {
                confidence = lengthVisible;
            }

            return confidence;
        }
        #endregion

        #region Joint Confidence Methods

        /// <summary>
        /// uses the normal vector of a joint / bone (outwards pointing one) and the direction from joint to device 
        /// to calculate per-joint confidence values
        /// </summary>
        float[] Confidence_RelativeJointRot(float[] confidences, Transform deviceOrigin, Hand hand)
        {
            if (confidences == null)
            {
                confidences = new float[VectorHand.NUM_JOINT_POSITIONS];
            }

            foreach (var finger in hand.fingers)
            {
                for (int bone_idx = 0; bone_idx < 4; bone_idx++)
                {
                    int key = (int)finger.Type * 4 + bone_idx;

                    Vector3 jointPos = finger.GetBone((Bone.BoneType)bone_idx).NextJoint;
                    Vector3 jointNormalVector = new Vector3();
                    if ((int)finger.Type == 0) jointNormalVector = finger.GetBone((Bone.BoneType)bone_idx).Rotation * Vector3.right;
                    else jointNormalVector = finger.GetBone((Bone.BoneType)bone_idx).Rotation * Vector3.up * -1;

                    float angle = Vector3.Angle(jointPos - deviceOrigin.position, jointNormalVector);


                    // get confidence based on a cos where it should be 1 if the angle is 0 or 180 degrees,
                    // and it should be 0 if it is 90 degrees
                    confidences[key] = (Mathf.Cos(Mathf.Deg2Rad * 2 * angle) + 1f) / 2;
                }
            }

            return confidences;
        }

        /// <summary>
        /// uses the normal vector of a joint / bone (outwards pointing one) and the palm normal vector
        /// to calculate per-joint confidence values
        /// </summary>
        float[] Confidence_relativeJointRotToPalmRot(float[] confidences, Transform deviceOrigin, Hand hand)
        {
            if (confidences == null)
            {
                confidences = new float[VectorHand.NUM_JOINT_POSITIONS];
            }

            foreach (var finger in hand.fingers)
            {
                for (int bone_idx = 0; bone_idx < 4; bone_idx++)
                {
                    int key = (int)finger.Type * 4 + bone_idx;

                    Vector3 jointNormalVector = finger.GetBone((Bone.BoneType)bone_idx).Rotation * Vector3.up * -1;

                    float angle = Vector3.Angle(hand.PalmNormal, jointNormalVector);

                    // get confidence based on a cos where it should be 1 if the angle is 0,
                    // and it should be 0 if the angle is 180 degrees
                    confidences[key] = (Mathf.Cos(Mathf.Deg2Rad * angle) + 1f) / 2;
                }
            }

            return confidences;
        }

        #endregion

        #region Helper Methods

        // small class to save hand positions from old frames along with a timestamp
        class HandPositionHistory
        {
            LeapProvider provider;
            Vector3[] positions;
            float[] times;
            int index;

            public HandPositionHistory()
            {
                this.positions = new Vector3[10];
                this.times = new float[10];
                this.index = 0;
            }

            public void ClearAllPositions()
            {
                Leap.Utils.Fill(positions, Vector3.zero);
            }

            public void AddPosition(Vector3 position, float time)
            {
                positions[index] = position;
                times[index] = time;
                index = (index + 1) % 10;
            }

            public bool GetPastPosition(int pastIndex, out Vector3 position, out float time)
            {
                position = positions[(index - 1 - pastIndex + 10) % 10];
                time = times[(index - 1 - pastIndex + 10) % 10];

                if (position == null || position == Vector3.zero)
                {
                    return false;
                }
                else
                {
                    return true;
                }
            }

            public bool GetOldestPosition(out Vector3 position, out float time)
            {
                for (int i = 9; i >= 0; i--)
                {
                    if (GetPastPosition(i, out position, out time))
                    {
                        return true;
                    }
                }

                GetPastPosition(0, out position, out time);
                return false;
            }
        }

        /// <summary>
        /// add all hands in the frame given by frames[frameIdx] to the Dictionaries lastLeftHandPositions and lastRightHandPositions,
        /// and update leftHandFirstVisible and rightHandFirstVisible
        /// </summary>
        void AddFrameToTimeVisibleDicts(Frame[] frames, int frameIdx)
        {
            bool[] handsVisible = new bool[2];

            foreach (Hand hand in frames[frameIdx].Hands)
            {
                //Debug.Log(hand.Id);
                if (hand.IsLeft)
                {
                    handsVisible[0] = true;
                    if (leftHandFirstVisible[providers[frameIdx]] == 0)
                    {
                        leftHandFirstVisible[providers[frameIdx]] = Time.time;
                    }

                    if (!lastLeftHandPositions.ContainsKey(providers[frameIdx]))
                    {
                        lastLeftHandPositions.Add(providers[frameIdx], new HandPositionHistory());
                    }

                    lastLeftHandPositions[providers[frameIdx]].AddPosition(hand.PalmPosition, Time.time);
                }
                else
                {
                    handsVisible[1] = true;
                    if (rightHandFirstVisible[providers[frameIdx]] == 0)
                    {
                        rightHandFirstVisible[providers[frameIdx]] = Time.time;
                    }

                    if (!lastRightHandPositions.ContainsKey(providers[frameIdx]))
                    {
                        lastRightHandPositions.Add(providers[frameIdx], new HandPositionHistory());
                    }

                    lastRightHandPositions[providers[frameIdx]].AddPosition(hand.PalmPosition, Time.time);

                }
            }

            if (!handsVisible[0])
            {
                leftHandFirstVisible[providers[frameIdx]] = 0;
            }
            if (!handsVisible[1])
            {
                rightHandFirstVisible[providers[frameIdx]] = 0;
            }
        }

        /// <summary>
        /// small helper class to save previous joint confidences and average over them
        /// </summary>
        class JointConfidenceHistory
        {
            int length;
            float[,] jointConfidences;
            float[] averageConfidences;
            int index;
            List<int> validIndices;

            public JointConfidenceHistory(int length = 60)
            {
                this.length = length;
                this.jointConfidences = new float[length, VectorHand.NUM_JOINT_POSITIONS];
                this.index = 0;
                validIndices = new List<int>();
            }

            public void ClearAll()
            {
                validIndices = new List<int>();
            }

            public void AddConfidences(float[] confidences)
            {
                for (int joint_idx = 0; joint_idx < confidences.Length; joint_idx++)
                {
                    jointConfidences[index, joint_idx] = confidences[joint_idx];
                }
                if (validIndices.IndexOf(index) == -1)
                {
                    validIndices.Add(index);
                }
                index = (index + 1) % length;
            }

            public float[] GetAveragedConfidences()
            {
                if (validIndices.Count == 0)
                {
                    return null;
                }

                if (averageConfidences == null)
                {
                    averageConfidences = new float[jointConfidences.GetLength(1)];
                }
                else
                {
                    Leap.Utils.Fill(averageConfidences, 0);
                }

                for (int i = 0; i < averageConfidences.Length; i++)
                {
                    foreach (int j in validIndices)
                    {
                        averageConfidences[i] += jointConfidences[j, i] / validIndices.Count;
                    }
                }

                return averageConfidences;
            }
        }

        /// <summary>
        /// small helper class to save previous whole-hand confidences and average over them
        /// </summary>
        class HandConfidenceHistory
        {
            int length;
            float[] handConfidences;
            int index;
            List<int> validIndices;

            public HandConfidenceHistory(int length = 60)
            {
                this.length = length;
                this.handConfidences = new float[length];
                this.index = 0;
                validIndices = new List<int>();
            }

            public void ClearAll()
            {
                validIndices = new List<int>();
            }

            public void AddConfidence(float confidence)
            {
                handConfidences[index] = confidence;

                if (validIndices.IndexOf(index) == -1)
                {
                    validIndices.Add(index);
                }
                index = (index + 1) % length;
            }

            public float GetAveragedConfidence()
            {
                if (validIndices.Count == 0)
                {
                    return 0;
                }

                float confidenceSum = 0;
                foreach (int j in validIndices)
                {
                    confidenceSum += handConfidences[j];
                }

                return confidenceSum / validIndices.Count;
            }
        }

        /// <summary>
        /// create joint occlusion gameobjects if they are not there yet and update the position of all joint occlusion gameobjects that are attached to a xr service provider
        /// </summary>
        void SetupJointOcclusion()
        {
            if (jointOcclusions == null)
            {
                jointOcclusions = new List<JointOcclusion>();

                foreach (LeapProvider provider in providers)
                {
                    JointOcclusion jointOcclusion = provider.gameObject.GetComponentInChildren<JointOcclusion>();

                    if (jointOcclusion == null)
                    {
                        jointOcclusion = GameObject.Instantiate(Resources.Load<GameObject>("JointOcclusionPrefab"), provider.transform).GetComponent<JointOcclusion>();

                        foreach (CapsuleHand jointOcclusionHand in jointOcclusion.GetComponentsInChildren<CapsuleHand>(true))
                        {
                            jointOcclusionHand.leapProvider = provider;
                        }
                    }

                    jointOcclusions.Add(jointOcclusion);
                }

                foreach (JointOcclusion jointOcclusion in jointOcclusions)
                {
                    jointOcclusion.Setup();
                }
            }

            // if any providers are xr providers, update their jointOcclusions position and rotation
            for (int i = 0; i < jointOcclusions.Count; i++)
            {
                LeapXRServiceProvider xrProvider = providers[i] as LeapXRServiceProvider;
                if (xrProvider != null)
                {
                    Transform deviceOrigin = GetDeviceOrigin(providers[i]);

                    jointOcclusions[i].transform.SetPose(deviceOrigin.GetPose());
                    jointOcclusions[i].transform.Rotate(new Vector3(-90, 0, 180));
                }

            }
        }

        /// <summary>
        /// returns the transform of the device origin of the device corresponding to the given provider.
        /// If it is a desktop or screentop provider, this is simply provider.transform.
        /// If it is an XR provider, the main camera's transform is taken into account as well as the manual head offset values of the device
        /// </summary>
        Transform GetDeviceOrigin(LeapProvider provider)
        {
            Transform deviceOrigin = provider.transform;

            LeapXRServiceProvider xrProvider = provider as LeapXRServiceProvider;
            if (xrProvider != null)
            {
                deviceOrigin = xrProvider.mainCamera.transform;

                // xrProvider.deviceOrigin is set if camera follows a transform
                if (xrProvider.deviceOffsetMode == LeapXRServiceProvider.DeviceOffsetMode.Transform && xrProvider.deviceOrigin != null)
                {
                    deviceOrigin = xrProvider.deviceOrigin;
                }
                else if (xrProvider.deviceOffsetMode != LeapXRServiceProvider.DeviceOffsetMode.Transform)
                {
                    deviceOrigin.Translate(new Vector3(0, xrProvider.deviceOffsetYAxis, xrProvider.deviceOffsetZAxis));
                    deviceOrigin.Rotate(new Vector3(-90 - xrProvider.deviceTiltXAxis, 180, 0));
                }
            }

            return deviceOrigin;
        }

        /// <summary>
        /// visualize where the merged joint data comes from, by using the debugColors in the same order as the providers in the provider list.
        /// the color is then linearly interpolated based on the joint confidences
        /// </summary>
        void VisualizeMergedJoints(CapsuleHand hand, List<float[]> jointConfidences)
        {


            Color[] colors = hand.SphereColors;

            for (int finger_idx = 0; finger_idx < 5; finger_idx++)
            {
                for (int bone_idx = 0; bone_idx < 4; bone_idx++)
                {
                    int confidence_idx = finger_idx * 5 + bone_idx + 1;
                    int capsuleHand_idx = finger_idx * 4 + bone_idx;

                    colors[capsuleHand_idx] = debugColors[0];

                    for (int hand_idx = 1; hand_idx < jointConfidences.Count; hand_idx++)
                    {
                        float lerpValue = jointConfidences.Take(hand_idx).Sum(x => x[confidence_idx]) / jointConfidences.Take(hand_idx + 1).Sum(x => x[confidence_idx]);
                        colors[capsuleHand_idx] = Color.Lerp(debugColors[hand_idx], colors[capsuleHand_idx], lerpValue);
                    }
                }
            }

            hand.SphereColors = colors;
            hand.SetIndividualSphereColors = true;
        }

        #endregion
    }
}