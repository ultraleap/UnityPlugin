using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.UI.Interaction {

  public class InteractionBehaviour : InteractionBehaviourBase {

    /// <summary> The RigidbodyWarper manipulates the graphical (but not physical) position
    /// of grasped objects based on the movement of the Leap hand so they appear move with less latency. </summary>
    [HideInInspector]
    public Leap.Unity.Interaction.RigidbodyWarper rigidbodyWarper;


    public override float GetHoverScore(Hand hand) {
      throw new System.NotImplementedException();
    }

    public override void OnHoverBegin(Hand hand) {
      throw new System.NotImplementedException();
    }

    public override void OnHoverStay(Hand hand) {
      throw new System.NotImplementedException();
    }

    public override void OnHoverEnd(Hand hand) {
      throw new System.NotImplementedException();
    }

    public override void OnPrimaryHoverBegin(Hand hand) {
      throw new System.NotImplementedException();
    }

    public override void OnPrimaryHoverStay(Hand hand) {
      throw new System.NotImplementedException();
    }

    public override void OnPrimaryHoverEnd(Hand hand) {
      throw new System.NotImplementedException();
    }

    public override void OnTouchBegin(Hand hand) {
      throw new System.NotImplementedException();
    }

    public override void OnTouchStay(Hand hand) {
      throw new System.NotImplementedException();
    }

    public override void OnTouchEnd(Hand hand) {
      throw new System.NotImplementedException();
    }

    public override bool IsBeingGrabbedBy(Hand hand) {
      throw new System.NotImplementedException();
    }

    public override void OnGraspBegin(Hand hand) {
      throw new System.NotImplementedException();
    }

    public override void OnGraspHold(Hand hand) {
      throw new System.NotImplementedException();
    }

    public override void OnGraspRelease(Hand hand) {
      throw new System.NotImplementedException();
    }

    public override void OnSuspend(Hand hand) {
      throw new System.NotImplementedException();
    }

    public override void OnResume(Hand hand) {
      throw new System.NotImplementedException();
    }
  }

}
