using UnityEngine;

namespace Leap.Unity.Interaction {

  /**
  * IMoveToController defines the interface used by the Interaction Engine
  * to move an object from its current position and orientation
  * to a target position and orientation.
  *
  * The Interaction Engine provides the MoveToControllerKinematic and MoveToControllerVelocity
  * implementations for this controller.
  * @since 4.1.4
  */
  public abstract class IMoveToController : IControllerBase {

    /**
    * Move the interactable object to or towards the Interaction Engine's desired position.
    * 
    * @param hands the list of Leap.Hand objects involved in the interaction.
    * @param info hints about the move.
    * @param solvedPosition the target position calculated by the Interaction Engine.
    * @param solvedRotation the target rotation calculated by the Interaction Engine.
    * @since 4.1.4
    */
    public abstract void MoveTo(ReadonlyList<Hand> hands, PhysicsMoveInfo info, Vector3 solvedPosition, Quaternion solvedRotation);
    /**
    * Set the object to a state appropriate for being held in a hand.
    * @since 4.1.4
    */
    public abstract void SetGraspedState();
    /**
    * Called when the object is grasped by a hand.
    * @since 4.1.4
    */
    public abstract void OnGraspBegin();
    /**
    * Called when the object is released by all hands.
    * @since 4.1.4
    */
    public abstract void OnGraspEnd();
  }

  /**
  * Provides hints for moving an interactable object that is being held.
  */
  public struct PhysicsMoveInfo {
    /** Whether you should move the object by changing its Transform.position property. */
    public bool shouldTeleport;
    /** The distance between the object's final position and its target position in the 
    previous Unity frame. */ 
    public float remainingDistanceLastFrame;
  }
}
