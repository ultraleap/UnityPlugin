/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2020.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using UnityEngine;
using System.Collections;
using Leap;
using Leap.Unity;

namespace Leap.Unity{
  /**
  * The base class for all fingers.
  *
  * This class serves as the interface between the HandController object,
  * the parent Hand object and the concrete finger objects.
  *
  * Subclasses of FingerModel must implement InitFinger() and UpdateFinger(). The InitHand() function
  * is typically called by the parent HandModel InitHand() method; likewise, the UpdateFinger()
  * function is typically called by the parent HandModel UpdateHand() function.
  */

  public abstract class FingerModel : MonoBehaviour {

    /** The number of bones in a finger. */
    public const int NUM_BONES = 4;

    /** The number of joints in a finger. */
    public const int NUM_JOINTS = 3;

    public Finger.FingerType fingerType = Finger.FingerType.TYPE_INDEX;

    // Unity references
    /** Bones positioned and rotated by FingerModel. */
    public Transform[] bones = new Transform[NUM_BONES];
    /** Joints positioned and rotated by FingerModel. */
    public Transform[] joints = new Transform[NUM_BONES - 1];

    // Leap references
    /** The Leap Hand object. */
    protected Hand hand_;
    /** The Leap Finger object. */
    protected Finger finger_;

    /** Sets the Leap Hand and Leap Finger for this finger.
    * Note that Leap Hand and Finger objects are recreated every frame. The
    * parent HandModel object calls this function to set or update the underlying
    * finger. The tracking data in the Leap objects are used to update the FingerModel.
    */
    public void SetLeapHand(Hand hand) {
      hand_ = hand;
      if (hand_ != null) {
        finger_ = hand.Fingers[(int)fingerType];
      }
    }

    /** The Leap Hand object. */
    public Hand GetLeapHand() { return hand_; }
    /** The Leap Finger object. */
    public Finger GetLeapFinger() { return finger_; }

    /**
    * Implement this function to initialize this finger after it is created.
    * Typically, this function is called by the parent HandModel object.
    */
    public virtual void InitFinger() {
      UpdateFinger();
    }

    /**
    * Implement this function to update this finger once per game loop.
    * Typically, this function is called by the parent HandModel object's
    * UpdateHand() function, which is called in the Unity Update() phase for
    * graphics hand models and in the FixedUpdate() phase for physics hand
    * models.
    */
    public abstract void UpdateFinger();

    /** Returns the location of the tip of the finger */
    public Vector3 GetTipPosition() {
      if (finger_ != null) {
        Vector3 local_tip = finger_.Bone ((Bone.BoneType.TYPE_DISTAL)).NextJoint.ToVector3();
        return local_tip;
      }
      if (bones [NUM_BONES - 1] && joints [NUM_JOINTS - 2]) {
        return 2f*bones [NUM_BONES - 1].position - joints [NUM_JOINTS - 2].position;
      }
      return Vector3.zero;
    }

    /** Returns the location of the given joint on the finger */
    public Vector3 GetJointPosition(int joint) {
      if (joint >= NUM_BONES) {
        return GetTipPosition ();
      }
      if (finger_ != null) {
        Vector3 local_position = finger_.Bone((Bone.BoneType)(joint)).PrevJoint.ToVector3();
        return local_position;
      }
      if (joints [joint]) {
        return joints[joint].position;
      }
      return Vector3.zero;
    }

    /** Returns a ray from the tip of the finger in the direction it is pointing.*/
    public Ray GetRay() {
      Ray ray = new Ray(GetTipPosition(), GetBoneDirection(NUM_BONES - 1));
      return ray;
    }

    /** Returns the center of the given bone on the finger */
    public Vector3 GetBoneCenter(int bone_type) {
      if (finger_ != null) {
        Bone bone = finger_.Bone ((Bone.BoneType)(bone_type));
        return bone.Center.ToVector3();
      }
      if (bones [bone_type]) {
        return bones[bone_type].position;
      }
      return Vector3.zero;
    }

    /** Returns the direction the given bone is facing on the finger */
    public Vector3 GetBoneDirection(int bone_type) {
      if (finger_ != null) {
        Vector3 direction = GetJointPosition (bone_type + 1) - GetJointPosition (bone_type);
        return direction.normalized;
      }
      if (bones[bone_type]) {
        return bones[bone_type].forward;
      }
      return Vector3.forward;
    }

    /** Returns the rotation quaternion of the given bone */
    public Quaternion GetBoneRotation(int bone_type) {
      if (finger_ != null) {
        Quaternion local_rotation = finger_.Bone ((Bone.BoneType)(bone_type)).Rotation.ToQuaternion();
        return local_rotation;
      }
      if (bones[bone_type]) {
        return bones[bone_type].rotation;
      }
      return Quaternion.identity;
    }

    /** Returns the length of the finger bone.*/
    public float GetBoneLength(int bone_type) {
      return finger_.Bone((Bone.BoneType)(bone_type)).Length;
    }

    /** Returns the width of the finger bone.*/
    public float GetBoneWidth(int bone_type) {
      return finger_.Bone((Bone.BoneType)(bone_type)).Width;
    }

    /**
     * Returns Mecanim stretch angle in the range (-180, +180]
     * NOTE: Positive stretch opens the hand.
     * For the thumb this moves it away from the palm.
     */
    public float GetFingerJointStretchMecanim(int joint_type) {
      // The successive actions of local rotations on a vector yield the global rotation,
      // so the inverse of the parent rotation appears on the left.
      Quaternion jointRotation = Quaternion.identity;
      if (finger_ != null) {
        jointRotation = Quaternion.Inverse(finger_.Bone((Bone.BoneType)(joint_type)).Rotation.ToQuaternion())
          * finger_.Bone ((Bone.BoneType)(joint_type + 1)).Rotation.ToQuaternion();
      } else if (bones [joint_type] && bones [joint_type + 1]) {
        jointRotation = Quaternion.Inverse (GetBoneRotation (joint_type)) * GetBoneRotation (joint_type + 1);
      }
      // Stretch is a rotation around the X axis of the base bone
      // Positive stretch opens joints
      float stretchAngle = -jointRotation.eulerAngles.x;
      if (stretchAngle <= -180f) {
        stretchAngle += 360f;
      }
      // NOTE: eulerAngles range is [0, 360) so stretchAngle > +180f will not occur.
      return stretchAngle;
    }

    /**
     * Returns Mecanim spread angle, which only applies to joint_type = 0
     * NOTE: Positive spread is towards thumb for index and middle,
     * but is in the opposite direction for the ring and pinky.
     * For the thumb negative spread rotates the thumb in to the palm.
     * */
    public float GetFingerJointSpreadMecanim() {
      // The successive actions of local rotations on a vector yield the global rotation,
      // so the inverse of the parent rotation appears on the left.
      Quaternion jointRotation = Quaternion.identity;
      if (finger_ != null) {
        jointRotation = Quaternion.Inverse (finger_.Bone ((Bone.BoneType)(0)).Rotation.ToQuaternion())
          * finger_.Bone ((Bone.BoneType)(1)).Rotation.ToQuaternion();
      } else if (bones [0] && bones [1]) {
        jointRotation = Quaternion.Inverse (GetBoneRotation (0)) * GetBoneRotation (1);
      }
      // Spread is a rotation around the Y axis of the base bone when joint_type = 0
      float spreadAngle = 0f;
      Finger.FingerType fType = fingerType;
      if (finger_ != null) {
        fingerType = finger_.Type;
      }

      if (fType == Finger.FingerType.TYPE_INDEX ||
        fType == Finger.FingerType.TYPE_MIDDLE) {
        spreadAngle = jointRotation.eulerAngles.y;
        if (spreadAngle > 180f) {
          spreadAngle -= 360f;
        }
        // NOTE: eulerAngles range is [0, 360) so spreadAngle <= -180f will not occur.
      }
      if (fType == Finger.FingerType.TYPE_THUMB ||
        fType == Finger.FingerType.TYPE_RING ||
        fType == Finger.FingerType.TYPE_PINKY) {
        spreadAngle = -jointRotation.eulerAngles.y;
        if (spreadAngle <= -180f) {
          spreadAngle += 360f;
        }
        // NOTE: eulerAngles range is [0, 360) so spreadAngle > +180f will not occur.
      }
      return spreadAngle;
    }
  }
}
