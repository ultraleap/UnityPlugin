using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.UI.Interaction {

  public class KinematicHoldMovementBehaviour : IHoldMovementBehaviour {

    public void MoveTo(Vector3 solvedPosition, Quaternion solvedRotation,
                       InteractionBehaviour interactionObj,
                       ReadonlyList<Hand> graspingHands) {
      interactionObj.Rigidbody.position = solvedPosition;
      interactionObj.Rigidbody.rotation = solvedRotation;
    }

  }

}
