using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.UI.Interaction {

  public interface IHoldMovementBehaviour {

    /// <summary>
    /// Called by an InteractionBehaviour when it calculates a target position
    /// and rotation; this method should attempt to move the InteractionBehaviourBase
    /// to those positions.
    /// </summary>
    void MoveTo(Vector3 solvedPosition, Quaternion solvedRotation,
                InteractionBehaviour interactionObj,
                ReadonlyList<Hand> graspingHands);

  }

}