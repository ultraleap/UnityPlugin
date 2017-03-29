using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.UI.Interaction {

  public class KinematicGraspedMovement : IGraspedMovementController {

    public void MoveTo(Vector3 solvedPosition, Quaternion solvedRotation,
                       InteractionBehaviourBase interactionObj) {
      //interactionObj.rigidbody.MovePosition(solvedPosition);
      //interactionObj.rigidbody.MoveRotation(solvedRotation);
      interactionObj.rigidbody.transform.position = solvedPosition;
      interactionObj.rigidbody.transform.rotation = solvedRotation;
      interactionObj.rigidbody.position = solvedPosition;
      interactionObj.rigidbody.rotation = solvedRotation;
    }

  }

}
