using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

using Leap;

namespace Leap.Unity {
  public abstract class HandRepresentation
  {
    public int HandID { get; private set; }
    public int LastUpdatedTime { get; set; }
    public bool IsMarked { get; set; }
    public Chirality RepChirality { get; protected set;}
    public ModelType RepType { get; protected set;}
    public Hand MostRecentHand { get; protected set; }

    public HandRepresentation(int handID, Hand hand, Chirality chirality, ModelType modelType) {
      HandID = handID;
      this.MostRecentHand = hand;
      this.RepChirality = chirality;
      this.RepType = modelType;

    }

    /**
    * Notifies the representation that a hand information update is available
    * @param hand The current Leap.Hand.
    * @param modelType Filters for a type of hand model, for example, physics or graphics hands.
    */
    public virtual void UpdateRepresentation(Hand hand) {
      MostRecentHand = hand;
    }

    /**
    * Called when a hand representation is no longer needed
    */
    public abstract void Finish();
    public abstract void AddModel(IHandModel model);
    public abstract void RemoveModel(IHandModel model);
  }
}
