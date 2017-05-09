/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2017.                                 *
 * Leap Motion proprietary and  confidential.                                 *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

using Leap.Unity.Interaction.Internal;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.Interaction {

  /// <summary>
  /// IGraspedMovementControllers take in a target position and rotation (a "pose")
  /// from an IGraspedPoseController and are responsible for attempting to move the
  /// Interaction Behaviour to match the target pose.
  /// </summary>
  public interface IGraspedMovementController {

    /// <summary>
    /// Called by an Interaction Behaviour when its IGraspedPoseController
    /// has determined a target position and rotation (or "pose"); this method should attempt
    /// to move the InteractionBehaviourBase to match that pose.
    /// </summary>
    void MoveTo(Vector3 solvedPosition, Quaternion solvedRotation,
                InteractionBehaviour interactionObj, bool justGrasped);

  }

}
