using Leap;
using Leap.Unity;
using Leap.Unity.Encoding;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.EventSystems;

/// <summary>
/// Modifies a hand tracking data source using data from the PoseToIRImageConfidence
/// source
/// </summary>
public class ImageBasedConfidenceProvider : PostProcessProvider
{
    public PoseToIRImageConfidence imageBasedConfidence;

    /// <summary>
    /// Degree to which each joint in the finger is blended 0<->1, going from the metacarpal to the tip
    /// </summary>
    public float[] JointLerpFactors = { 0.0f, 0.5f, 1.0f, 1.0f, 1.0f };

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    /// <summary>
    /// Post-processes the input frame in place to give hands bouncy-feeling physics.
    /// </summary>
    public override void ProcessFrame(ref Frame inputFrame)
    {
        var leftHand = inputFrame.Hands.FirstOrDefault(h => h.IsLeft);
        var rightHand = inputFrame.Hands.FirstOrDefault(h => !h.IsLeft);

        if (leftHand != null && imageBasedConfidence != null)
        {
            UpdateHandFromConfidence(ref leftHand, inputFrame.Timestamp, imageBasedConfidence.TipConfidence.LeftConfidence);
        }

        if (rightHand != null && imageBasedConfidence != null)
        {
            UpdateHandFromConfidence(ref rightHand, inputFrame.Timestamp, imageBasedConfidence.TipConfidence.RightConfidence);
        }
    }

    private void UpdateHandFromConfidence(ref Hand inputHand, long timestamp, HandFingerConfidences confidenceInfo)
    {
        if (confidenceInfo != null)
        {
            // Only affect the thumb and little finger for now
            UpdateFinger(ref inputHand, (int)Finger.FingerType.TYPE_THUMB, confidenceInfo.Thumb);
            //UpdateFinger(ref inputHand, (int)Finger.FingerType.TYPE_INDEX, confidenceInfo.Index);
            //UpdateFinger(ref inputHand, (int)Finger.FingerType.TYPE_MIDDLE, confidenceInfo.Middle);
            //UpdateFinger(ref inputHand, (int)Finger.FingerType.TYPE_RING, confidenceInfo.Ring);
            UpdateFinger(ref inputHand, (int)Finger.FingerType.TYPE_PINKY, confidenceInfo.Pinky);
        }
    }

    public void UpdateFinger(ref Hand hand, int fingerIndex, FingerConfidence fingerConfidence)
    {
        if (fingerConfidence.LastConfidentPositionForFinger_LeapSpace != null)
        {
            hand.Fingers[fingerIndex] = LerpFinger(hand, hand.Fingers[fingerIndex], fingerConfidence.LastConfidentPositionForFinger_LeapSpace);
        }
    }

    public Finger LerpFinger(Hand ha, Finger fa, Finger fb)
    {
        return LerpFinger(ha, fa, fb, JointLerpFactors);
    }

    public static Finger LerpFinger(Hand ha, Finger fa, Finger fb, float[] jointLerpFactors)
    {
        Assert.AreEqual(5, jointLerpFactors.Length);
        Finger f = new Finger();

        if (ha == null || fa == null || fb == null)
            return null;

        // We are using the target hand info for palm position and rotation
        Vector3 palmPos = ha.GetPalmPose().position; //  Vector3.LerpUnclamped(a.palmPos, b.palmPos, t);
        Quaternion palmRot = ha.GetPalmPose().rotation; // Quaternion.SlerpUnclamped(a.palmRot, b.palmRot, t);

        int boneIdx;
        Vector3 prevJointA, prevJointB;
        Vector3 nextJointA, nextJointB;
        Vector3 prevJoint, nextJoint = new Vector3();
        Quaternion boneRot = Quaternion.identity;

        // Lerp bones
        for (int jointIdx = 0; jointIdx < 4; jointIdx++)
        {
            boneIdx = jointIdx;

            prevJointA = fa.bones[boneIdx].PrevJoint;
            prevJointB = fb.bones[boneIdx].PrevJoint;

            nextJointA = fa.bones[boneIdx].NextJoint;
            nextJointB = fb.bones[boneIdx].NextJoint; 

            prevJoint = Vector3.LerpUnclamped(prevJointA, prevJointB, jointLerpFactors[jointIdx]);
            nextJoint = Vector3.LerpUnclamped(nextJointA, nextJointB, jointLerpFactors[jointIdx + 1]);

            boneRot = Quaternion.LerpUnclamped(fa.bones[boneIdx].Rotation, fb.bones[boneIdx].Rotation, jointLerpFactors[jointIdx]);

            f.bones[boneIdx].Fill(
                  prevJoint: prevJoint,
                  nextJoint: nextJoint,
                  center: ((nextJoint + prevJoint) / 2f),
                  direction: (palmRot * Vector3.forward),
                  length: (prevJoint - nextJoint).magnitude,
                  width: 0.01f,
                  type: (Bone.BoneType)jointIdx,
                  rotation: boneRot);
        }

        f.Fill(frameId: -1,
                  handId: (ha.IsLeft ? 0 : 1),
                  fingerId: (int)fa.Type,
                  timeVisible: 10f,// Time.time, <- This is unused and main thread only
                  tipPosition: nextJoint,
                  direction: (boneRot * Vector3.forward),
                  width: 1f,
                  length: 1f,
                  isExtended: true,
                  type: fa.Type);

        return f;
    }

    /// <summary>
    /// Old - comes from VectorHand which does not preserve rotations so they cannot be interpolated, this causes issues with mesh hands. 
    /// </summary>
    /// <param name="ha"></param>
    /// <param name="fa"></param>
    /// <param name="fb"></param>
    /// <param name="jointLerpFactors"></param>
    /// <returns></returns>
    public static Finger LerpFinger2(Hand ha, Finger fa, Finger fb, float[] jointLerpFactors)
    {
        Assert.AreEqual(5, jointLerpFactors.Length);
        Finger f = new Finger();

        if (ha == null || fa == null || fb == null)
            return null;

        Vector3 palmPos = ha.GetPalmPose().position; //  Vector3.LerpUnclamped(a.palmPos, b.palmPos, t);
        Quaternion palmRot = ha.GetPalmPose().rotation; // Quaternion.SlerpUnclamped(a.palmRot, b.palmRot, t);

        int boneIdx = 0;
        Vector3 prevJointA, prevJointB;
        Vector3 nextJointA, nextJointB;

        Vector3 cachedTip = new Vector3();

        // Lerp bones
        for (int jointIdx = 0; jointIdx < 4; jointIdx++)
        {
            boneIdx = jointIdx;

            prevJointA = fa.bones[boneIdx].PrevJoint;
            prevJointB = fb.bones[boneIdx].PrevJoint;

            nextJointA = fa.bones[boneIdx].NextJoint;
            nextJointB = fb.bones[boneIdx].NextJoint;

            f.bones[boneIdx].PrevJoint = Vector3.LerpUnclamped(prevJointA, prevJointB, jointLerpFactors[jointIdx]);
            f.bones[boneIdx].NextJoint = Vector3.LerpUnclamped(nextJointA, nextJointB, jointLerpFactors[jointIdx + 1]);

            if (boneIdx == (int)Bone.BoneType.TYPE_DISTAL)
            {
                cachedTip = f.bones[boneIdx].NextJoint;
            }  
        }

        // Might have to lerp the rotations of the bones?????
        //Quaternion.LerpUnclamped();

        Vector3 prevJoint, nextJoint = new Vector3();
        Quaternion boneRot = Quaternion.identity;

        // Set the cached tip position and the next joint to the lerped location
        f.TipPosition = cachedTip;
        f.bones[(int)Bone.BoneType.TYPE_DISTAL].NextJoint = cachedTip;

        // Regenerate other finger data
        for (int jointIdx = 0; jointIdx < 4; jointIdx++)
        {
            boneIdx = jointIdx;

            prevJoint = f.bones[boneIdx].PrevJoint;
            nextJoint = f.bones[boneIdx].NextJoint;
           
            if ((nextJoint - prevJoint).normalized == Vector3.zero)
            {
                // Thumb "metacarpal" slot is an identity bone.
                boneRot = Quaternion.identity;
            }
            else
            {
                boneRot = Quaternion.LookRotation(
                            (nextJoint - prevJoint).normalized,
                            Vector3.Cross((nextJoint - prevJoint).normalized,
                                          (ha.Id == 0 ?
                                            (ha.IsLeft ? -Vector3.up : Vector3.up)
                                           : Vector3.right)));
            }

            // Convert to world space from palm space.
            nextJoint = VectorHand.ToWorld(nextJoint, palmPos, palmRot);
            prevJoint = VectorHand.ToWorld(prevJoint, palmPos, palmRot);
            boneRot = palmRot * boneRot;

            f.bones[boneIdx].Fill(
              prevJoint: prevJoint,
              nextJoint: nextJoint,
              center: ((nextJoint + prevJoint) / 2f),
              direction: (palmRot * Vector3.forward),
              length: (prevJoint - nextJoint).magnitude,
              width: 0.01f,
              type: (Bone.BoneType)jointIdx,
              rotation: boneRot);  
        }

        f.Fill(frameId: -1,
                  handId: (ha.IsLeft ? 0 : 1),
                  fingerId: (int)fa.Type,
                  timeVisible: 10f,// Time.time, <- This is unused and main thread only
                  tipPosition: nextJoint,
                  direction: (boneRot * Vector3.forward),
                  width: 1f,
                  length: 1f,
                  isExtended: true,
                  type: fa.Type );

        // Set the cached tip position and the next joint to the lerped location
        f.TipPosition = cachedTip; 
        f.bones[(int)Bone.BoneType.TYPE_DISTAL].NextJoint = cachedTip;

        return f;
    }
}
