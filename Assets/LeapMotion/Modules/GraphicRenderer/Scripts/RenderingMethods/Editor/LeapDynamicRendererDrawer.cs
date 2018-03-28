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

namespace Leap.Unity.GraphicalRenderer {

  [CustomPropertyDrawer(typeof(LeapDynamicRenderer))]
  public class LeapDynamicRendererDrawer : LeapMesherBaseDrawer {

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
      base.OnGUI(position, property, label);

      //Nothing to do yet!
    }
  }
}
