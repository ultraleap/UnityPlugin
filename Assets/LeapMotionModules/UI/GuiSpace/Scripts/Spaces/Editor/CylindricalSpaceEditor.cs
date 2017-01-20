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

    protected override void OnEnable() {
      base.OnEnable();

      _space = target as CylindricalSpace;
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
