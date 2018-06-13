using UnityEditor;
using UnityEngine;

namespace Leap.Unity.Animation {
  
  public abstract class SwitchEditorBase<T> : CustomEditorBase<T>
                                              where T : UnityEngine.Object,
                                                        IPropertySwitch {

    protected override void OnEnable() {
      base.OnEnable();
    }

    public override void OnInspectorGUI() {
      drawSwitchNowButtons();
      drawSwitchButtons();

      base.OnInspectorGUI();
    }

    protected void drawSwitchNowButtons() {
      EditorGUILayout.BeginHorizontal();

      if (GUILayout.Button(new GUIContent("Switch On Now",
                                          "Calls OnNow() on the selected switch(es)."))) {
        
        // Support undo history.
        Undo.IncrementCurrentGroup();
        var curGroupIdx = Undo.GetCurrentGroup();

        // Note: It is the responsibility of the IPropertySwitch implementation
        // to perform operations that correctly report their actions in OnNow() to the
        // Undo history!
        foreach (var target in targets) {
          target.OnNow();
        }

        Undo.CollapseUndoOperations(curGroupIdx);
        Undo.SetCurrentGroupName("Switch Object(s) On Now");

      }

      if (GUILayout.Button(new GUIContent("Switch Off Now",
                                          "Calls OffNow() on the selected switch(es)."))) {

        // Support undo history.
        Undo.IncrementCurrentGroup();
        var curGroupIdx = Undo.GetCurrentGroup();

        // Note: It is the responsibility of the IPropertySwitch implementation
        // to perform operations in OffNow() that correctly report their actions to the
        // Undo history!
        foreach (var target in targets) {
          target.OffNow();
        }

        Undo.CollapseUndoOperations(curGroupIdx);
        Undo.SetCurrentGroupName("Switch Object(s) Off Now");

      }

      EditorGUILayout.EndHorizontal();
    }
    
    protected void drawSwitchButtons() {
      // Calling On() and Off() via the editor is currently only allowed during play mode.
      EditorGUI.BeginDisabledGroup(!Application.isPlaying);
      
      EditorGUILayout.BeginHorizontal();

      if (GUILayout.Button(new GUIContent("Switch On",
                                          "Calls On() on the selected switch(es)."
                                        + "This can only be used during play mode."))) {
        foreach (var target in targets) {
          target.On();
        }
      }

      if (GUILayout.Button(new GUIContent("Switch Off",
                                          "Calls Off() on the selected switch(es)."
                                        + "This can only be used during play mode."))) {
        foreach (var target in targets) {
          target.Off();
        }
      }

      EditorGUILayout.EndHorizontal();

      EditorGUI.EndDisabledGroup();
    }

  }

}
