using UnityEngine;
using System.Collections;
using Leap;

namespace Leap.Unity{

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
  * 
  * Use with AttachmentControllers and Detectors to activate and deactivate attachments in response to 
  * hand poses.
  *  @since 4.1.1
  */
  public class HandAttachments : IHandModel {
  
    /** The palm of the hand. */
    [Tooltip("The palm of the hand.")]
    public Transform Palm;
    /** The center of the forearm. */
    [Tooltip("The center of the forearm.")]
    public Transform Arm;
    /** The tip of the thumb. */
    [Tooltip("The tip of the thumb.")]
    public Transform Thumb;
    /** The point midway between the thumb and index finger tips.*/
    [Tooltip("The pont between the thumb and index finger.")]
    public Transform PinchPoint;
    /** The tip of the index finger. */
    [Tooltip("The tip of the index finger.")]
    public Transform Index;
    /** The tip of the middle finger. */
    [Tooltip("The tip of the middle finger.")]
    public Transform Middle;
    /** The tip of the ring finger. */
    [Tooltip("The tip of the ring finger.")]
    public Transform Ring;
    /** The tip of the pinky finger. */
    [Tooltip("The tip of the little finger.")]
    public Transform Pinky;
    /** The point midway between the finger tips. */
    [Tooltip("The point midway between the finger tips.")]
    public Transform GrabPoint;

    private Hand _hand;
  
    public override ModelType HandModelType {
      get {
        return ModelType.Graphics;
      }
    }
  
    [Tooltip("Whether to use this for right or left hands")]
    [SerializeField]
    private Chirality _handedness;

    /** 
     * Restrict this set of attachments to one hand or another. Set to
     * Either if the attachment object can be used for both hands.
     */
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
  
    /** Updates the position and rotation for each non-null attachment transform. */
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
      if (PinchPoint != null) {
        Vector thumbTip = _hand.Fingers[0].TipPosition;
        Vector indexTip = _hand.Fingers[1].TipPosition;
        Vector pinchPoint = Vector.Lerp(thumbTip, indexTip, 0.5f);
        PinchPoint.position = pinchPoint.ToVector3();

        Vector forward = pinchPoint - _hand.Fingers[1].Bone(Bone.BoneType.TYPE_PROXIMAL).PrevJoint;
        Vector up = _hand.Fingers[1].Bone(Bone.BoneType.TYPE_PROXIMAL).Direction.Cross(forward);
        PinchPoint.rotation = Quaternion.LookRotation(forward.ToVector3(), up.ToVector3());
      }
      if (GrabPoint != null) {
        Vector GrabCenter = Vector.Zero;
        for (int i = 0; i < _hand.Fingers.Count; i++) {
          GrabCenter += _hand.Fingers[i].TipPosition;
        }
        Vector GrabForward = (_hand.Fingers[2].TipPosition - _hand.WristPosition).Normalized;
        Vector thumbToPinky = _hand.Fingers[0].TipPosition - _hand.Fingers[4].TipPosition;
        Vector GrabNormal = GrabForward.Cross(thumbToPinky).Normalized;

        GrabCenter = GrabCenter / 5.0f;
        GrabPoint.position = GrabCenter.ToVector3();
        GrabPoint.rotation = Quaternion.LookRotation(GrabForward.ToVector3(), GrabNormal.ToVector3());
      }
    }
  }
}
