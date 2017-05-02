/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2017.                                 *
 * Leap Motion proprietary and  confidential.                                 *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

using System.Collections.Generic;

namespace Leap.Unity {
  /**
   * HandRepresentation is a container class that facillitates the IHandModel lifecycle
   * @param parent The HandPool which creates HandRepresentations
   * @param handModel the IHandModel to be paired with Leap Hand data.
   * @param hand The Leap Hand data to paired with an IHandModel
   */
  public class HandRepresentation {
    HandPool parent;
    public int HandID { get; private set; }
    public int LastUpdatedTime { get; set; }
    public bool IsMarked { get; set; }
    public Chirality RepChirality { get; protected set; }
    public ModelType RepType { get; protected set; }
    public Hand MostRecentHand { get; protected set; }
    public Hand PostProcessHand { get; set; }
    public List<IHandModel> handModels;

    public HandRepresentation(HandPool parent, Hand hand, Chirality repChirality, ModelType repType) {
      this.parent = parent;
      HandID = hand.Id;
      this.RepChirality = repChirality;
      this.RepType = repType;
      this.MostRecentHand = hand;
      this.PostProcessHand = new Hand();
    }

    /** To be called if the HandRepresentation no longer has a Leap Hand. */
    public void Finish() {
      if (handModels != null) {
        for (int i = 0; i < handModels.Count; i++) {
          handModels[i].FinishHand();
          parent.ReturnToPool(handModels[i]);
          handModels[i] = null;
        }
      }
      parent.RemoveHandRepresentation(this);
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
      } else {
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

    /** Calls Updates in IHandModels that are part of this HandRepresentation */
    public void UpdateRepresentation(Hand hand) {
      MostRecentHand = hand;
      if (handModels != null) {
        for (int i = 0; i < handModels.Count; i++) {
          if (handModels[i].group != null && handModels[i].group.HandPostProcesses.GetPersistentEventCount() > 0) {
            PostProcessHand.CopyFrom(hand);
            handModels[i].group.HandPostProcesses.Invoke(PostProcessHand);
            handModels[i].SetLeapHand(PostProcessHand);
          } else {
            handModels[i].SetLeapHand(hand);
          }
          handModels[i].UpdateHand();
        }
      }
    }
  }
}
