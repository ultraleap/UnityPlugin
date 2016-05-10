using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;

namespace Leap.Unity.Interaction {

  [CustomEditor(typeof(InteractionManager), true)]
  public class InteractionManagerEditor : CustomEditorBase {

    private IInteractionBehaviour[] _interactionBehaviours;

    private string[] _layerNames;
    private List<int> _layerValues;

    private bool _anyBehavioursUnregistered;

    protected override void OnEnable() {
      base.OnEnable();

      Dictionary<int, string> valueToLayer = new Dictionary<int, string>();
      for (int i = 0; i < 32; i++) {
        string layerName = LayerMask.LayerToName(i);
        if (!string.IsNullOrEmpty(layerName)) {
          valueToLayer[i] = layerName;
        }
      }

      _layerValues = valueToLayer.Keys.ToList();
      _layerNames = valueToLayer.Values.ToArray();

      specifyCustomDecorator("_leapProvider", providerDectorator);

      SerializedProperty autoGenerateLayerProperty = serializedObject.FindProperty("_autoGenerateLayers");
      specifyConditionalDrawing(() => !autoGenerateLayerProperty.boolValue,
                                "_brushHandLayer",
                                "_interactionNoClipLayer");

      specifyCustomDrawer("_interactionLayer", doSingleLayerGUI);
      specifyCustomDrawer("_brushHandLayer", doSingleLayerGUI);
      specifyCustomDrawer("_interactionNoClipLayer", doSingleLayerGUI);

      specifyCustomDecorator("_interactionLayer", collisionLayerHelper);

      _interactionBehaviours = FindObjectsOfType<IInteractionBehaviour>();
      for (int i = 0; i < _interactionBehaviours.Length; i++) {
        if (_interactionBehaviours[i].Manager == null) {
          _anyBehavioursUnregistered = true;
          break;
        }
      }
    }

    private void collisionLayerHelper(SerializedProperty prop) {
      InteractionManager manager = target as InteractionManager;

      bool hasError = false;
      if (manager.InteractionBrushLayer == manager.InteractionLayer) {
        EditorGUILayout.HelpBox("Brush Layer cannot be the same as Interaction Layer", MessageType.Error);
        hasError = true;
      }

      if (manager.InteractionBrushLayer == manager.InteractionNoClipLayer) {
        EditorGUILayout.HelpBox("Brush Layer cannot be the same as No-Clip Layer", MessageType.Error);
        hasError = true;
      }

      if (manager.InteractionLayer == manager.InteractionNoClipLayer) {
        EditorGUILayout.HelpBox("Interaction Layer cannot be the same as No-Clip Layer", MessageType.Error);
        hasError = true;
      }

      if (hasError) {
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
      if (_anyBehavioursUnregistered) {
        EditorGUILayout.HelpBox("Some Interaction Behaviours do not have their manager assigned!  Do you want to assign them to this manager?", MessageType.Warning);
        if (GUILayout.Button("Assign To This Manager")) {
          for (int i = 0; i < _interactionBehaviours.Length; i++) {
            var behaviour = _interactionBehaviours[i];
            if (behaviour.Manager == null) {
              behaviour.Manager = target as InteractionManager;
              EditorUtility.SetDirty(behaviour);
            }
          }
          _anyBehavioursUnregistered = false;
        }
        GUILayout.Space(EditorGUIUtility.singleLineHeight);
      }
    }

    private void doSingleLayerGUI(SerializedProperty property) {
      int index = _layerValues.IndexOf(property.intValue);
      if (index < 0) {
        if (Application.isPlaying) {
          //If application is playing we dont want to change the layers on the fly
          //Instead, just display them as an int value
          property.intValue = EditorGUILayout.IntField(property.displayName, property.intValue);
          return;
        } else {
          //If the application is not running, reset the layer to the default layer
          property.intValue = 0;
          index = 0;
        }
      }

      index = EditorGUILayout.Popup(property.displayName, index, _layerNames);
      property.intValue = _layerValues[index];
    }

    public override void OnInspectorGUI() {
      base.OnInspectorGUI();

      if (Application.isPlaying) {
        EditorGUILayout.Space();
        InteractionManager manager = target as InteractionManager;

        EditorGUILayout.LabelField("Info", EditorStyles.boldLabel);
        using (new EditorGUI.DisabledGroupScope(true)) {
          EditorGUILayout.IntField("Registered Count", manager.RegisteredObjects.Count());
          EditorGUILayout.IntField("Grasped Count", manager.GraspedObjects.Count());
        }
      }
    }
  }
}
