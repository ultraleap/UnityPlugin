using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Leap.Unity.UI.Interaction {

  [CustomEditor(typeof(InteractionButton), editorForChildClasses: true)]
  public class InteractionButtonEditor : InteractionBehaviourEditor {

    protected override void OnEnable() {
      base.OnEnable();

      deferProperty("OnPress");
      deferProperty("OnUnpress");
    }
  }
}
