using Leap;
using Leap.Unity;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FollowLeftPalm : MonoBehaviour {

  void Update() {
    Hand lHand = Hands.Left;
    if (lHand != null) {
      this.transform.position = lHand.PalmPosition.ToVector3();
      this.transform.rotation = lHand.Rotation.ToQuaternion();
    }
  }

}
