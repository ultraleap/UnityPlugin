/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2017.                                 *
 * Leap Motion proprietary and  confidential.                                 *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

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

      specifyConditionalDrawing("_anchorType",
                                (int)AnchorableBehaviour.AnchorType.AnchorGroup,
                                "_anchorGroup");

      specifyConditionalDrawing("isAttractedByHand",
                                "maxAttractionReach",
                                "attractionReachByDistance");
    }

  }

}
