using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.UI.Interaction {

  /// <summary>
  /// IGraspedHoldBehaviour specifies where an object grasped by a hand (or
  /// multiple hands) should move as the grasping hand(s) move. The most common
  /// implementation is the KabschHoldBehaviour, which produces a physically
  /// intuitive following motion for the object no matter how the hand moves.
  /// 
  /// IGraspedHoldBehaviours do not actually move the object; they only calculate
  /// where the object should be moved. Actually moving the object is the concern
  /// of an IGraspedMovementBehavior.
  /// </summary>
  public interface IGraspedHoldBehaviour {

    /// <summary>
    /// Called when a new InteractionHand begins grasping a certain object.
    /// The hand should be included in the grasping position calculation.
    /// </summary>
    void AddHand(InteractionManager.InteractionHand hand);

    /// <summary>
    /// Called when an InteractionHand stops grasping a certain object;
    /// the hand should no longer be included in the grasping position calculation.
    /// </summary>
    void RemoveHand(InteractionManager.InteractionHand hand);

    /// <summary>
    /// Calculate the best holding position and orientation given the current
    /// state of added InteractionHands (added via AddHand()).
    /// </summary>
    void GetHoldingPose(out Vector3 position, out Quaternion rotation);

  }

}