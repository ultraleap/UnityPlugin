using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Leap.Unity.Gestures {

  [CustomEditor(typeof(TwoHandedGesture), editorForChildClasses: true)]
  public class TwoHandedGestureEditor : CustomEditorBase<TwoHandedGesture> {

    private EnumEventTableEditor _tableEditor;

    protected override void OnEnable() {
      base.OnEnable();

      deferProperty("_eventTable");
      specifyCustomDrawer("_eventTable", drawEventTable);
    }

    private void drawEventTable(SerializedProperty property) {
      if (_tableEditor == null) {
        _tableEditor = new EnumEventTableEditor(property, typeof(TwoHandedGesture.EventType));
      }

      _tableEditor.DoGuiLayout();
    }

  }

}