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

namespace Leap.Unity {

  [CustomEditor(typeof(LeapServiceProvider))]
  public class LeapServiceProviderEditor : CustomEditorBase<LeapServiceProvider> {
    protected override void OnEnable() {
      base.OnEnable();

      specifyConditionalDrawing("_overrideDeviceType",
                                "_overrideDeviceTypeWith");

      specifyCustomDecorator("_frameOptimization", frameOptimizationWarning);
    }

    private void frameOptimizationWarning(SerializedProperty property) {
      var mode = (LeapServiceProvider.FrameOptimizationMode)property.intValue;
      string warningText;

      switch (mode) {
        case LeapServiceProvider.FrameOptimizationMode.ReuseUpdateForPhysics:
          warningText = "Reusing update frames for physics introduces a frame of latency for physics interactions.";
          break;
        case LeapServiceProvider.FrameOptimizationMode.ReusePhysicsForUpdate:
          warningText = "This optimization REQUIRES physics framerate to match your target framerate EXACTLY.";
          break;
        default:
          return;
      }

      EditorGUILayout.HelpBox(warningText, MessageType.Warning);
    }

    public override void OnInspectorGUI() {
      if (UnityEditor.PlayerSettings.virtualRealitySupported) {
        EditorGUILayout.HelpBox("VR support is enabled. If your Leap is mounted to your headset, you should be using "
                              + "LeapVRServiceProvider instead of LeapServiceProvider. (If your Leap is not "
                              + "mounted to your headset, you can safely ignore this warning.)",
                                MessageType.Warning);
      }

      base.OnInspectorGUI();
    }
  }
}
