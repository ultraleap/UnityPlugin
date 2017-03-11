using Leap;
using Leap.Unity;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OnFistApplyAcceleration : MonoBehaviour {

  public Rigidbody target;

  void Update() {
    Hand rHand = Hands.Right;
    if (rHand != null) {
      if (rHand.FistStrength() > 0.8F) {
        target.AddForce(-Vector3.right);
      }
    }
  }

}
