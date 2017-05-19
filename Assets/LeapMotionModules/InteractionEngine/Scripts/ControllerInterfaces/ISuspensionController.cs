/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2017.                                 *
 * Leap Motion proprietary and  confidential.                                 *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

namespace Leap.Unity.Interaction {

  /**
  * ISuspensionController defines the interface used by the Interaction Engine
  * when the hand holding an interactable object ceases to be tracked and when
  * it starts to be tracked again.
  *
  * Tracking of a hand can be lost if it passes out of the Leap device's field of view
  * or is otherwise blocked from view.
  *
  * The Interaction Engine provides the SuspensionControllerDefault
  * implementation for this controller.
  * @since 4.1.4
  */
  public abstract class ISuspensionController : IControllerBase {

    /**
    * The length of time in seconds that an object can stay in suspension
    * until a timeout is triggered.
    * @since 4.1.4
    */
    public abstract float MaxSuspensionTime { get; }
    /**
    * Put the object into a suspended state.
    * @since 4.1.4
    */
    public abstract void Suspend();
    /**
    * Put the object into a normal, interacting state.
    * @since 4.1.4
    */
    public abstract void Resume();
    /**
    * End the suspended state without returning to an interacting state.
    * @since 4.1.4
    */
    public abstract void Timeout();
  }
}
