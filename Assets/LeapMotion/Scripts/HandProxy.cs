using UnityEngine;
using System.Collections;
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
    public IHandModel handModel;

    public HandProxy(HandPool parent, IHandModel handModel, Hand hand) :
      base(hand.Id)
    {
      this.parent = parent;
      this.handModel = handModel;

      // Check to see if the hand model has been initialized yet
      if (handModel.GetLeapHand() == null) {
        handModel.SetLeapHand(hand);
        handModel.InitHand();
      } else {
        handModel.SetLeapHand(hand);
      }
      handModel.BeginHand();
    }
    /** To be called if the HandRepresentation no longer has a Leap Hand. */
    public override void Finish() {
      handModel.FinishHand();
      parent.ModelPool.Add(handModel);
      handModel = null;
    }

    /** Calls Updates in IHandModels that are part of this HandRepresentation */
    public override void UpdateRepresentation(Hand hand, ModelType modelType){
      handModel.SetLeapHand(hand);
      handModel.UpdateHand();
    }
  }
}
