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
    }
  }
}
  