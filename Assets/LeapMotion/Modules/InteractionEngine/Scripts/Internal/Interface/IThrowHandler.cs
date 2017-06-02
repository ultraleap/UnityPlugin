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

  /// <summary>
  /// Interaction objects feed their throw handlers callbacks when they are held (for
  /// data collection) and then fire OnThrow when they are released from all grasping
  /// hands or controllers so that the throw handler can manipulate the interaction
  /// object's trajectory to better match the user's intention.
  /// </summary>
  public interface IThrowHandler {

    /// <summary>
    /// Called every FixedUpdate frame while an interaction object is being held.
    /// </summary>
    /// <param name="intObj">The interaction object being held.</param>
    /// <param name="hands">A list of the interaction controllers currently grasping
    /// the object.</param>
    void OnHold(InteractionBehaviour intObj,
               ReadonlyList<InteractionController> controllers);

    /// <summary>
    /// Called when an Interaction object is released by the last interaction controller
    /// holding it.
    /// </summary>
    void OnThrow(InteractionBehaviour intObj, InteractionController controller);

  }

}
