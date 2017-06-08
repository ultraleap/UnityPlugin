/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2017.                                 *
 * Leap Motion proprietary and  confidential.                                 *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Leap.Unity.Interaction {

  [CanEditMultipleObjects]
  [CustomEditor(typeof(InteractionController), true)]
  public class InteractionControllerEditor : CustomEditorBase<InteractionController> {

    protected override void OnEnable() {
      base.OnEnable();
    }

    public override void OnInspectorGUI() {
      base.OnInspectorGUI();

      // Check if any selected InteractionHands have no Primary Hover points checked.
      bool anyPrimaryHoverPoints = false;
      bool anyWithNoPrimaryHoverPoints = false;
      foreach (var singleTarget in targets) {
        anyPrimaryHoverPoints = false;
        foreach (var primaryHoverPoint in singleTarget.primaryHoverPoints) {
          if (primaryHoverPoint != null) {
            anyPrimaryHoverPoints = true;
            break;
          }
        }

        if (singleTarget.intHand != null) {
          for (int i = 0; i < singleTarget.intHand.enabledPrimaryHoverFingertips.Length; i++) {
            if (singleTarget.intHand.enabledPrimaryHoverFingertips[i]) {
              anyPrimaryHoverPoints = true;
              break;
            }
          }
        }

        if (!anyPrimaryHoverPoints) {
          anyWithNoPrimaryHoverPoints = true;
          break;
        }
      }

      if (anyWithNoPrimaryHoverPoints) {
        string message = "No primary hover points found for this interaction controller. "
                       + "This controller will never trigger primary hover for an object. "
                       + "UI elements such as InteractionButton and InteractionSlider "
                       + "will not be able to interact with this interaction controller.";
        EditorGUILayout.HelpBox(message, MessageType.Warning);
      }
    }

  }

}
