using Leap.Unity;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Leap.Unity.UI.Interaction {

  [CustomEditor(typeof(InteractionBehaviourBase))]
  public class InteractionBehaviourBaseEditor : CustomEditorBase {

    protected override void OnEnable() {
      base.OnEnable();

      specifyConditionalDrawing("enableGrasping", "allowsTwoHandedGrab");
    }

  }

  [CustomEditor(typeof(InteractionBehaviour))]
  public class InteractionBehaviourEditor : InteractionBehaviourBaseEditor {

    protected override void OnEnable() {
      base.OnEnable();
    }

  }

}