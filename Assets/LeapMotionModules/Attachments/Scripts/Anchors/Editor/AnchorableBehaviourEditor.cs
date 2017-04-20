using Leap.Unity;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Leap.Unity.Attachments {

  [CustomEditor(typeof(AnchorableBehaviour))]
  public class AnchorableBehaviourEditor : CustomEditorBase {

    protected override void OnEnable() {
      base.OnEnable();

      specifyConditionalDrawing("isAttractedByHand",
                                "maxAttractionReach",
                                "attractionReachByDistance");
    }

  }

}