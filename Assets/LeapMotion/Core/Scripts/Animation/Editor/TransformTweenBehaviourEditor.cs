/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2017.                                 *
 * Leap Motion proprietary and  confidential.                                 *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

using Leap.Unity.Query;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Leap.Unity.Animation {

  [CanEditMultipleObjects]
  [CustomEditor(typeof(TransformTweenBehaviour))]
  public class TransformTweenBehaviourEditor : CustomEditorBase<TransformTweenBehaviour> {

    protected override void OnEnable() {
      base.OnEnable();

      dontShowScriptField();

      deferProperty("_eventTable");
      specifyCustomDrawer("_eventTable", drawEventTable);
    }

    private EnumEventTableEditor _tableEditor;
    private void drawEventTable(SerializedProperty property) {
      if (_tableEditor == null) {
        _tableEditor = new EnumEventTableEditor(property, typeof(TransformTweenBehaviour.EventType));
      }

      _tableEditor.DoGuiLayout();
    }

    public override void OnInspectorGUI() {

      drawScriptField();

      EditorGUI.BeginDisabledGroup(target.targetTransform == null
                                  || target.startTransform == null
                                  || PrefabUtility.GetPrefabType(target.gameObject) == PrefabType.Prefab);
      
      EditorGUILayout.BeginHorizontal();

      if (GUILayout.Button(new GUIContent("Set Target" + (targets.Length > 1 ? "s" : "") + " To Start",
                                          "If this TransformTweenBehaviour has a valid target and start transform, "
                                        + "you can press this button to set the target transform to the start state."))) {
        Undo.IncrementCurrentGroup();
        Undo.SetCurrentGroupName("Set Target(s) To Start");
        foreach (var individualTarget in targets) {
          Undo.RecordObject(individualTarget.targetTransform, "Move Target To Start");
          individualTarget.SetTargetToStart();
        }
      }

      EditorGUI.EndDisabledGroup();

      EditorGUI.BeginDisabledGroup(target.targetTransform == null
                                  || target.endTransform == null
                                  || PrefabUtility.GetPrefabType(target.gameObject) == PrefabType.Prefab);

      if (GUILayout.Button(new GUIContent("Set Target" + (targets.Length > 1 ? "s" : "") + " To End",
                                          "If this TransformTweenBehaviour has a valid target and end transform, "
                                        + "you can press this button to set the target transform to the end state."))) {
        Undo.IncrementCurrentGroup();
        Undo.SetCurrentGroupName("Set Target(s) To End");
        foreach (var individualTarget in targets) {
          Undo.RecordObject(individualTarget.targetTransform, "Move Target To End");
          individualTarget.SetTargetToEnd();
        }
      }

      EditorGUILayout.EndHorizontal();

      EditorGUI.EndDisabledGroup();

      base.OnInspectorGUI();
    }

  }

}
