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

namespace Leap.Unity.UI.Interaction {

  /// <summary>
  /// IGraspedPoseController specifies where an object grasped by a hand (or
  /// multiple hands) should move as the grasping hand(s) move. The most common
  /// implementation is the KabschGraspedPose, which produces a physically
  /// intuitive following motion for the object no matter how the hand moves.
  /// 
  /// IGraspedPoseControllers do not actually move the object; they only calculate
  /// where the object should be moved. Actually moving the object is the concern
  /// of the IGraspedMovementController.
  /// </summary>
  public interface IGraspedPoseController {

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
    /// Called e.g. if the InteractionBehaviour is set not to move while being grasped;
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
