using UnityEngine;
using System.Collections;
using Leap;

namespace Leap.Unity {
  public abstract class HandFactory : MonoBehaviour {
    /// <summary>
    /// Creates a hand representation object that can receive updates from LeapHandController
    /// </summary>
    /// <param name="hand">The hand for which a representation is to be generated</param>
    /// <returns>A hand representation for the given hand</returns>
    
    public abstract HandRepresentation MakeHandRepresentation(Hand hand, ModelType modelType);
  }
}
