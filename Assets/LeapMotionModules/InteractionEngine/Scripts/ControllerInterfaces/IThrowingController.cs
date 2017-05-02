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
  * IThrowingController defines the interface used by the Interaction Engine
  * when an interactable object is released by a hand.
  *
  * The Interaction Engine provides the ThrowingControllerPalmVelocity and
  * ThrowingControllerSlidingWindow implementations for this controller.
  * @since 4.1.4
  */
  public abstract class IThrowingController : IControllerBase {
    /**
    * Called every physics frame while an interactable object is being held.
    * @param hands a list of the hands holding the object. Note that currently, only one
    * hand will ever be holding an object simultaneously.
    * @since 4.1.4
    */
    public abstract void OnHold(ReadonlyList<Hand> hands);
    /**
    * Called when an interactable object is released by the last hand holding it.
    * @param throwingHand the releasing hand.
    * @since 4.1.4
    */
    public abstract void OnThrow(Hand throwingHand);
  }
}
