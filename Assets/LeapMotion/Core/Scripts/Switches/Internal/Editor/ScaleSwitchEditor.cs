using UnityEditor;

namespace Leap.Unity.Animation {
  
  [CustomEditor(typeof(ScaleSwitch), editorForChildClasses: true)]
  public class ScaleSwitchEditor : SwitchEditorBase<ScaleSwitch> {

    protected override void OnEnable() {
      base.OnEnable();

      specifyConditionalDrawing("nonUniformScale",
                                "xScaleCurve",
                                "yScaleCurve",
                                "zScaleCurve");
    }

  }

}
