/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2020.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
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
