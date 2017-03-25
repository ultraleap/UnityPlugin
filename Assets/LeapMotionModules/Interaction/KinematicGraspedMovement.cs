using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.UI.Interaction {

  public class KinematicGraspedMovement : IGraspedMovementController {

    public void MoveTo(Vector3 solvedPosition, Quaternion solvedRotation,
                       InteractionBehaviourBase interactionObj) {
      //interactionObj.Rigidbody.MovePosition(solvedPosition);
      //interactionObj.Rigidbody.MoveRotation(solvedRotation);
      interactionObj.Rigidbody.transform.position = solvedPosition;
      interactionObj.Rigidbody.transform.rotation = solvedRotation;
      interactionObj.Rigidbody.position = solvedPosition;
      interactionObj.Rigidbody.rotation = solvedRotation;
    }

  }

}
