/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2017.                                 *
 * Leap Motion proprietary and  confidential.                                 *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.Recording {

  public struct VectorHand {

    public const int NUM_JOINT_POSITIONS = 25;

    public bool       isLeft;
    public Vector3    palmPos;
    public Quaternion palmRot;
    
    private Vector3[]  _backingJointPositions;
    public Vector3[] jointPositions {
      get {
        if (_backingJointPositions == null) {
          _backingJointPositions = new Vector3[NUM_JOINT_POSITIONS];
        }
        return _backingJointPositions;
      }
      set {
        _backingJointPositions = value;
      }
    }

    /// <summary>
    /// Constructs a VectorHand representation from a Leap hand using VectorHand.Encode.
    /// </summary>
    public VectorHand(Hand hand) : this() {
      Encode(hand, ref this);
    }

    #region Encoding & Decoding

    /// <summary>
    /// Encodes a Leap hand into a VectorHand representation.
    /// </summary>
    public static void Encode(Hand hand, ref VectorHand toVectorHand) {
      toVectorHand.isLeft = hand.IsLeft;
      toVectorHand.palmPos = hand.PalmPosition.ToVector3();
      toVectorHand.palmRot = hand.Rotation.ToQuaternion();
      if (toVectorHand.jointPositions == null || toVectorHand.jointPositions.Length != NUM_JOINT_POSITIONS) {
        toVectorHand.jointPositions = new Vector3[NUM_JOINT_POSITIONS];
      }

      int boneIdx = 0;
      for (int i = 0; i < 5; i++) {
        Vector3 baseMetacarpal = ToLocal(hand.Fingers[i].bones[0].PrevJoint.ToVector3(),
                                         toVectorHand.palmPos, toVectorHand.palmRot);
        toVectorHand.jointPositions[boneIdx++] = baseMetacarpal;
        for (int j = 0; j < 4; j++) {
          Vector3 joint = ToLocal(hand.Fingers[i].bones[j].NextJoint.ToVector3(),
                                  toVectorHand.palmPos, toVectorHand.palmRot);
          toVectorHand.jointPositions[boneIdx++] = joint;
        }
      }
    }

    /// <summary>
    /// Decodes a VectorHand representation into a Leap hand.
    /// </summary>
    public static void Decode(ref VectorHand vectorHand, Hand toHand) {
      int boneIdx = 0;
      Vector3 prevJoint = Vector3.zero;
      Vector3 nextJoint = Vector3.zero;
      Quaternion boneRot = Quaternion.identity;

      var isLeft          = vectorHand.isLeft;
      var palmPos         = vectorHand.palmPos;
      var palmRot         = vectorHand.palmRot;
      var jointPositions  = vectorHand.jointPositions;

      // Fill fingers.
      for (int fingerIdx = 0; fingerIdx < 5; fingerIdx++) {
        for (int jointIdx = 0; jointIdx < 4; jointIdx++) {
          boneIdx   = fingerIdx * 4 + jointIdx;
          prevJoint = jointPositions[fingerIdx * 5 + jointIdx];
          nextJoint = jointPositions[fingerIdx * 5 + jointIdx + 1];

          if ((nextJoint - prevJoint).normalized == Vector3.zero) {
            // Thumb "metacarpal" slot is an identity bone.
            boneRot = Quaternion.identity;
          }
          else {
            boneRot = Quaternion.LookRotation((nextJoint - prevJoint).normalized,
                                              Vector3.Cross((nextJoint - prevJoint).normalized,
                                                (fingerIdx == 0 ?
                                                  (isLeft ? -Vector3.up : Vector3.up) 
                                                 : Vector3.right)));
          }
        
          // Convert to world space from palm space.
          nextJoint = ToWorld(nextJoint, palmPos, palmRot);
          prevJoint = ToWorld(prevJoint, palmPos, palmRot);
          boneRot = palmRot * boneRot;

          toHand.GetBone(boneIdx).Fill(prevJoint: prevJoint.ToVector(),
                                       nextJoint: nextJoint.ToVector(),
                                       center:    ((nextJoint + prevJoint) / 2f).ToVector(),
                                       direction: (palmRot * Vector3.forward).ToVector(),
                                       length:    (prevJoint - nextJoint).magnitude,
                                       width:     0.01f,
                                       type:      (Bone.BoneType)jointIdx,
                                       rotation:  boneRot.ToLeapQuaternion());
        }
        toHand.Fingers[fingerIdx].Fill(frameId:                -1,
                                       handId:                 (isLeft ? 0 : 1),
                                       fingerId:               fingerIdx,
                                       timeVisible:            Time.time,
                                       tipPosition:            nextJoint.ToVector(),
                                       tipVelocity:            Vector.Zero,
                                       direction:              (boneRot * Vector3.forward).ToVector(),
                                       stabilizedTipPosition:  nextJoint.ToVector(),
                                       width:                  1f,
                                       length:                 1f,
                                       isExtended:             true,
                                       type:                   (Finger.FingerType)fingerIdx);
      }

      // Fill arm data.
      toHand.Arm.Fill(ToWorld(new Vector3(0f, 0f, -0.3f), palmPos, palmRot).ToVector(),
                      ToWorld(new Vector3(0f, 0f, -0.055f), palmPos, palmRot).ToVector(),
                      ToWorld(new Vector3(0f, 0f, -0.125f), palmPos, palmRot).ToVector(),
                      Vector.Zero,
                      0.3f,
                      0.05f,
                      (palmRot).ToLeapQuaternion());

      // Finally, fill hand data.
      toHand.Fill(frameID:                -1,
                  id:                     (isLeft ? 0 : 1),
                  confidence:             1f,
                  grabStrength:           0.5f,
                  grabAngle:              100f,
                  pinchStrength:          0.5f,
                  pinchDistance:          50f,
                  palmWidth:              0.085f,
                  isLeft:                 isLeft,
                  timeVisible:            1f,
                  fingers:                null /* already uploaded finger data */,
                  palmPosition:           palmPos.ToVector(),
                  stabilizedPalmPosition: palmPos.ToVector(),
                  palmVelocity:           Vector3.zero.ToVector(),
                  palmNormal:             (palmRot * Vector3.down).ToVector(),
                  rotation:               (palmRot.ToLeapQuaternion()),
                  direction:              (palmRot * Vector3.forward).ToVector(),
                  wristPosition:          ToWorld(new Vector3(0f, 0f, -0.055f), palmPos, palmRot).ToVector());
    }

    #endregion

    #region Utility

    /// <summary>
    /// Converts a local-space point to a world-space point given the local space's
    /// origin and rotation.
    /// </summary>
    public static Vector3 ToWorld(Vector3 localPoint,
                                  Vector3 localOrigin, Quaternion localRot) {
      return (localRot * localPoint) + localOrigin;
    }

    /// <summary>
    /// Converts a world-space point to a local-space point given the local space's
    /// origin and rotation.
    /// </summary>
    public static Vector3 ToLocal(Vector3 worldPoint,
                                  Vector3 localOrigin, Quaternion localRot) {
      return Quaternion.Inverse(localRot) * (worldPoint - localOrigin);
    }

    #endregion

  }

  #region Utility Extension Methods

  public static class VectorHandUtilityExtensions {

    /// <summary>
    /// Returns a bone object from the hand as if all bones were aligned metacarpal-
    /// to-tip and thumb-to-pinky. So 0-3 represent thumb bones, 4-7 represent index
    /// bones, etc. There are 20 such Bones in a Hand.
    /// </summary>
    public static Bone GetBone(this Hand hand, int boneIdx) {
      return hand.Fingers[boneIdx / 4].bones[boneIdx % 4];
    }

  }

  #endregion

}
