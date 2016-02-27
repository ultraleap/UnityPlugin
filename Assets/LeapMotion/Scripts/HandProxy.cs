using UnityEngine;
using System.Collections;

namespace Leap {
  public class HandProxy:
    HandRepresentation
  {
    HandPool parent;
    public IHandModel handModel;

  
    public HandProxy(HandPool parent, IHandModel handModel, Leap.Hand hand) :
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

    public override void UpdateRepresentation(Leap.Hand hand, ModelType modelType){
      handModel.SetLeapHand(hand);
      handModel.UpdateHand();
    }
  }
}
