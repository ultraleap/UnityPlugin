/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2018.                                 *
 * Leap Motion proprietary and confidential.                                  *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
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

      specifyConditionalDrawing("_allowManualDeviceOffset",
                                  "_deviceOffsetYAxis", "_deviceOffsetZAxis",
                                  "_deviceTiltXAxis");
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
      arcDirection = Vector3.up;
      deviceRotation = Quaternion.Euler(90f, 0f, 0f);

      var vrProvider = target as LeapXRServiceProvider;

      deviceRotation *= Quaternion.Euler(vrProvider.deviceTiltXAxis, 0f, 0f);

      controllerOffset = new Vector3(0f,
                                     vrProvider.deviceOffsetYAxis,
                                     vrProvider.deviceOffsetZAxis);

      base.OnSceneGUI();
    }
  }
}
