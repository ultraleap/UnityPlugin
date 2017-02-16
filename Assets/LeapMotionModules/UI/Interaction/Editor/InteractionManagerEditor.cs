using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Leap.Unity.UI.Interaction {

  [CustomEditor(typeof(InteractionManager))]
  public class InteractionManagerEditor : CustomEditorBase {

    protected override void OnEnable() {
      base.OnEnable();

      specifyConditionalDrawing("_showDebugOptions", "_debugDrawInteractionHands");
    }

  }

}