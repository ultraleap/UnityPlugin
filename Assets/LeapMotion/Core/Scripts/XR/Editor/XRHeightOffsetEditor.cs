/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2018.                                 *
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
using Leap.Unity;

namespace Leap.Unity {

  [CanEditMultipleObjects]
  [CustomEditor(typeof(XRHeightOffset))]
  public class XRHeightOffsetEditor : CustomEditorBase<XRHeightOffset> {

    protected override void OnEnable() {
      base.OnEnable();

      //specifyCustomDrawer("_roomScaleHeightOffset", drawHeightOffset);
      //specifyCustomDecorator("_roomScaleHeightOffset", decorateHeightOffset);
    }

    //private void drawHeightOffset(SerializedProperty property) {
    //  var isRoomScale = XRSupportUtils.IsRoomScale();
    //  EditorGUI.BeginDisabledGroup(isRoomScale && Application.isPlaying);
    //  EditorGUILayout.PropertyField(property);
    //  EditorGUI.EndDisabledGroup();
    //}

    //private void decorateHeightOffset(SerializedProperty property) {
    //  if (isRoomScaleTrackingDetected()) {
    //    var message = "RoomScale XR space tracking detected. The Height Offset field "
    //                + "is unnecessary because the rig root is interpreted as the floor "
    //                + "rather than the root for the head. The offset will be set to zero "
    //                + "during play mode.";
    //    EditorGUILayout.HelpBox(message, MessageType.Info);
    //  }
    //}

  }

}
