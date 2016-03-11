using UnityEngine;
using System;
using System.Collections;
using Leap;

namespace Leap.Unity {
  public abstract class HandRepresentation
  {
    public int HandID { get; private set; }
    public int LastUpdatedTime { get; set; }
    public bool IsMarked { get; set; }


    public HandRepresentation(int handID) {
      HandID = handID;
    }

    /**
    * Notifies the representation that a hand information update is available
    * @param hand The current Leap.Hand.
    * @param modelType Filters for a type of hand model, for example, physics or graphics hands.
    */
    public abstract void UpdateRepresentation(Hand hand, ModelType modelType);

    /**
    * Called when a hand representation is no longer needed
    */
    public abstract void Finish();
  }
}
