using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Leap;

namespace Leap.Unity {
  /**
   * HandProxy is a container class that facillitates the IHandModel lifecycle
   * @param parent The HandPool which creates HandProxies
   * @param handModel the IHandModel to be paired with Leap Hand data.
   * @param hand The Leap Hand data to paired with an IHandModel
   */ 
  public class HandProxy {
    HandPool parent;
    public int HandID { get; private set; }
    public int LastUpdatedTime { get; set; }
    public bool IsMarked { get; set; }
    public Chirality ProxChirality { get; protected set; }
    public ModelType ProxType { get; protected set; }
    public Hand MostRecentHand { get; protected set; }
    public Hand PostProcessHand { get; set; }
    public HandPool.ModelGroup Group { get; set; }
    public List<IHandModel> handModels;

    public HandProxy(HandPool parent, Hand hand, Chirality proxChirality, ModelType proxType) {
      this.parent = parent;
      HandID = hand.Id;
      this.ProxChirality = proxChirality;
      this.ProxType = proxType;
      this.MostRecentHand = hand;
      this.PostProcessHand = new Hand();
    }

    /** To be called if the HandProxy no longer has a Leap Hand. */
    public void Finish() {
      if (handModels != null) {
        for (int i = 0; i < handModels.Count; i++) {
          handModels[i].FinishHand();
          parent.ReturnToPool(handModels[i]);
          handModels[i] = null;
        }
      }
      parent.RemoveHandProxy(this);
    }

    public void AddModel(IHandModel model) {
      if (handModels == null) {
        handModels = new List<IHandModel>();
      }
      handModels.Add(model);
      if (model.GetLeapHand() == null) {
        model.SetLeapHand(MostRecentHand);
        model.InitHand();
        model.BeginHand();
        model.UpdateHand();
      }
      else {
        model.SetLeapHand(MostRecentHand);
        model.BeginHand();

      }
    }

    public void RemoveModel(IHandModel model) {
      if (handModels != null) {
        model.FinishHand();
        handModels.Remove(model);
      }
    }

    /** Calls Updates in IHandModels that are part of this HandProxy */
    public void UpdateProxy(Hand hand)
    {
      MostRecentHand = hand;
      if (handModels != null) {
        for (int i = 0; i < handModels.Count; i++) {
          handModels[i].SetLeapHand(hand);
          handModels[i].UpdateHand();
        }
      }
    }
  }
}
