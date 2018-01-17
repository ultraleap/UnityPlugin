using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Leap.Unity {

  [CanEditMultipleObjects]
  [CustomEditor(typeof(XRHeightOffset))]
  public class XRHeightOffsetEditor : CustomEditorBase<XRHeightOffset> {

    protected override void OnEnable() {
      base.OnEnable();

      specifyCustomDrawer("_stationaryHeightOffset", drawHeightOffset);
      specifyCustomDecorator("_stationaryHeightOffset", decorateHeightOffset);
    }

    private void drawHeightOffset(SerializedProperty property) {
      var isRoomScale = isRoomScaleTrackingDetected();
      EditorGUI.BeginDisabledGroup(isRoomScale && Application.isPlaying);
      EditorGUILayout.PropertyField(property);
      EditorGUI.EndDisabledGroup();
    }

    private void decorateHeightOffset(SerializedProperty property) {
      if (isRoomScaleTrackingDetected()) {
        var message = "RoomScale XR space tracking detected. The Height Offset field "
                    + "is unnecessary because the rig root is interpreted as the floor "
                    + "rather than the root for the head. The offset will be set to zero "
                    + "during play mode.";
        EditorGUILayout.HelpBox(message, MessageType.Info);
      }
    }

    private bool isRoomScaleTrackingDetected() {
      var trackingSpaceType = UnityEngine.XR.XRDevice.GetTrackingSpaceType();
      if (trackingSpaceType == UnityEngine.XR.TrackingSpaceType.RoomScale) {
        return true;
      }
      return false;
    }

  }

}