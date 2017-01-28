using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.UI.Interaction {

  public abstract class Hoverable : MonoBehaviour {

    protected virtual void Start() {
      HoverManager manager = GetComponentInParent<HoverManager>();
      if (manager != null) {
        manager.Add(this);
      }
      else {
        Debug.LogWarning("Hoverable object not childed to a HoverManager: Will not receive automatic OnHover...() calls.");
      }
    }

    /// <summary> Values >= zero are "hovered." Of hovered objects, the one with the highest score is the "primary" hovered object. </summary>
    public virtual float GetHoverScore(Hand hand) {
      return Vector3.Distance(hand.AttentionPosition(), this.transform.position).Map(0F, 0.2F, 1F, 0F);
    }

    protected Hand hoveringLeftHand = null;
    protected Hand hoveringRightHand = null;
    protected int hoveringHandCount = 0;

    public virtual void OnHoverBegin(Hand hand) {
      if (hand.IsLeft) {
        hoveringLeftHand = hand;
      }
      else {
        hoveringRightHand = hand;
      }
      hoveringHandCount += 1;
    }

    public virtual void OnHoverStay(Hand hand) { }

    public virtual void OnHoverEnd(Hand hand) {
      if (hand.IsLeft) {
        hoveringLeftHand = null;
      }
      else {
        hoveringRightHand = null;
      }
      hoveringHandCount -= 1;
    }

    protected Hand primaryHoveringHand = null;

    public virtual void OnPrimaryHoverBegin(Hand hand) {
      primaryHoveringHand = hand;
    }

    public virtual void OnPrimaryHoverStay(Hand hand) { }

    public virtual void OnPrimaryHoverEnd(Hand hand) {
      primaryHoveringHand = null;
    }

  }

}