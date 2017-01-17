using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Leap.Unity.Query;

namespace Leap.Unity.Gui.Space {

  [CustomEditor(typeof(CylindricalSpace))]
  public class CylindricalSpaceEditor : CustomEditorBase {

    private CylindricalSpace _space;
    private SerializedProperty _typeProperty;

    protected override void OnEnable() {
      base.OnEnable();

      _space = target as CylindricalSpace;

      _typeProperty = serializedObject.FindProperty("_type");
      Func<bool> isUsingAngularType = () => _typeProperty.intValue == (int)CylindricalSpace.CylindricalType.Angular;
      specifyConditionalDrawing(isUsingAngularType, "_offsetOfConstantWidth");
    }

    public override void OnInspectorGUI() {
      base.OnInspectorGUI();

      if (_modifiedProperties.Query().Any(p => SerializedProperty.EqualContents(p, _typeProperty))) {
        _space.UpdateSpace();
      }
    }

    void OnSceneGUI() {
      if (Tools.current != Tool.None && Tools.current != Tool.View) {
        return;
      }

      EditorUtility.SetDirty(_space);
      Undo.RecordObject(_space, "Move Center");
      _space.worldCenter = Handles.Slider2D(_space.worldCenter,
                                            Vector3.up,
                                            Vector3.right,
                                            Vector3.forward,
                                            0.1f * HandleUtility.GetHandleSize(_space.worldCenter),
                                            Handles.DotCap,
                                            snap: 0.05f,
                                            drawHelper: false);

      serializedObject.Update();
    }
  }
}
