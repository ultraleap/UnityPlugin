using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.LeapPaint.LineDrawing {

  public class PinchBrushController : MonoBehaviour {

    public Brush3DBase brush;
    public Chirality whichHand;

    void Update() {
      Hand hand = Hands.Get(whichHand);
      if (hand != null) {
        brush.transform.position = hand.GetPinchPosition();

        if (hand.IsPinching() && !brush.isBrushing) {
          brush.BeginStroke();
        }

        if (!hand.IsPinching() && brush.isBrushing) {
          brush.EndStroke();
        }
      }
    }

  }

}