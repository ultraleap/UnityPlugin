using UnityEditor;
using UnityEngine;

namespace Leap.Unity.Animation {
  
  [CustomEditor(typeof(ObjectSwitch), editorForChildClasses: true)]
  public class ObjectSwitchEditor : SwitchEditorBase<ObjectSwitch> {

    protected override void OnEnable() {
      base.OnEnable();
    }

    public override void OnInspectorGUI() {
      base.OnInspectorGUI();

      refreshSwitches();

      drawAttachedSwitches();
    }

    private void refreshSwitches() {
      foreach (var target in targets) {
        target.RefreshSwitches();
      }
    }

    private void drawAttachedSwitches() {
      EditorGUILayout.Space();

      EditorGUILayout.LabelField("Attached Switches",
                                 EditorStyles.boldLabel);

      EditorGUILayout.BeginVertical();

      foreach (var switchComponent in target.switches) {
        EditorGUILayout.LabelField(new GUIContent(switchComponent.GetType().Name));
      }

      EditorGUILayout.EndVertical();
    }

  }

}
