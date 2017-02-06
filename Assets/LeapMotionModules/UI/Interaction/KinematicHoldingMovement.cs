using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.UI.Interaction {

  public class KinematicHoldingMovement : IHoldingMovementController {

    public void MoveTo(Vector3 solvedPosition, Quaternion solvedRotation,
                       InteractionBehaviour interactionObj) {
      interactionObj.Rigidbody.MovePosition(solvedPosition);
      interactionObj.Rigidbody.MoveRotation(solvedRotation);
    }

  }

}
