using UnityEngine;
using UnityEditor;
using System.Collections;

namespace Leap.Unity {
  [CustomEditor(typeof(HandPool))]
  public class LeapHandPoolEditor : CustomEditorBase {

    protected override void OnEnable() {
      base.OnEnable();
      specifyCustomDrawer("ModelCollection", showModelCollection);
      specifyCustomDrawer("ModelPool", showModelPool);
    }

    private void showModelCollection(SerializedProperty list) {
      EditorGUILayout.PropertyField(list,true);
    }

    private void showModelPool(SerializedProperty modelPool) {
      if (Application.isPlaying) {
        using (new EditorGUI.DisabledGroupScope(true)) {
          EditorGUILayout.PropertyField(modelPool, true);
        }
      }
    }

  }
}
