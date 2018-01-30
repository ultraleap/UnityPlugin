using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Leap.Unity {

  [CustomEditor(typeof(LeapEyeDislocator))]
  public class LeapEyeDislocatorEditor : CustomEditorBase<LeapEyeDislocator> {

    protected override void OnEnable() {
      base.OnEnable();

      specifyConditionalDrawing("_useCustomBaseline", "_customBaselineValue");
    }
  }
}
