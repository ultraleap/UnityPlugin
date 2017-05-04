/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2017.                                 *
 * Leap Motion proprietary and  confidential.                                 *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

using UnityEngine;
using UnityEditor;
using System.Linq;

namespace Leap.Unity.Interaction {

  [CustomEditor(typeof(InteractionManager), true)]
  public class InteractionManagerEditor : CustomEditorBase<InteractionManager> {

    private IInteractionBehaviour[] _interactionBehaviours;

    private bool _anyBehavioursUnregistered;

    protected override void OnEnable() {
      base.OnEnable();

      specifyCustomDrawer("_ldatPath", disableWhenRunning);
      specifyCustomDrawer("_contactEnabled", disableWhenRunning);
      specifyCustomDrawer("_autoGenerateLayers", disableWhenRunning);
      specifyCustomDrawer("_templateLayer", disableWhenRunning);

      specifyCustomDecorator("_leapProvider", providerDectorator);

      SerializedProperty autoGenerateLayerProperty = serializedObject.FindProperty("_autoGenerateLayers");
      specifyConditionalDrawing(() => autoGenerateLayerProperty.boolValue,
                                "_templateLayer");
      specifyConditionalDrawing(() => !autoGenerateLayerProperty.boolValue,
                                "_interactionLayer",
                                "_interactionNoClipLayer",
                                "_brushLayer");

      specifyConditionalDrawing("_graspingEnabled", "_twoHandedGrasping");

      specifyCustomDecorator("_interactionLayer", collisionLayerHelper);

      _interactionBehaviours = FindObjectsOfType<IInteractionBehaviour>();
      for (int i = 0; i < _interactionBehaviours.Length; i++) {
        if (_interactionBehaviours[i].Manager == null) {
          _anyBehavioursUnregistered = true;
          break;
        }
      }
    }

    private void disableWhenRunning(SerializedProperty property) {
      EditorGUI.BeginDisabledGroup(EditorApplication.isPlaying);
      EditorGUILayout.PropertyField(property);
      EditorGUI.EndDisabledGroup();
    }

    private void collisionLayerHelper(SerializedProperty prop) {
      if (target.InteractionBrushLayer == target.InteractionLayer) {
        EditorGUILayout.HelpBox("Brush Layer cannot be the same as Interaction Layer", MessageType.Error);
        return;
      }

      if (target.InteractionBrushLayer == target.InteractionNoClipLayer) {
        EditorGUILayout.HelpBox("Brush Layer cannot be the same as No-Clip Layer", MessageType.Error);
        return;
      }

      if (target.InteractionLayer == target.InteractionNoClipLayer) {
        EditorGUILayout.HelpBox("Interaction Layer cannot be the same as No-Clip Layer", MessageType.Error);
        return;
      }

      if (!serializedObject.FindProperty("_autoGenerateLayers").boolValue) {
        if (Physics.GetIgnoreLayerCollision(target.InteractionBrushLayer, target.InteractionLayer)) {
          using (new GUILayout.HorizontalScope()) {
            EditorGUILayout.HelpBox("Brush Layer should collide with Interaction Layer", MessageType.Warning);
            if (GUILayout.Button("Auto-fix")) {
              Physics.IgnoreLayerCollision(target.InteractionBrushLayer, target.InteractionLayer, false);
            }
          }
        }

        if (!Physics.GetIgnoreLayerCollision(target.InteractionBrushLayer, target.InteractionNoClipLayer)) {
          using (new GUILayout.HorizontalScope()) {
            EditorGUILayout.HelpBox("Brush Layer should not collide with No-Clip Layer", MessageType.Warning);
            if (GUILayout.Button("Auto-fix")) {
              Physics.IgnoreLayerCollision(target.InteractionBrushLayer, target.InteractionNoClipLayer, true);
            }
          }
        }
      }
    }

    private void providerDectorator(SerializedProperty prop) {
      if (Physics.defaultContactOffset > target.RecommendedContactOffsetMaximum) {
        GUILayout.BeginHorizontal();
        EditorGUILayout.HelpBox("The current default contact offset is " + Physics.defaultContactOffset + ", which is greater than the recomended value " + target.RecommendedContactOffsetMaximum, MessageType.Warning);
        if (GUILayout.Button("Auto-fix")) {
          Physics.defaultContactOffset = target.RecommendedContactOffsetMaximum;
        }
        GUILayout.EndHorizontal();
      }

      if (_anyBehavioursUnregistered) {
        GUILayout.BeginHorizontal();
        EditorGUILayout.HelpBox("Some Interaction Behaviours do not have their manager assigned!  Do you want to assign them to this manager?", MessageType.Warning);
        if (GUILayout.Button("Auto-fix")) {
          for (int i = 0; i < _interactionBehaviours.Length; i++) {
            var behaviour = _interactionBehaviours[i];
            if (behaviour.Manager == null) {
              behaviour.Manager = target;
              EditorUtility.SetDirty(behaviour);
            }
          }
          _anyBehavioursUnregistered = false;
        }
        GUILayout.EndHorizontal();
      }
    }

    public override void OnInspectorGUI() {
      base.OnInspectorGUI();

      if (Application.isPlaying) {
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Info", EditorStyles.boldLabel);
        using (new EditorGUI.DisabledGroupScope(true)) {
          EditorGUILayout.IntField("Registered Count", target.RegisteredObjects.Count());
          EditorGUILayout.IntField("Grasped Count", target.GraspedObjects.Count);
        }
      }
    }
  }
}
