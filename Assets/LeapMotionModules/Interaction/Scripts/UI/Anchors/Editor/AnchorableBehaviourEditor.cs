using Leap.Unity;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Leap.Unity.Interaction {

  [CustomEditor(typeof(AnchorableBehaviour))]
  public class AnchorableBehaviourEditor : CustomEditorBase {

    protected override void OnEnable() {
      base.OnEnable();

      specifyConditionalDrawing("_anchorType",
                                (int)AnchorableBehaviour.AnchorType.AnchorGroup,
                                "_anchorGroup");

      specifyConditionalDrawing("isAttractedByHand",
                                "maxAttractionReach",
                                "attractionReachByDistance");
    }

  }

}