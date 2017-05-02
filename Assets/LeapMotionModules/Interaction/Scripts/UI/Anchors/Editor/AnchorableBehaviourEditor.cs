using Leap.Unity;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Leap.Unity.Interaction {

  [CustomEditor(typeof(AnchorableBehaviour))]
  public class AnchorableBehaviourEditor : CustomEditorBase<AnchorableBehaviour> {

    protected override void OnEnable() {
      base.OnEnable();

      // Only draw anchor group settings when set to the AnchorGroup type.
      specifyConditionalDrawing("_anchorType",
                                (int)AnchorableBehaviour.AnchorType.AnchorGroup,
                                "_anchorGroup");

      // Only draw matchAnchorMotionWhileReturning when lockToAnchorWhenAttached is enabled
      specifyConditionalDrawing("lockToAnchorWhenAttached",
                                "matchAnchorMotionWhileReturning");

      // Only draw Interaction settings when assigned an InteractionBehaviour.
      specifyConditionalDrawing(() => { return target.interactionBehaviour != null; },
                                "detachWhenGrasped",
                                "tryAnchorOnGraspEnd",
                                "isAttractedByHand",
                                "maxAttractionReach",
                                "attractionReachByDistance");

      // Only draw hand attraction settings when it is enabled.
      specifyConditionalDrawing("isAttractedByHand",
                                "maxAttractionReach",
                                "attractionReachByDistance");
    }

  }

}