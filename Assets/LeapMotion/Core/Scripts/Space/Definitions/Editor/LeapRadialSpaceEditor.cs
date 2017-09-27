/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2017.                                 *
 * Leap Motion proprietary and  confidential.                                 *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Leap.Unity.Space {

  [CustomEditor(typeof(LeapRadialSpace), editorForChildClasses: true)]
  public class LeapRadialSpaceEditor : CustomEditorBase<LeapRadialSpace> {

    protected override void OnEnable() {
      base.OnEnable();

      specifyCustomDrawer("_radius", drawRadius);
    }

    private void drawRadius(SerializedProperty property) {
      float curvature = 1.0f / property.floatValue;

      EditorGUI.BeginChangeCheck();
      curvature = EditorGUILayout.Slider("Curvature",
                                         curvature,
                                         -LeapRadialSpace.MAX_ABS_CURVATURE,
                                         LeapRadialSpace.MAX_ABS_CURVATURE);
      if (EditorGUI.EndChangeCheck()) {
        property.floatValue = LeapRadialSpace.GetRadiusFromCurvature(curvature);
      }

      EditorGUILayout.PropertyField(property);
    }
  }
}
