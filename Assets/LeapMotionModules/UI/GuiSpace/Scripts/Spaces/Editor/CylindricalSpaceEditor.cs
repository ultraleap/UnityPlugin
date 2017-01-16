using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Leap.Unity.Gui.Space {

  [CustomEditor(typeof(CylindricalSpace))]
  public class CylindricalSpaceEditor : CustomEditorBase {

    private CylindricalSpace _space;

    protected override void OnEnable() {
      base.OnEnable();

      _space = target as CylindricalSpace;

      SerializedProperty type = serializedObject.FindProperty("_type");
      Func<bool> isUsingAngularType = () => type.intValue == (int)CylindricalSpace.CylindricalType.Angular;
      specifyConditionalDrawing(isUsingAngularType, "_radiusOfConstantWidth");
    }

    void OnSceneGUI() {
      if (Tools.current != Tool.None && Tools.current != Tool.View) {
        return;
      }

      Undo.RecordObject(_space, "Move Center");
      _space.worldCenter = Handles.Slider2D(_space.worldCenter,
                                            Vector3.up,
                                            Vector3.right,
                                            Vector3.forward,
                                            0.1f * HandleUtility.GetHandleSize(_space.worldCenter),
                                            Handles.DotCap,
                                            snap: 0.05f,
                                            drawHelper: false);
    }
  }
}
