using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Leap.Unity.UI.Interaction {

  [CustomEditor(typeof(InteractionManager))]
  public class InteractionManagerEditor : CustomEditorBase<InteractionManager> {

    protected override void OnEnable() {
      base.OnEnable();

      // Advanced settings
      specifyConditionalDrawing("_showAdvancedSettings", "hoverActivationRadius");
      specifyConditionalDrawing("_showAdvancedSettings", "touchActivationRadius");
      specifyConditionalDrawing("_showAdvancedSettings", "_autoGenerateLayers");
      specifyConditionalDrawing("_showAdvancedSettings", "_templateLayer");
      specifyConditionalDrawing("_showAdvancedSettings", "_interactionLayer");
      specifyConditionalDrawing("_showAdvancedSettings", "_interactionNoContactLayer");
      specifyConditionalDrawing("_showAdvancedSettings", "_contactBoneLayer");
      specifyConditionalDrawing("_showAdvancedSettings", "_drawHandRuntimeGizmos");

      // Layers
      SerializedProperty showAdvancedSettingsProperty = serializedObject.FindProperty("_showAdvancedSettings");
      SerializedProperty autoGenerateLayerProperty = serializedObject.FindProperty("_autoGenerateLayers");
      specifyConditionalDrawing(() => autoGenerateLayerProperty.boolValue
                                   && showAdvancedSettingsProperty.boolValue,
                                "_templateLayer");
      specifyConditionalDrawing(() => !autoGenerateLayerProperty.boolValue
                                   && showAdvancedSettingsProperty.boolValue,
                                "_interactionLayer",
                                "_interactionNoContactLayer",
                                "_contactBoneLayer");
    }

  }

}