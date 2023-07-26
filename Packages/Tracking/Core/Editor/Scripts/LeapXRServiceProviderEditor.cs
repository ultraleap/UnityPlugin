/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2023.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using UnityEditor;
using UnityEngine;
namespace Leap.Unity
{

    [CustomEditor(typeof(LeapXRServiceProvider))]
    public class LeapXRServiceProviderEditor : LeapServiceProviderEditor
    {
        SerializedProperty _mainCamera;

        string[] testHandPoses = new string[] { "HeadMountedA", "HeadMountedB" };

        protected override void OnEnable()
        {
            _mainCamera = serializedObject.FindProperty("_mainCamera");

            if (_mainCamera.objectReferenceValue == null)
            {
                _mainCamera.objectReferenceValue = Camera.main;
                serializedObject.ApplyModifiedProperties();

                if (_mainCamera.objectReferenceValue != null)
                {
                    Debug.Log("Camera.Main automatically assigned");
                }
            }

            base.OnEnable();
            isVRProvider = true;

            specifyCustomDrawer("editTimePose", DrawCustomEnum);

            specifyConditionalDrawing(() =>
            {
                return serializedObject
                         .FindProperty("_temporalWarpingMode")
                           .enumValueIndex == 1;
            },
                                      "_customWarpAdjustment");

            specifyConditionalDrawing(() =>
            {
                return serializedObject
                         .FindProperty("_deviceOffsetMode")
                           .enumValueIndex == 1;
            },
                                      "_deviceOffsetYAxis",
                                      "_deviceOffsetZAxis",
                                      "_deviceTiltXAxis");

            specifyConditionalDrawing(() =>
            {
                return serializedObject
                         .FindProperty("_deviceOffsetMode")
                           .enumValueIndex == 2;
            },
                                      "_deviceOrigin");

            addPropertyToFoldout("_deviceOffsetMode", "Advanced Options");
            addPropertyToFoldout("_temporalWarpingMode", "Advanced Options");
            addPropertyToFoldout("_customWarpAdjustment", "Advanced Options");
            addPropertyToFoldout("_positionDeviceRelativeToMainCamera", "Advanced Options");
            addPropertyToFoldout("_deviceOffsetYAxis", "Advanced Options");
            addPropertyToFoldout("_deviceOffsetZAxis", "Advanced Options");
            addPropertyToFoldout("_deviceTiltXAxis", "Advanced Options");
            addPropertyToFoldout("_deviceOrigin", "Advanced Options");
            addPropertyToFoldout("_updateHandInPrecull", "Advanced Options");
            addPropertyToFoldout("_preventInitializingTrackingMode", "Advanced Options");
            addPropertyToFoldout("_autoCreateTrackedPoseDriver", "Advanced Options");
            hideField("_trackingOptimization");

            // Ensure the default values are up to date by re-firing the property setter
            if ((target as LeapXRServiceProvider).deviceOffsetMode == LeapXRServiceProvider.DeviceOffsetMode.Default)
            {
                (target as LeapXRServiceProvider).deviceOffsetMode = LeapXRServiceProvider.DeviceOffsetMode.Default;
            }
        }

        private void DrawCustomEnum(SerializedProperty property)
        {
            property.enumValueIndex = EditorGUILayout.Popup("Edit Time Pose", property.enumValueIndex, testHandPoses);
            serializedObject.ApplyModifiedProperties();
        }
    }
}