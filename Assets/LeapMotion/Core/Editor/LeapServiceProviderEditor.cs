/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2017.                                 *
 * Leap Motion proprietary and  confidential.                                 *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

using UnityEditor;

namespace Leap.Unity {

  [CustomEditor(typeof(LeapServiceProvider))]
  public class LeapServiceProviderEditor : CustomEditorBase {
    protected override void OnEnable() {
      base.OnEnable();

      specifyCustomDecorator("_frameOptimization", frameOptimizationWarning);

      specifyConditionalDrawing("_frameOptimization", (int)LeapServiceProvider.FrameOptimizationMode.None,
                                "_physicsExtrapolation",
                                "_physicsExtrapolationTime");

      specifyConditionalDrawing("_physicsExtrapolation", (int)LeapServiceProvider.PhysicsExtrapolationMode.Manual,
                                "_physicsExtrapolationTime");
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
  }
}
