using UnityEngine;
using System.Collections;

namespace Leap {
  public abstract class HandFactory : MonoBehaviour {
    /// <summary>
    /// Creates a hand representation object that can receive updates from LeapHandController
    /// </summary>
    /// <param name="hand">The hand for which a repersentation is to be generaetd</param>
    /// <returns>A hand representation for the given hand</returns>
    
    public abstract HandRepresentation MakeHandRepresentation(Leap.Hand hand, ModelType modelType);
  }
}
