/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2017.                                 *
 * Leap Motion proprietary and  confidential.                                 *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Leap.Unity.Interaction {

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
      specifyConditionalDrawing("_showAdvancedSettings", "_drawControllerRuntimeGizmos");

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
