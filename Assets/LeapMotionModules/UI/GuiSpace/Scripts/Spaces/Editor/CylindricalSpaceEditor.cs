using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Leap.Unity.Gui.Space {

  [CustomEditor(typeof(CylindricalSpace))]
  public class CylindricalSpaceEditor : CustomEditorBase {

    protected override void OnEnable() {
      base.OnEnable();

      SerializedProperty type = serializedObject.FindProperty("_type");
      Func<bool> isUsingAngularType = () => type.intValue == (int)CylindricalSpace.CylindricalType.Angular;
      specifyConditionalDrawing(isUsingAngularType, "_radiusOfConstantWidth");
    }
  }
}
