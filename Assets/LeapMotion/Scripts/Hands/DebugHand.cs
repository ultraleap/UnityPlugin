/******************************************************************************\
* Copyright (C) Leap Motion, Inc. 2011-2016.                                   *
* Leap Motion proprietary. Licensed under Apache 2.0                           *
* Available at http://www.apache.org/licenses/LICENSE-2.0.html                 *
\******************************************************************************/

using UnityEngine;
using System.Collections;
using Leap;

namespace Leap.Unity{
  /**
   * A HandModel that draws lines for the bones in the hand and its fingers.
   * 
   * The debugs lines are only drawn in the Editor Scene view (when a hand is tracked) and
   * not in the Game view. Use debug hands when you aren't using visible hands in a scene
   * so that you can see where the hands are in the scene view.
   * */
  public class DebugHand : HandModel {
    public override ModelType HandModelType {
      get {
        return ModelType.Graphics;
      }
    }
  
    /**
    * Initializes the hand and calls the line drawing function.
    */
    public override void InitHand() {
      for (int f = 0; f < fingers.Length; ++f) {
        if (fingers[f] != null)
          fingers[f].InitFinger();
      }
  
      DrawDebugLines();
    }
  
    /**
    * Updates the hand and calls the line drawing function.
    */
    public override void UpdateHand() {
      for (int f = 0; f < fingers.Length; ++f) {
        if (fingers[f] != null)
          fingers[f].UpdateFinger();
      }
  
      DrawDebugLines();
    }
  
    /**
    * Draws lines from elbow to wrist, wrist to palm, and normal to the palm.
    */
    protected void DrawDebugLines() {
      HandModel handModel = GetComponent<HandModel>();
      Hand hand = handModel.GetLeapHand();
      Debug.DrawLine(hand.Arm.ElbowPosition.ToVector3(), hand.Arm.WristPosition.ToVector3(), Color.red); //Arm
      Debug.DrawLine(hand.WristPosition.ToVector3(), hand.PalmPosition.ToVector3(), Color.white); //Wrist to palm line
      Debug.DrawLine(hand.PalmPosition.ToVector3(), (hand.PalmPosition + hand.PalmNormal * hand.PalmWidth/2).ToVector3(), Color.black); //Hand Normal

      DrawBasis(hand.PalmPosition, hand.Basis, hand.PalmWidth/4 ); //Hand basis
      DrawBasis(hand.Arm.Basis.origin, hand.Arm.Basis, 10); //Arm basis
    }

    public static void DrawBasis(Vector position, Matrix basis, float scale){
      Vector3 origin = position.ToVector3();
      Debug.DrawLine(origin, origin + basis.xBasis.ToVector3() * scale, Color.red);
      Debug.DrawLine(origin, origin + basis.yBasis.ToVector3() * scale, Color.green);
      Debug.DrawLine(origin, origin + basis.zBasis.ToVector3() * scale, Color.blue);
    }

  }
}
