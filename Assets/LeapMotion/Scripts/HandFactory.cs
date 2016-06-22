using UnityEngine;
using System.Collections;
using Leap;

namespace Leap.Unity {
  public abstract class HandFactory : MonoBehaviour {

    /** Creates a hand representation object that can receive updates from LeapHandController
     * @param hand The hand for which a representation is to be generated.
     * @returns hand representation for the given hand
     */
     public abstract HandRepresentation MakeHandRepresentation(Hand hand, ModelType modelType);

     /** Attempts to draw another model from the Model pool for an existing HandRepresentation
      * @param handRep The Representation for the hand which doesn't have any models assigned to it
      * @returns Whether this representation was successfully assigned a model
      */
     public abstract bool AttemptToReassignHandModel(HandRepresentation handRep, ModelType modelType);

     /** Check if this HandRepresentation is using any models
      * @param hand The HandRepresentation for which to check
      * @returns Whether this HandRepresentation has any models attached to it
      */
     public abstract bool CheckModelUsage(HandRepresentation handRep);
  }
}
