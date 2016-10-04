using UnityEditor;

namespace Leap.Unity {

  [CustomEditor(typeof(LeapServiceProvider))]
  public class LeapServiceProviderEditor : CustomEditorBase {
    protected override void OnEnable() {
      base.OnEnable();

      specifyConditionalDrawing("_overrideDeviceType",
                                "_overrideDeviceTypeWith");
    }
  }
}
