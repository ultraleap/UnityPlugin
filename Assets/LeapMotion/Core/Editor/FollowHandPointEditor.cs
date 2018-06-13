using UnityEngine;
using UnityEditor;

namespace Leap.Unity.Attachments {
  
  [CanEditMultipleObjects]
  [CustomEditor(typeof(FollowHandPoint))]
  public class FollowHandPointEditor : CustomEditorBase<FollowHandPoint> {

    protected override void OnEnable() {
      base.OnEnable();

      specifyConditionalDrawing("showAdvancedSettings",
                                "usePalmRotationForWrist");
    }

  }

}
