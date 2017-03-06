using Leap.Unity.RuntimeGizmos;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Leap.Unity;

namespace Leap.Paint.Controllers {

  public class PinchBrushController : MonoBehaviour {

    public Brush brush;
    public Chirality whichHand;

    void Update() {
      Hand hand = Hands.Get(whichHand);
      if (hand != null) {
        brush.transform.position = hand.GetPinchPosition();

        if (hand.IsPinching() && !brush.IsBrushing()) {
          brush.Begin();
        }

        if (!hand.IsPinching() && brush.IsBrushing()) {
          brush.End();
        }
      }
    }

  }

}