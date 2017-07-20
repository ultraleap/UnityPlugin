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

  [CustomEditor(typeof(LeapVRTemporalWarping))]
  public class LeapTemporalWarpingEditor : CustomEditorBase {

    protected override void OnEnable() {
      base.OnEnable();

      specifyConditionalDrawing("allowManualTimeAlignment",
                                "warpingAdjustment",
                                "unlockHold",
                                "moreRewind",
                                "lessRewind");

      specifyCustomDecorator("provider", warningDecorator);
    }

    private void warningDecorator(SerializedProperty prop) {
      if (!PlayerSettings.virtualRealitySupported) {
        EditorGUILayout.HelpBox("Unity VR Disabled.  ManuallyUpdateTemporalWarping must be called right after " +
                                "the Head transform has been updated.", MessageType.Warning);
      }
    }
  }
}
