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
        EditorGUILayout.HelpBox("Unity VR Disabled.  ManualyUpdateTemporalWarping must be called right after " +
                                "the Head transform has been updated.", MessageType.Warning);
      }
    }
  }
}
