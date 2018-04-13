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
using UnityEditor;
using UnityEngine;

namespace Leap.Unity.Examples {

  [CustomEditor(typeof(TransformHandle), true)]
  [CanEditMultipleObjects]
  public class TransformHandleEditor : CustomEditorBase<TransformHandle> {

    protected override void OnEnable() {
      base.OnEnable();

      deferProperty("OnHandleDeactivated");
      deferProperty("OnHandleActivated");
      deferProperty("OnShouldHideHandle");
      deferProperty("OnShouldShowHandle");
    }

  }

}
