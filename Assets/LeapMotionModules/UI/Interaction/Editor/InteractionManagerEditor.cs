using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Leap.Unity.UI.Interaction {

  [CustomEditor(typeof(InteractionManager))]
  public class InteractionManagerEditor : CustomEditorBase {

    void OnEnable() {
      base.OnEnable();

      var manager = target as InteractionManager;

      specifyConditionalDrawing("_showDebugOptions", "_debugDrawInteractionHands");
    }

  }

}