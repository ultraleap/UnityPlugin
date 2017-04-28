using Leap.Unity.Interaction.Internal;
using UnityEngine;

namespace Leap.Unity.Interaction {

  /// <summary>
  /// Interaction Behaviours feed their throw controllers callbacks when they are
  /// held (for data collection) and when they are released so that the controller
  /// can manipulate the Interaction object's thrown direction and velocity to match
  /// the user's intention.
  /// </summary>
  public interface IThrowController {

    /// <summary>
    /// Called every FixedUpdate frame while an Interaction object is being held.
    /// </summary>
    /// <param name="intObj">The interaction object being held.</param>
    /// <param name="hands">A list of the hands currently holding the object.</param>
    void OnHold(InteractionBehaviour intObj, ReadonlyList<InteractionHand> hands);

    /// <summary>
    /// Called when an Interaction object is released by the last hands holding it.
    /// </summary>
    void OnThrow(InteractionBehaviour intObj, InteractionHand hand);

  }

}