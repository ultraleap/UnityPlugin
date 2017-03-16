using Leap;
using Leap.Unity;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FollowWrist : MonoBehaviour {

  public Chirality whichHand;

  void Update() {
    Hand hand = Hands.Get(whichHand);
    if (hand != null) {
      this.transform.position = (hand.WristPosition - hand.Arm.Direction * 0.04F).ToVector3();
      this.transform.rotation = hand.Arm.Rotation.ToQuaternion();
    }
  }

}
