using UnityEngine;
using UnityEditor;
using System.Linq;

namespace Leap.Unity.Interaction {

  [CustomEditor(typeof(InteractionManager), true)]
  public class InteractionManagerEditor : CustomEditorBase {

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
      InteractionManager manager = target as InteractionManager;

      if (manager.InteractionBrushLayer == manager.InteractionLayer) {
        EditorGUILayout.HelpBox("Brush Layer cannot be the same as Interaction Layer", MessageType.Error);
        return;
      }

      if (manager.InteractionBrushLayer == manager.InteractionNoClipLayer) {
        EditorGUILayout.HelpBox("Brush Layer cannot be the same as No-Clip Layer", MessageType.Error);
        return;
      }

      if (manager.InteractionLayer == manager.InteractionNoClipLayer) {
        EditorGUILayout.HelpBox("Interaction Layer cannot be the same as No-Clip Layer", MessageType.Error);
        return;
      }

      if (!serializedObject.FindProperty("_autoGenerateLayers").boolValue) {
        if (Physics.GetIgnoreLayerCollision(manager.InteractionBrushLayer, manager.InteractionLayer)) {
          using (new GUILayout.HorizontalScope()) {
            EditorGUILayout.HelpBox("Brush Layer should collide with Interaction Layer", MessageType.Warning);
            if (GUILayout.Button("Auto-fix")) {
              Physics.IgnoreLayerCollision(manager.InteractionBrushLayer, manager.InteractionLayer, false);
            }
          }
        }

        if (!Physics.GetIgnoreLayerCollision(manager.InteractionBrushLayer, manager.InteractionNoClipLayer)) {
          using (new GUILayout.HorizontalScope()) {
            EditorGUILayout.HelpBox("Brush Layer should not collide with No-Clip Layer", MessageType.Warning);
            if (GUILayout.Button("Auto-fix")) {
              Physics.IgnoreLayerCollision(manager.InteractionBrushLayer, manager.InteractionNoClipLayer, true);
            }
          }
        }
      }
    }

    private void providerDectorator(SerializedProperty prop) {
      var manager = target as InteractionManager;

      if (Physics.defaultContactOffset > manager.RecommendedContactOffsetMaximum) {
        GUILayout.BeginHorizontal();
        EditorGUILayout.HelpBox("The current default contact offset is " + Physics.defaultContactOffset + ", which is greater than the recomended value " + manager.RecommendedContactOffsetMaximum, MessageType.Warning);
        if (GUILayout.Button("Auto-fix")) {
          Physics.defaultContactOffset = manager.RecommendedContactOffsetMaximum;
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
              behaviour.Manager = target as InteractionManager;
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
        InteractionManager manager = target as InteractionManager;

        EditorGUILayout.LabelField("Info", EditorStyles.boldLabel);
        using (new EditorGUI.DisabledGroupScope(true)) {
          EditorGUILayout.IntField("Registered Count", manager.RegisteredObjects.Count());
          EditorGUILayout.IntField("Grasped Count", manager.GraspedObjects.Count);
        }
      }
    }
  }
}
