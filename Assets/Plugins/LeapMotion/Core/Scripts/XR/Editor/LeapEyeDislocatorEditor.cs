/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2020.                                   *
 * Ultraleap proprietary and confidential.                                    *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Leap.Unity {

  [CustomEditor(typeof(LeapEyeDislocator))]
  public class LeapEyeDislocatorEditor : CustomEditorBase<LeapEyeDislocator> {

    protected override void OnEnable() {
      base.OnEnable();

      specifyConditionalDrawing("_useCustomBaseline", "_customBaselineValue");
    }
  }
}
