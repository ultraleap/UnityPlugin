using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Leap.Unity.UI.Interaction {

  [CustomEditor(typeof(InteractionManager))]
  public class InteractionManagerEditor : CustomEditorBase {

    void OnEnable() {
      var manager = target as InteractionManager;

      base.specifyConditionalDrawing("_showDebugOptions", "_debugIntHandRoughVolumes");
    }

  }

}