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

    public HandProxy(HandPool parent, Hand hand, Chirality repChirality, ModelType repType) :
      base(hand.Id, hand, repChirality, repType)
    {
      this.parent = parent;
      this.RepChirality = repChirality;
      this.RepType = repType;
      this.MostRecentHand = hand;
    }

    /** To be called if the HandRepresentation no longer has a Leap Hand. */
    public override void Finish() {
      if (handModels != null) {
        for (int i = 0; i < handModels.Count; i++) {
          handModels[i].FinishHand();
          Debug.Log("HandProxy.Finish(): " + parent);
          parent.ReturnToPool(handModels[i]);
          handModels[i] = null;
        }
      }
      parent.ActiveHandReps.Remove(this);
    }

    public override void AddModel(IHandModel model) {
      Debug.Log("Adding:" + model);
      if (handModels == null) {
        handModels = new List<IHandModel>();
      }
      handModels.Add(model);
      if (model.GetLeapHand() == null) {
        model.SetLeapHand(MostRecentHand);
        model.InitHand();
        model.UpdateHand();
      }
      else {
        model.SetLeapHand(MostRecentHand);
      }
      model.BeginHand();
      parent.ModelToHandProxyMapping.Add(model, this);
    }

    public override void RemoveModel(IHandModel model) {
      Debug.Log("RemoveModel");
      if (handModels != null) {
        model.FinishHand();
        handModels.Remove(model);
        parent.ReturnToPool(model);
      }
    }

    /** Calls Updates in IHandModels that are part of this HandRepresentation */
    public override void UpdateRepresentation(Hand hand)
    {
      base.UpdateRepresentation(hand);
      if (handModels != null) {
        for (int i = 0; i < handModels.Count; i++) {
          handModels[i].SetLeapHand(hand);
          handModels[i].UpdateHand();
        }
      }

      //this runs if handModels is null ?
    }
  }
}
