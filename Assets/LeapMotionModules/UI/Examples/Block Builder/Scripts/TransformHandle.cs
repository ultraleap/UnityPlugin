using Leap;
using Leap.Unity;
using Leap.Unity.UI.Interaction;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TransformHandle : Hoverable {
  
  private Hand grabbingHand = null;
  private bool grabbed = false;

  void Update() {
    if (primaryHoveringHand != null) {
      if (IsGrabbedBy(primaryHoveringHand)) {
        OnGrabbed(primaryHoveringHand);
      }
    }

    if (grabbingHand != null) {

    }
  }

  private Vector3 _translateBasis;
  private Vector3 _handOffset;

  private void OnGrabbed(Hand hand) {
    if (grabbingHand != null) OnReleased(grabbingHand);

    grabbingHand = hand;
    _translateBasis = this.transform.position;
    _handOffset = this.transform.position - hand.PalmPosition.ToVector3();
  }

  private void OnHeld(Hand hand) {
    this.transform.position = hand.PalmPosition.ToVector3() + _handOffset;
    this.transform.position = _translateBasis + Vector3.Project(this.transform.position, this.transform.forward);
  }

  private void OnReleased(Hand hand) {
    grabbingHand = null;
  }

  private bool IsGrabbedBy(Hand hand) {
    float minGrabDistance = Mathf.Min(Vector3.Distance(hand.AttentionPosition(), this.transform.position), Vector3.Distance(hand.GetPinchPosition(), this.transform.position));
    return ((hand.PinchStrength > 0.75F || hand.GrabStrength > 0.75F) && minGrabDistance < 0.05F);
  }

}
