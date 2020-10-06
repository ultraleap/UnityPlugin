/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2020.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
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
