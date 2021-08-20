/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2020.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

 using UnityEditor;
 using UnityEngine;

namespace Leap.Unity {

  [CustomEditor(typeof(LeapXRServiceProvider))]
  public class LeapXRServiceProviderEditor : LeapServiceProviderEditor {

    protected override void OnEnable() {
      base.OnEnable();
      isVRProvider = true;

      specifyConditionalDrawing(() => { return serializedObject
                                                 .FindProperty("_temporalWarpingMode")
                                                   .enumValueIndex == 1; },
                                "_customWarpAdjustment");

      specifyConditionalDrawing(() => { return serializedObject
                                                 .FindProperty("_deviceOffsetMode")
                                                   .enumValueIndex == 1; },
                                "_deviceOffsetYAxis", 
                                "_deviceOffsetZAxis",
                                "_deviceTiltXAxis");

      specifyConditionalDrawing(() => { return serializedObject
                                                 .FindProperty("_deviceOffsetMode")
                                                   .enumValueIndex == 2; },
                                "_deviceOrigin");

      addPropertyToFoldout("_deviceOffsetMode"    , "Advanced Options");
      addPropertyToFoldout("_temporalWarpingMode" , "Advanced Options");
      addPropertyToFoldout("_customWarpAdjustment", "Advanced Options");
      addPropertyToFoldout("_deviceOffsetYAxis"   , "Advanced Options");
      addPropertyToFoldout("_deviceOffsetZAxis"   , "Advanced Options");
      addPropertyToFoldout("_deviceTiltXAxis"     , "Advanced Options");
      addPropertyToFoldout("_deviceOrigin"        , "Advanced Options");
      addPropertyToFoldout("_preCullCamera"       , "Advanced Options");
      addPropertyToFoldout("_updateHandInPrecull" , "Advanced Options");
    }

    private void decorateAllowManualTimeAlignment(SerializedProperty property) {
      bool pcOrAndroidPlatformDetected = false;
      string targetPlatform = "";
#if UNITY_STANDALONE
      pcOrAndroidPlatformDetected = true;
      targetPlatform = "Standalone (Desktop)";
#elif UNITY_ANDROID
      pcOrAndroidPlatformDetected = true;
      targetPlatform = "Android";
#endif

      if (pcOrAndroidPlatformDetected && property.boolValue) {
        EditorGUILayout.HelpBox(targetPlatform + " target platform detected; "
                              + "manual time alignment should not be enabled under most "
                              + "circumstances.", MessageType.Warning);
      }
    }

    public override void OnSceneGUI() {
      LeapXRServiceProvider xrProvider = target as LeapXRServiceProvider;
      if (serializedObject.FindProperty("_deviceOffsetMode").enumValueIndex == 2 &&
          xrProvider.deviceOrigin != null) {
        controllerOffset = xrProvider.transform.InverseTransformPoint(xrProvider.deviceOrigin.position);
        deviceRotation = Quaternion.Inverse(xrProvider.transform.rotation) * 
                         xrProvider.deviceOrigin.rotation * 
                         Quaternion.Euler(90f, 0f, 0f);
      } else {
        var vrProvider = target as LeapXRServiceProvider;

        deviceRotation = Quaternion.Euler(90f, 0f, 0f) * 
                         Quaternion.Euler(vrProvider.deviceTiltXAxis, 0f, 0f);

        controllerOffset = new Vector3(0f,
                                       vrProvider.deviceOffsetYAxis,
                                       vrProvider.deviceOffsetZAxis);
      }

      base.OnSceneGUI();
    }
  }
}
