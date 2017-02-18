using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.UI.Interaction {

  /// <summary>
  /// IHoldingPoseController specifies where an object grasped by a hand (or
  /// multiple hands) should move as the grasping hand(s) move. The most common
  /// implementation is the KabschHoldBehaviour, which produces a physically
  /// intuitive following motion for the object no matter how the hand moves.
  /// 
  /// IHoldingPoseControllers do not actually move the object; they only calculate
  /// where the object should be moved. Actually moving the object is the concern
  /// of an IHoldingMovementController.
  /// </summary>
  public interface IGraspedPositionController {

    /// <summary>
    /// Called when a new InteractionHand begins grasping a certain object.
    /// The hand should be included in the held pose calculation.
    /// </summary>
    void AddHand(InteractionHand hand);

    /// <summary>
    /// Called when an InteractionHand stops grasping a certain object;
    /// the hand should no longer be included in the held pose calculation.
    /// </summary>
    void RemoveHand(InteractionHand hand);

    /// <summary>
    /// Called if an InteractionBehaviour is set not to move while being grasped;
    /// this should clear any hands to be included in the grasping position calculation.
    /// </summary>
    void ClearHands();

    /// <summary>
    /// Calculate the best holding position and orientation given the current
    /// state of added InteractionHands (via AddHand()).
    /// </summary>
    void GetGraspedPosition(out Vector3 position, out Quaternion rotation);

  }

}