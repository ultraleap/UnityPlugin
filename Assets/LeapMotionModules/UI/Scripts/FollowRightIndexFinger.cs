using Leap;
using Leap.Unity;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FollowRightIndexFinger : MonoBehaviour {

  void Update() {
    Hand rHand = Hands.Get(Chirality.Right);
    if (rHand != null) {
      this.transform.position = rHand.Index().TipPosition.ToVector3();
    }
  }

}
