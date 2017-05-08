using Leap.Unity;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Leap.Unity.Interaction {

  [CanEditMultipleObjects]
  [CustomEditor(typeof(AnchorableBehaviour))]
  public class AnchorableBehaviourEditor : CustomEditorBase<AnchorableBehaviour> {

    protected override void OnEnable() {
      base.OnEnable();

      deferProperty("_eventTable");
      specifyCustomDrawer("_eventTable", drawEventTable);

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

    private EnumEventTableEditor _tableEditor;
    private void drawEventTable(SerializedProperty property) {
      if (_tableEditor == null) {
        _tableEditor = new EnumEventTableEditor(property, typeof(AnchorableBehaviour.EventType));
      }

      _tableEditor.DoGuiLayout();
    }

    public override void OnInspectorGUI() {
      //if (target.anchorType == AnchorableBehaviour.AnchorType.SingleAnchor
      //    && target.currentAnchor != null
      //    && target.enabled
      //    && Vector3.Distance(target.transform.position, target.currentAnchor.transform.position) > 0.0001F) {
      //  if (GUILayout.Button(new GUIContent("Move Object To Anchor Position",
      //                                      "Detected that the object is not currently at its anchor, but upon pressing play, "
      //                                    + "the object will move to its anchor. If you'd like the object to move to its anchor now, "
      //                                    + "click this button."))) {
      //    target.transform.position = target.currentAnchor.transform.position;
      //  }
      //}

      base.OnInspectorGUI();
    }

  }

}