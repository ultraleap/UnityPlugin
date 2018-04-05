/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2018.                                 *
 * Leap Motion proprietary and confidential.                                  *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

using UnityEngine;
using System.Collections;
using Leap;

namespace Leap.Unity {
  /**
* A deforming, very low poly count hand.
*
* All the graphics for this hand are drawn by the fingers. There is no representation
* for the palm or the arm.
*/
  public class PolyHand : HandModel {
    public override ModelType HandModelType {
      get {
        return ModelType.Graphics;
      }
    }
    public override bool SupportsEditorPersistence() {
      return true;
    }
    /** Initializes the hand and calls the finger initializers. */
    public override void InitHand() {
      SetPalmOrientation();

      for (int f = 0; f < fingers.Length; ++f) {
        if (fingers[f] != null) {
          fingers[f].fingerType = (Finger.FingerType)f;
          fingers[f].InitFinger();
        }
      }
    }

    /** Updates the hand and calls the finger update functions. */
    public override void UpdateHand() {
      SetPalmOrientation();

      for (int f = 0; f < fingers.Length; ++f) {
        if (fingers[f] != null) {
          fingers[f].UpdateFinger();
        }
      }
    }

    /** Sets the palm transform. */
    protected void SetPalmOrientation() {
      if (palm != null) {
        palm.position = GetPalmPosition();
        palm.rotation = GetPalmRotation();
      }
    }
  }
  
}
