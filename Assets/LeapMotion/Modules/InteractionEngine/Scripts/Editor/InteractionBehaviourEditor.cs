/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2017.                                 *
 * Leap Motion proprietary and  confidential.                                 *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

using UnityEditor;
using UnityEngine;

namespace Leap.Unity.Interaction {

  [CanEditMultipleObjects]
  [CustomEditor(typeof(InteractionBehaviour), editorForChildClasses: true)]
  public class InteractionBehaviourEditor : CustomEditorBase<InteractionBehaviour> {

    private EnumEventTableEditor _tableEditor;

    protected override void OnEnable() {
      base.OnEnable();

      deferProperty("_eventTable");
      specifyCustomDrawer("_eventTable", drawEventTable);

      specifyConditionalDrawing(() => !target.ignoreContact,
                                "_contactForceMode");

      specifyConditionalDrawing(() => !target.ignoreGrasping,
                                "_allowMultiGrasp",
                                "_moveObjectWhenGrasped",
                                "graspedMovementType",
                                "graspHoldWarpingEnabled__curIgnored");
    }

    public override void OnInspectorGUI() {
      checkHasColliders();
      
      base.OnInspectorGUI();
    }

    private void checkHasColliders() {
      bool anyMissingColliders = false;
      foreach (var singleTarget in targets) {
        if (singleTarget.GetComponentsInChildren<Collider>().Length == 0) {
          anyMissingColliders = true; break;
        }
      }

      if (anyMissingColliders) {
        bool pluralObjects = targets.Length > 1;

        string message;
        if (pluralObjects) {
          message = "One or more of the currently selected interaction objects have no "
                  + "colliders. Interaction objects without any Colliders cannot be "
                  + "interacted with.";
        }
        else {
          message = "This interaction object has no Colliders. Interaction objects "
                  + "without any Colliders cannot be interacted with.";
        }

        EditorGUILayout.HelpBox(message, MessageType.Warning);
      }
    }

    private void drawEventTable(SerializedProperty property) {
      if (_tableEditor == null) {
        _tableEditor = new EnumEventTableEditor(property, typeof(InteractionBehaviour.EventType));
      }

      _tableEditor.DoGuiLayout();
    }
  }
}
