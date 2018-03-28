/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2018.                                 *
 * Leap Motion proprietary and confidential.                                  *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
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
