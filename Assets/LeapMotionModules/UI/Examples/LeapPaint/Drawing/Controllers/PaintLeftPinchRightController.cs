using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Leap;
using Leap.Unity;
using Leap.Paint;

namespace Leap.Paint.Controllers {

  public class PaintLeftPinchRightController : MonoBehaviour {

    public Brush brush;
    public Chirality pinchControlHand;
    public Chirality indexBrushHand;

    void Update() {
      Hand brushHand = Hands.Get(indexBrushHand);
      if (brushHand != null) {
        Hand controlHand = Hands.Get(pinchControlHand);
        if (controlHand != null) {
          brush.transform.position = brushHand.Index().TipPosition.ToVector3();

          if (controlHand.IsPinching() && !brush.IsBrushing()) {
            brush.Begin();
          }
          if (!controlHand.IsPinching() && brush.IsBrushing()) {
            brush.End();
          }
        }
      }
    }

  }


}