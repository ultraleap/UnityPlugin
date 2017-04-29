using Leap.Unity.Attributes;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.Attachments {

  /// <summary>
  /// Simple container class for storing a reference to the attachment point this
  /// transform corresponds to within an AttachmentHand.
  /// 
  /// Can be used implicitly as a reference to a single AttachmentPointFlags flag constant.
  /// </summary>
  [AddComponentMenu("")]
  public class AttachmentPointBehaviour : MonoBehaviour, IEquatable<AttachmentPointBehaviour> {

    [Disable]
    public AttachmentPointFlags attachmentPoint;

    void OnValidate() {
      if (!attachmentPoint.IsSinglePoint() && attachmentPoint != AttachmentPointFlags.None) {
        Debug.LogError("AttachmentPointBehaviours should refer to a single attachmentPoint flag.", this.gameObject);
        attachmentPoint = AttachmentPointFlags.None;
      }
    }

    public bool Equals(AttachmentPointBehaviour other) {
      return this.attachmentPoint == other.attachmentPoint;
    }

    public static implicit operator AttachmentPointFlags(AttachmentPointBehaviour p) {
      return p.attachmentPoint;
    }

    public void SetTransformUsingHand(Leap.Hand hand) {
      if (hand == null) {
        //Debug.LogError("Unable to set transform with a null hand.", this.gameObject);
        return;
      }

      Vector3 position = Vector3.zero;
      Quaternion rotation = Quaternion.identity;

      switch (attachmentPoint) {
        case AttachmentPointFlags.None:
          Debug.LogError("Unable to set transform; this AttachmentPointBehaviour does not have its attachment point flag set.");
          return;

        case AttachmentPointFlags.Wrist:
          position = hand.WristPosition.ToVector3();
          rotation = hand.Arm.Basis.rotation.ToQuaternion();
          break;
        case AttachmentPointFlags.Palm:
          position = hand.PalmPosition.ToVector3();
          rotation = hand.Basis.rotation.ToQuaternion();
          break;

        case AttachmentPointFlags.ThumbProximalJoint:
          position = hand.Fingers[0].bones[1].NextJoint.ToVector3();
          rotation = hand.Fingers[0].bones[2].Rotation.ToQuaternion();
          break;
        case AttachmentPointFlags.ThumbDistalJoint:
          position = hand.Fingers[0].bones[2].NextJoint.ToVector3();
          rotation = hand.Fingers[0].bones[3].Rotation.ToQuaternion();
          break;
        case AttachmentPointFlags.ThumbTip:
          position = hand.Fingers[0].bones[3].NextJoint.ToVector3();
          rotation = hand.Fingers[0].bones[3].Rotation.ToQuaternion();
          break;

        case AttachmentPointFlags.IndexKnuckle:
          position = hand.Fingers[1].bones[0].NextJoint.ToVector3();
          rotation = hand.Fingers[1].bones[1].Rotation.ToQuaternion();
          break;
        case AttachmentPointFlags.IndexMiddleJoint:
          position = hand.Fingers[1].bones[1].NextJoint.ToVector3();
          rotation = hand.Fingers[1].bones[2].Rotation.ToQuaternion();
          break;
        case AttachmentPointFlags.IndexDistalJoint:
          position = hand.Fingers[1].bones[2].NextJoint.ToVector3();
          rotation = hand.Fingers[1].bones[3].Rotation.ToQuaternion();
          break;
        case AttachmentPointFlags.IndexTip:
          position = hand.Fingers[1].bones[3].NextJoint.ToVector3();
          rotation = hand.Fingers[1].bones[3].Rotation.ToQuaternion();
          break;

        case AttachmentPointFlags.MiddleKnuckle:
          position = hand.Fingers[2].bones[0].NextJoint.ToVector3();
          rotation = hand.Fingers[2].bones[1].Rotation.ToQuaternion();
          break;
        case AttachmentPointFlags.MiddleMiddleJoint:
          position = hand.Fingers[2].bones[1].NextJoint.ToVector3();
          rotation = hand.Fingers[2].bones[2].Rotation.ToQuaternion();
          break;
        case AttachmentPointFlags.MiddleDistalJoint:
          position = hand.Fingers[2].bones[2].NextJoint.ToVector3();
          rotation = hand.Fingers[2].bones[3].Rotation.ToQuaternion();
          break;
        case AttachmentPointFlags.MiddleTip:
          position = hand.Fingers[2].bones[3].NextJoint.ToVector3();
          rotation = hand.Fingers[2].bones[3].Rotation.ToQuaternion();
          break;

        case AttachmentPointFlags.RingKnuckle:
          position = hand.Fingers[3].bones[0].NextJoint.ToVector3();
          rotation = hand.Fingers[3].bones[1].Rotation.ToQuaternion();
          break;
        case AttachmentPointFlags.RingMiddleJoint:
          position = hand.Fingers[3].bones[1].NextJoint.ToVector3();
          rotation = hand.Fingers[3].bones[2].Rotation.ToQuaternion();
          break;
        case AttachmentPointFlags.RingDistalJoint:
          position = hand.Fingers[3].bones[2].NextJoint.ToVector3();
          rotation = hand.Fingers[3].bones[3].Rotation.ToQuaternion();
          break;
        case AttachmentPointFlags.RingTip:
          position = hand.Fingers[3].bones[3].NextJoint.ToVector3();
          rotation = hand.Fingers[3].bones[3].Rotation.ToQuaternion();
          break;

        case AttachmentPointFlags.PinkyKnuckle:
          position = hand.Fingers[4].bones[0].NextJoint.ToVector3();
          rotation = hand.Fingers[4].bones[1].Rotation.ToQuaternion();
          break;
        case AttachmentPointFlags.PinkyMiddleJoint:
          position = hand.Fingers[4].bones[1].NextJoint.ToVector3();
          rotation = hand.Fingers[4].bones[2].Rotation.ToQuaternion();
          break;
        case AttachmentPointFlags.PinkyDistalJoint:
          position = hand.Fingers[4].bones[2].NextJoint.ToVector3();
          rotation = hand.Fingers[4].bones[3].Rotation.ToQuaternion();
          break;
        case AttachmentPointFlags.PinkyTip:
          position = hand.Fingers[4].bones[3].NextJoint.ToVector3();
          rotation = hand.Fingers[4].bones[3].Rotation.ToQuaternion();
          break;
      }

      this.transform.position = position;
      this.transform.rotation = rotation;
    }

  }

}