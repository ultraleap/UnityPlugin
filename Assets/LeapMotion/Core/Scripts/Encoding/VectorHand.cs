/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2018.                                 *
 * Leap Motion proprietary and confidential.                                  *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

using System;
using UnityEngine;

namespace Leap.Unity.Encoding {

  /// <summary>
  /// A Vector-based encoding of a Leap Hand.
  /// 
  /// You can Encode a VectorHand from a Leap hand, Decode a VectorHand into a Leap hand,
  /// convert the VectorHand to a compressed byte representation using FillBytes,
  /// and decompress back into a VectorHand using FromBytes.
  /// 
  /// Also see CurlHand for a more compressed but slightly less articulated encoding.
  /// TODO: CurlHand not yet brought in from Networking module!
  /// </summary>
  [Serializable]
  public class VectorHand {

    #region Data

    public const int NUM_JOINT_POSITIONS = 25;

    public bool       isLeft;
    public Vector3    palmPos;
    public Quaternion palmRot;
    
    [SerializeField]
    private Vector3[]  _backingJointPositions;
    public Vector3[] jointPositions {
      get {
        if (_backingJointPositions == null) {
          _backingJointPositions = new Vector3[NUM_JOINT_POSITIONS];
        }
        return _backingJointPositions;
      }
    }

    #endregion

    public VectorHand() { }

    /// <summary>
    /// Constructs a VectorHand representation from a Leap hand. This allocates a vector
    /// array for the encoded hand data.
    /// 
    /// Use a pooling strategy to avoid unnecessary allocation in runtime contexts.
    /// </summary>
    public VectorHand(Hand hand) : this() {
      Encode(hand);
    }

    #region Hand Encoding

    public void Encode(Hand fromHand) {
      isLeft = fromHand.IsLeft;
      palmPos = fromHand.PalmPosition.ToVector3();
      palmRot = fromHand.Rotation.ToQuaternion();

      int boneIdx = 0;
      for (int i = 0; i < 5; i++) {
        Vector3 baseMetacarpal = ToLocal(fromHand.Fingers[i].bones[0].PrevJoint.ToVector3(),
                                         palmPos, palmRot);
        jointPositions[boneIdx++] = baseMetacarpal;
        for (int j = 0; j < 4; j++) {
          Vector3 joint = ToLocal(fromHand.Fingers[i].bones[j].NextJoint.ToVector3(),
                                  palmPos, palmRot);
          jointPositions[boneIdx++] = joint;
        }
      }
    }

    public void Decode(Hand intoHand) {
      int boneIdx = 0;
      Vector3 prevJoint = Vector3.zero;
      Vector3 nextJoint = Vector3.zero;
      Quaternion boneRot = Quaternion.identity;

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
            boneRot = Quaternion.LookRotation(
                        (nextJoint - prevJoint).normalized,
                        Vector3.Cross((nextJoint - prevJoint).normalized,
                                      (fingerIdx == 0 ?
                                        (isLeft ? -Vector3.up : Vector3.up) 
                                       : Vector3.right)));
          }
        
          // Convert to world space from palm space.
          nextJoint = ToWorld(nextJoint, palmPos, palmRot);
          prevJoint = ToWorld(prevJoint, palmPos, palmRot);
          boneRot = palmRot * boneRot;

          intoHand.GetBone(boneIdx).Fill(
            prevJoint: prevJoint.ToVector(),
            nextJoint: nextJoint.ToVector(),
            center: ((nextJoint + prevJoint) / 2f).ToVector(),
            direction: (palmRot * Vector3.forward).ToVector(),
            length: (prevJoint - nextJoint).magnitude,
            width: 0.01f,
            type: (Bone.BoneType)jointIdx,
            rotation: boneRot.ToLeapQuaternion());
        }
        intoHand.Fingers[fingerIdx].Fill(
          frameId: -1,
          handId: (isLeft ? 0 : 1),
          fingerId: fingerIdx,
          timeVisible: Time.time,
          tipPosition: nextJoint.ToVector(),
          direction: (boneRot * Vector3.forward).ToVector(),
          width: 1f,
          length: 1f,
          isExtended: true,
          type: (Finger.FingerType)fingerIdx);
      }

      // Fill arm data.
      intoHand.Arm.Fill(ToWorld(new Vector3(0f, 0f, -0.3f), palmPos, palmRot).ToVector(),
                      ToWorld(new Vector3(0f, 0f, -0.055f), palmPos, palmRot).ToVector(),
                      ToWorld(new Vector3(0f, 0f, -0.125f), palmPos, palmRot).ToVector(),
                      Vector.Zero,
                      0.3f,
                      0.05f,
                      (palmRot).ToLeapQuaternion());

      // Finally, fill hand data.
      intoHand.Fill(frameID:                -1,
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
                  wristPosition:          ToWorld(new Vector3(0f, 0f, -0.055f),
                                                  palmPos,
                                                  palmRot).ToVector());
    }

    #endregion

    #region Byte Encoding & Decoding

    /// <summary>
    /// The number of bytes required to encode a VectorHand into its byte representation.
    /// The byte representation is compressed to 86 bytes.
    /// 
    /// The first byte determines chirality, the camera-local hand position uses 6 bytes,
    /// the camera-local hand rotation uses 4 bytes, and each joint position component is
    /// encoded in hand-local space using 3 bytes.
    /// </summary>
    public int numBytesRequired { get { return 86; } }

    /// <summary>
    /// Fills this VectorHand with data read from the provided byte array, starting at
    /// the provided offset.
    /// </summary>
    public void ReadBytes(byte[] bytes, ref int offset) {
      if (bytes.Length - offset < numBytesRequired) {
        throw new System.IndexOutOfRangeException(
          "Not enough room to read bytes for VectorHand encoding starting at offset "
          + offset + " for array of size " + bytes + "; need at least "
          + numBytesRequired + " bytes from the offset position.");
      }

      // Chirality.
      isLeft = bytes[offset++] == 0x00;

      // Palm position and rotation.
      for (int i = 0; i < 3; i++) {
        palmPos[i] = Convert.ToSingle(
                       BitConverterNonAlloc.ToInt16(bytes, ref offset))
                     / 4096f;
      }
      palmRot = Utils.DecompressBytesToQuat(bytes, ref offset);

      // Palm-local bone joint positions.
      for (int i = 0; i < NUM_JOINT_POSITIONS; i++) {
        for (int j = 0; j < 3; j++) {
          jointPositions[i][j] = VectorHandExtensions.ByteToFloat(bytes[offset++]);
        }
      }
    }

    /// <summary>
    /// Fills the provided byte array with a compressed, 86-byte form of this VectorHand,
    /// starting at the provided offset.
    /// 
    /// Throws an IndexOutOfRangeException if the provided byte array doesn't have enough
    /// space (starting from the offset) to write the number of bytes required.
    /// </summary>
    public void FillBytes(byte[] bytesToFill, ref int offset) {
      if (_backingJointPositions == null) {
        throw new System.InvalidOperationException(
          "Joint positions array is null. You must fill a VectorHand with data before "
        + "you can use it to fill byte representations.");
      }

      if (bytesToFill.Length - offset < numBytesRequired) {
        throw new System.IndexOutOfRangeException(
          "Not enough room to fill bytes for VectorHand encoding starting at offset "
        + offset + " for array of size " + bytesToFill.Length + "; need at least "
        + numBytesRequired + " bytes from the offset position.");
      }

      // Chirality.
      bytesToFill[offset++] = isLeft ? (byte)0x00 : (byte)0x01;
      
      // Palm position, each component compressed 
      for (int i = 0; i < 3; i++) {
        BitConverterNonAlloc.GetBytes(Convert.ToInt16(palmPos[i] * 4096f),
                                      bytesToFill,
                                      ref offset);
      }

      // Palm rotation.
      Utils.CompressQuatToBytes(palmRot, bytesToFill, ref offset);

      // Joint positions.
      for (int j = 0; j < NUM_JOINT_POSITIONS; j++) {
        for (int i = 0; i < 3; i++) {
          bytesToFill[offset++] =
            VectorHandExtensions.FloatToByte(jointPositions[j][i]);
        }
      }
    }

    /// <summary>
    /// Fills the provided byte array with a compressed, 86-byte form of this VectorHand.
    /// 
    /// Throws an IndexOutOfRangeException if the provided byte array doesn't have enough
    /// space to write the number of bytes required (see VectorHand.BYTE_ENCODING_SIZE).
    /// </summary>
    public void FillBytes(byte[] bytesToFill) {
      int unusedOffset = 0;
      FillBytes(bytesToFill, ref unusedOffset);
    }


    /// <summary>
    /// Shortcut for reading a VectorHand-encoded byte representation of a Leap hand and
    /// decoding it immediately into a Hand object.
    /// </summary>
    public void ReadBytes(byte[] bytes, ref int offset, Hand intoHand) {
      ReadBytes(bytes, ref offset);
      Decode(intoHand);
    }

    /// <summary>
    /// Shortcut for encoding a Leap hand into a VectorHand representation and
    /// compressing it immediately into a byte representation.
    /// </summary>
    public void FillBytes(byte[] bytes, ref int offset, Hand fromHand) {
      Encode(fromHand);
      FillBytes(bytes, ref offset);
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

  public static class VectorHandExtensions {

    #region VectorHand Instance API

    //public static void FillBytes(this VectorHand vectorHand, )

    #endregion

    #region Utilities

    /// <summary>
    /// Returns a bone object from the hand as if all bones were aligned metacarpal-
    /// to-tip and thumb-to-pinky. So 0-3 represent thumb bones, 4-7 represent index
    /// bones, etc. There are 20 such Bones in a Hand.
    /// </summary>
    public static Bone GetBone(this Hand hand, int boneIdx) {
      return hand.Fingers[boneIdx / 4].bones[boneIdx % 4];
    }

    /// <summary>
    /// Compresses a float into a byte based on the desired movement range.
    /// </summary>
    public static byte FloatToByte(float inFloat, float movementRange = 0.3f) {
      float clamped = Mathf.Clamp(inFloat, -movementRange / 2f, movementRange / 2f);
      clamped += movementRange / 2f;
      clamped /= movementRange;
      clamped *= 255f;
      clamped = Mathf.Floor(clamped);
      return (byte)clamped;
    }

    /// <summary>
    /// Expands a byte back into a float based on the desired movement range.
    /// </summary>
    public static float ByteToFloat(byte inByte, float movementRange = 0.3f) {
      float clamped = (float)inByte;
      clamped /= 255f;
      clamped *= movementRange;
      clamped -= movementRange / 2f;
      return clamped;
    }

    #endregion

  }

  #endregion

}
