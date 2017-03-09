using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.UI.Interaction {

  /// <summary>
  /// IGraspedMovementControllers take in a target position and rotation (a "pose")
  /// from an IGraspedPoseController and are responsible for attempting to move the
  /// InteractionBehaviourBase to match the target pose.
  /// </summary>
  public interface IGraspedMovementController {

    /// <summary>
    /// Called by an InteractionBehaviourBase when its IGraspedPoseController
    /// has determined a target position and rotation (or "pose"); this method should attempt
    /// to move the InteractionBehaviourBase to match that pose.
    /// </summary>
    void MoveTo(Vector3 solvedPosition, Quaternion solvedRotation,
                InteractionBehaviourBase interactionObj);

  }

}