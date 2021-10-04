/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2020.                                   *
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

        SerializedProperty _camera;

        public enum XRTestHandPose
        {
            HeadMountedA,
            HeadMountedB,
        }
        public XRTestHandPose TestHandPose;

        protected override void OnEnable()
        {

            _camera = serializedObject.FindProperty("_camera");

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
            addPropertyToFoldout("_deviceOffsetYAxis", "Advanced Options");
            addPropertyToFoldout("_deviceOffsetZAxis", "Advanced Options");
            addPropertyToFoldout("_deviceTiltXAxis", "Advanced Options");
            addPropertyToFoldout("_deviceOrigin", "Advanced Options");
            addPropertyToFoldout("_updateHandInPrecull", "Advanced Options");
        }

        private void DrawCustomEnum(SerializedProperty property)
        {
            TestHandPose = (XRTestHandPose)EditorGUILayout.EnumPopup("Edit Time Pose", TestHandPose);

            switch (TestHandPose)
            {
                case XRTestHandPose.HeadMountedA:
                    property.enumValueIndex = (int)TestHandFactory.TestHandPose.HeadMountedA;
                    break;
                case XRTestHandPose.HeadMountedB:
                    property.enumValueIndex = (int)TestHandFactory.TestHandPose.HeadMountedB;
                    break;
            }
        }


        private void decorateAllowManualTimeAlignment(SerializedProperty property)
        {
            bool pcOrAndroidPlatformDetected = false;
            string targetPlatform = "";
#if UNITY_STANDALONE
            pcOrAndroidPlatformDetected = true;
            targetPlatform = "Standalone (Desktop)";
#elif UNITY_ANDROID
      pcOrAndroidPlatformDetected = true;
      targetPlatform = "Android";
#endif

            if (pcOrAndroidPlatformDetected && property.boolValue)
            {
                EditorGUILayout.HelpBox(targetPlatform + " target platform detected; "
                                      + "manual time alignment should not be enabled under most "
                                      + "circumstances.", MessageType.Warning);
            }
        }

        public override void OnInspectorGUI()
        {
            if (_camera.objectReferenceValue == null)
            {
                EditorGUILayout.PropertyField(_camera);
                serializedObject.ApplyModifiedProperties();
                return;
            }

            base.OnInspectorGUI();
        }

        public override void OnSceneGUI()
        {
            LeapXRServiceProvider xrProvider = target as LeapXRServiceProvider;

            if (xrProvider.Camera == null) { return; }

            controllerOffset = new Vector3(0f,
                                xrProvider.deviceOffsetYAxis,
                                xrProvider.deviceOffsetZAxis);

            deviceRotation = xrProvider.Camera.transform.InverseTransformRotation(xrProvider.Camera.transform.TransformRotation(Quaternion.Euler(xrProvider.deviceTiltXAxis, 0f, 0f))) * Quaternion.Euler(90f, 0f, 0f);


            base.OnSceneGUI();

        }
    }
}
