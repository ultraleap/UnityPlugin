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

    /// <summary>
    /// Notifies the representation that a hand information update is available
    /// </summary>
    /// <param name="hand">The current Leap.Hand</param>
    public abstract void UpdateRepresentation(Hand hand, ModelType modelType);

    /// <summary>
    /// Called when a hand representation is no longer needed
    /// </summary>
    public abstract void Finish();
  }
}
