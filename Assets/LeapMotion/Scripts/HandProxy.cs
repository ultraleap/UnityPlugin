using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Leap;

namespace Leap.Unity {
  /**
   * HandProxy is a concrete example of HandRepresentation
   * @param parent The HandPool which creates HandRepresentations
   * @param handModel the IHandModel to be paired with Leap Hand data.
   * @param hand The Leap Hand data to paired with an IHandModel
   */ 
  public class HandProxy:
    HandRepresentation
  {
    HandPool parent;
    public List<IHandModel> handModels;

    public HandProxy(HandPool parent, List<IHandModel> handModels, Hand hand) :
      base(hand.Id)
    {
      for (int i = 0; i < handModels.Count; i++) {

        this.parent = parent;
        this.handModels = handModels;

        // Check to see if the hand model has been initialized yet
        if (handModels[i].GetLeapHand() == null) {
          handModels[i].SetLeapHand(hand);
          handModels[i].InitHand();
        }
        else {
          handModels[i].SetLeapHand(hand);
        }
        handModels[i].BeginHand();
      }
    }
    /** To be called if the HandRepresentation no longer has a Leap Hand. */
    public override void Finish() {
      if (handModels != null) {
        for (int i = 0; i < handModels.Count; i++) {
          handModels[i].FinishHand();
          parent.ReturnToPool(handModels[i]);
          handModels[i] = null;
        }
      }
    }

    /** Calls Updates in IHandModels that are part of this HandRepresentation */
    public override void UpdateRepresentation(Hand hand, ModelType modelType)
    {
      if (handModels != null) {
        for (int i = 0; i < handModels.Count; i++) {
          handModels[i].SetLeapHand(hand);
          handModels[i].UpdateHand();
        }
      }
    }
  }
}
