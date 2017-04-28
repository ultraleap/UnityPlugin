using Leap.Unity.Interaction.Internal;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.Interaction {

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
