using UnityEngine;
using System.Collections;

namespace Leap {
  public class HandProxy:
    HandRepresentation
  {
    HandPool parent;
    HandTransitionBehavior handFinishBehavior;

  
    public HandProxy(HandPool parent, IHandModel handModel, Leap.IHand hand) :
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


      handFinishBehavior = handModel.GetComponent<HandTransitionBehavior>();
      if (handFinishBehavior) {
        handFinishBehavior.Reset();
      }
    }
    /** To be called if the HandRepresentation no longer has a Leap Hand. */
    public override void Finish() {
      if (handFinishBehavior) {
        handFinishBehavior.HandFinish();
      }
      parent.ModelPool.Add(handModel);
      handModel = null;
    }

    public override void UpdateRepresentation(Leap.IHand hand, ModelType modelType){
      handModel.SetLeapHand(hand);
      handModel.UpdateHand();
    }
  }
}
