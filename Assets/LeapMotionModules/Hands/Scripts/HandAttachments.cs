using UnityEngine;
using System.Collections;
using Leap;
using Leap.Unity;

/**
* An IHandModel object that has no graphics of its own, but which allows you to 
* add transforms matched to points on the hand, fingertips, and arm.
*
* To enable an attachment point, add an empty GameObject as a child of the 
* object containing this script. Then drag the the child to the desired
* Transform slot. For example, to attach game objects to the palm, drag the child
* to the Palm slot. You can adjust the position rotation and scale of the child relative
* to the attachment point and the object will maintain its place relative to that point 
* when the hand is live. Add other game objects, such as UI elements, primitives, meshes,
* etc. under the attachment point.
*
* Attachment points are updated during the Unity Update loop.
*/
public class HandAttachments : IHandModel {

  public Transform Palm;
  public Transform Arm;
  public Transform Thumb;
  public Transform Index;
  public Transform Middle;
  public Transform Ring;
  public Transform Pinky;

  private Hand _hand;

  public override ModelType HandModelType {
    get {
      return ModelType.Graphics;
    }
  }

  [SerializeField]
  private Chirality _handedness;
  public override Chirality Handedness {
    get {
      return _handedness;
    }
  }

  public override void SetLeapHand(Hand hand) {
    _hand = hand;
  }

  public override Hand GetLeapHand() {
    return _hand;
  }

  public override void UpdateHand () {
    if(Palm != null) {
      Palm.position = _hand.PalmPosition.ToVector3();
      Palm.rotation = _hand.Basis.rotation.ToQuaternion();
    }
    if(Arm != null) {
      Arm.position = _hand.Arm.Center.ToVector3();
      Arm.rotation = _hand.Arm.Basis.rotation.ToQuaternion();
    }
    if(Thumb != null) {
      Thumb.position = _hand.Fingers[0].Bone(Bone.BoneType.TYPE_DISTAL).NextJoint.ToVector3();
      Thumb.rotation = _hand.Fingers[0].Bone(Bone.BoneType.TYPE_DISTAL).Rotation.ToQuaternion();
    }
    if(Index != null) {
      Index.position = _hand.Fingers[1].Bone(Bone.BoneType.TYPE_DISTAL).NextJoint.ToVector3();
      Index.rotation = _hand.Fingers[1].Bone(Bone.BoneType.TYPE_DISTAL).Rotation.ToQuaternion();
    }
    if(Middle != null) {
      Middle.position = _hand.Fingers[2].Bone(Bone.BoneType.TYPE_DISTAL).NextJoint.ToVector3();
      Middle.rotation = _hand.Fingers[2].Bone(Bone.BoneType.TYPE_DISTAL).Rotation.ToQuaternion();
    }
    if(Ring != null) {
      Ring.position = _hand.Fingers[3].Bone(Bone.BoneType.TYPE_DISTAL).NextJoint.ToVector3();
      Ring.rotation = _hand.Fingers[3].Bone(Bone.BoneType.TYPE_DISTAL).Rotation.ToQuaternion();
    }
    if(Pinky != null) {
      Pinky.position = _hand.Fingers[4].Bone(Bone.BoneType.TYPE_DISTAL).NextJoint.ToVector3();
      Pinky.rotation = _hand.Fingers[4].Bone(Bone.BoneType.TYPE_DISTAL).Rotation.ToQuaternion();
    }
  }
}
