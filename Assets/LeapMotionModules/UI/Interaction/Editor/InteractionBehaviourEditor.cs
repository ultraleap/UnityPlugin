using Leap.Unity;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Leap.Unity.UI.Interaction {

  [CustomEditor(typeof(InteractionBehaviour))]
  public class InteractionBehaviourEditor : CustomEditorBase {

    protected override void OnEnable() {
      base.OnEnable();

      specifyConditionalDrawing("enableHover", "hoverType");
      specifyConditionalDrawing("enableTouch", "touchType");
      specifyConditionalDrawing("enableGrab",  "grabType");
    }

  }

}