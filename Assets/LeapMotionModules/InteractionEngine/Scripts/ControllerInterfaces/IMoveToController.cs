/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2017.                                 *
 * Leap Motion proprietary and  confidential.                                 *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

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
    * If info.shouldTeleport is true, this function must move the object using a teleport-style
    * movement, ie.e by changing the transform.position and transform.rotation, or the
    * RigidBody.position and RigidBody.rotation properties directly. InteractionBehaviour.NotifyTeleport() does
    * not be called in MoveTo().
    *
    * If info.shouldTeleport is false, this function must move the object using linear and angular
    * forces and velocities.
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
    *
    * You should only change the following properties in this method:
    * 
    * * InteractionBehavior.IsKinematic
    * * InteractionBehavior.UseGravity
    * * RigidBody.drag
    * * RigidBody.angularDrag
    * 
    * These properties are automatically restored when the object leaves the grabbed state.
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
