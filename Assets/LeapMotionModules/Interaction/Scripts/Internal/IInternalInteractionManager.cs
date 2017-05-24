using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.Interaction {

  /// <summary>
  /// Defines the interface functionality for internal methods used only by classes
  /// within the Interaction Engine. Generally speaking, developers should not need
  /// to call these methods, so encapsulating them in this interface makes that
  /// distinction clearer (because the methods still have to be public in order to be
  /// called from outside the InteractionManager class).
  /// </summary>
  public interface IInternalInteractionManager {



  }


}