/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2017.                                 *
 * Leap Motion proprietary and  confidential.                                 *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

using UnityEngine;
using Leap;
using Leap.Unity;
using Leap.Unity.RuntimeGizmos;

namespace Leap.Unity.Attachments {

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
  public class HandAttachments : IHandModel, IRuntimeGizmoComponent {
  
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
     * Whether to use this for right or left hands.
     * @since 4.1.1
     */
    public override Chirality Handedness {
      get {
        return _handedness;
      }
      set { }
    }

    /** Whether to draw lines to visualize the hand. */
    [Tooltip(" Whether to draw lines to visualize the hand.")]
    public bool DrawHand = false;

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
        var fingers = _hand.Fingers;
        Vector3 GrabCenter = _hand.WristPosition.ToVector3();
        Vector3 GrabForward = Vector3.zero;
        for (int i = 0; i < fingers.Count; i++) {
          Finger finger = fingers[i];
          GrabCenter += finger.TipPosition.ToVector3();
          if (i > 0) { //don't include thumb
            GrabForward += finger.TipPosition.ToVector3();
          }
        }
        GrabPoint.position = GrabCenter / 6.0f; //average between wrist and fingertips
        GrabForward = (GrabForward / 4 - _hand.WristPosition.ToVector3()).normalized;
        Vector3 thumbToPinky = fingers[0].TipPosition.ToVector3() - fingers[4].TipPosition.ToVector3();
        Vector3 GrabNormal = Vector3.Cross(GrabForward, thumbToPinky).normalized;
        GrabPoint.rotation = Quaternion.LookRotation(GrabForward, GrabNormal);
      }
    }

    public override bool SupportsEditorPersistence() { return true; }

    /** The colors used for each bone. */
    protected Color[] colors = { Color.gray, Color.yellow, Color.cyan, Color.magenta };

    /**
    * Draws lines from elbow to wrist, wrist to palm, and normal to the palm.
    * Also draws the orthogonal basis vectors for the pinch and grab points.
    */
    public void OnDrawRuntimeGizmos(RuntimeGizmoDrawer gizmoDrawer) {
      if (DrawHand) {
        Hand hand = GetLeapHand();
        gizmoDrawer.color = Color.red;
        gizmoDrawer.DrawLine(hand.Arm.ElbowPosition.ToVector3(), hand.Arm.WristPosition.ToVector3());
        gizmoDrawer.color = Color.white;
        gizmoDrawer.DrawLine(hand.WristPosition.ToVector3(), hand.PalmPosition.ToVector3()); //Wrist to palm line
        gizmoDrawer.color = Color.black;
        gizmoDrawer.DrawLine(hand.PalmPosition.ToVector3(), (hand.PalmPosition + hand.PalmNormal * hand.PalmWidth / 2).ToVector3()); //Hand Normal
        if (PinchPoint != null)
          DrawBasis(gizmoDrawer, PinchPoint.position, PinchPoint.GetLeapMatrix(), .01f); //Pinch basis
        if (GrabPoint != null)
          DrawBasis(gizmoDrawer, GrabPoint.position, GrabPoint.GetLeapMatrix(), .01f); //Grab basis

        for (int f = 0; f < 5; f++) { //Fingers
          Finger finger = hand.Fingers[f];
          for (int i = 0; i < 4; ++i) {
            Bone bone = finger.Bone((Bone.BoneType)i);
            gizmoDrawer.color = colors[i];
            gizmoDrawer.DrawLine(bone.PrevJoint.ToVector3(), bone.PrevJoint.ToVector3() + bone.Direction.ToVector3() * bone.Length);
          }
        }
      }
    }

    public void DrawBasis(RuntimeGizmoDrawer gizmoDrawer, Vector3 origin, LeapTransform basis, float scale) {
      gizmoDrawer.color = Color.red;
      gizmoDrawer.DrawLine(origin, origin + basis.xBasis.ToVector3() * scale);
      gizmoDrawer.color = Color.green;
      gizmoDrawer.DrawLine(origin, origin + basis.yBasis.ToVector3() * scale);
      gizmoDrawer.color = Color.blue;
      gizmoDrawer.DrawLine(origin, origin + basis.zBasis.ToVector3() * scale);
    }
  }
}
