/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2017.                                 *
 * Leap Motion proprietary and  confidential.                                 *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

using Leap.Unity.UI.Interaction.Internal;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.UI.Interaction {

  public class KinematicGraspedMovement : IGraspedMovementController {

    public void MoveTo(Vector3 solvedPosition, Quaternion solvedRotation,
                       InteractionBehaviour interactionObj, bool justGrasped) {
      //interactionObj.rigidbody.MovePosition(solvedPosition);
      //interactionObj.rigidbody.MoveRotation(solvedRotation);
      interactionObj.rigidbody.transform.position = solvedPosition;
      interactionObj.rigidbody.transform.rotation = solvedRotation;
      interactionObj.rigidbody.position = solvedPosition;
      interactionObj.rigidbody.rotation = solvedRotation;
    }

  }

}
