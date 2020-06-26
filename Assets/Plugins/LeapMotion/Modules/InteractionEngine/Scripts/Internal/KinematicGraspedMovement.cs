/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2020.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.Interaction {

  /// <summary>
  /// This implementation of IGraspedMovementHandler moves its interaction object to a
  /// target position and rotation by setting its rigidbody position and rotation
  /// directly to that target position and rotation. This is required when working with
  /// kinematic rigidbodies, which do not move based on their velocity and angular
  /// velocity.
  /// </summary>
  public class KinematicGraspedMovement : IGraspedMovementHandler {

    public void MoveTo(Vector3 solvedPosition, Quaternion solvedRotation,
                       InteractionBehaviour interactionObj, bool justGrasped) {
      interactionObj.rigidbody.MovePosition(solvedPosition);
      interactionObj.rigidbody.MoveRotation(solvedRotation);

      // Store the target position and rotation to prevent slippage in SwapGrasp
      // scenarios.
      interactionObj.latestScheduledGraspPose = new Pose(solvedPosition, solvedRotation);
    }
  }
}
