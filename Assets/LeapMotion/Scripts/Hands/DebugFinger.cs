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
  * The finger model for our debugging. Draws debug lines for each bone.
  */
  public class DebugFinger : FingerModel {
  
    /** The colors used for each bone. */
    protected Color[] colors = {Color.gray, Color.yellow, Color.cyan, Color.magenta};
  
    /** Updates the finger and calls the line drawing function. */
    public override void UpdateFinger() {
      DrawDebugLines();
    }
  
    /**
    * Draws a line from joint to joint.
    */
    protected void DrawDebugLines() {
      Finger finger = this.GetLeapFinger();

      for (int i = 0; i < 4; ++i){
        Bone bone = finger.Bone((Bone.BoneType)i);
        Debug.DrawLine(bone.PrevJoint.ToVector3(), bone.PrevJoint.ToVector3() + bone.Direction.ToVector3() * bone.Length, colors[i]);
        DebugHand.DrawBasis(bone.PrevJoint, bone.Basis, 10);
      }
    }
  }
}
