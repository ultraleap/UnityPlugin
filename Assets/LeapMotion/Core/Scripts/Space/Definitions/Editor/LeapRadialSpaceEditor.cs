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
