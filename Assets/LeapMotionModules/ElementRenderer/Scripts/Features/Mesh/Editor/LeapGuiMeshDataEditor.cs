using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Leap.Unity;

[CustomEditor(typeof(LeapGuiMeshData))]
public class LeapGuiMeshDataEditor : CustomEditorBase<LeapGuiMeshData> {

  private bool _isAnyProcedural;

  protected override void OnEnable() {
    base.OnEnable();
    dontShowScriptField();

    specifyCustomDrawer("_mesh", disableWhenProcedural);
    specifyCustomDrawer("_remappableChannels", disableWhenProcedural);
  }

  public override void OnInspectorGUI() {
    _isAnyProcedural = false;
    foreach (var meshData in targets) {
      meshData.RefreshMeshData();
      if (meshData.isUsingProcedural) {
        _isAnyProcedural = true;
        break;
      }
    }

    base.OnInspectorGUI();
  }

  private void disableWhenProcedural(SerializedProperty property) {
    EditorGUI.BeginDisabledGroup(_isAnyProcedural);
    EditorGUILayout.PropertyField(property);
    EditorGUI.EndDisabledGroup();
  }
}
