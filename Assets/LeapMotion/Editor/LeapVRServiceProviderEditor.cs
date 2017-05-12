/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2017.                                 *
 * Leap Motion proprietary and  confidential.                                 *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

using UnityEditor;
using UnityEngine;

namespace Leap.Unity {

  [CustomEditor(typeof(LeapVRServiceProvider))]
  public class LeapVRServiceProviderEditor : LeapServiceProviderEditor {
    public override void OnSceneGUI() {
      arcDirection = Vector3.up;
      deviceRotation = Quaternion.Euler(90f, 0f, 0f);
      base.OnSceneGUI();
    }
  }
}
