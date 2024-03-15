/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2024.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using LeapInternal;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Leap.Unity
{
    [CustomEditor(typeof(LeapServiceProvider))]
    public class LeapServiceProviderEditor : CustomEditorBase<LeapServiceProvider>
    {
        protected bool isVRProvider = false;

        private LeapServiceProvider _leapServiceProvider;

        private int _chosenDeviceIndex;

        protected override void OnEnable()
        {
            base.OnEnable();

            specifyConditionalDrawing("_interactionVolumeVisualization",
                            new int[]{
                                        (int)LeapServiceProvider.InteractionVolumeVisualization.StereoIR170,
                                        (int)LeapServiceProvider.InteractionVolumeVisualization.Device_3Di,
                                        (int)LeapServiceProvider.InteractionVolumeVisualization.LeapMotionController,
                                        (int)LeapServiceProvider.InteractionVolumeVisualization.LeapMotionController2,
                                        (int)LeapServiceProvider.InteractionVolumeVisualization.Automatic},
                            "FOV_Visualization",
                            "OptimalFOV_Visualization",
                            "MaxFOV_Visualization");

            specifyCustomDrawer("_interactionVolumeVisualization", deviceVisualGizmo);

            specifyConditionalDrawing("FOV_Visualization",
                                        "OptimalFOV_Visualization",
                                        "MaxFOV_Visualization");

            specifyCustomDrawer("FOV_Visualization", DeviceFOVGizmo);
            specifyCustomDrawer("OptimalFOV_Visualization", IndendProperty);
            specifyCustomDrawer("MaxFOV_Visualization", IndendProperty);

            specifyCustomDecorator("_frameOptimization", frameOptimizationWarning);

            specifyConditionalDrawing("_frameOptimization",
                                      (int)LeapServiceProvider.FrameOptimizationMode.None,
                                      "_physicsExtrapolation",
                                      "_physicsExtrapolationTime");

            specifyConditionalDrawing("_physicsExtrapolation",
                                      (int)LeapServiceProvider.PhysicsExtrapolationMode.Manual,
                                      "_physicsExtrapolationTime");

            specifyCustomDrawer("_multipleDeviceMode", multiDeviceToggleWithVersionCheck);
            specifyConditionalDrawing("_multipleDeviceMode",
                          (int)LeapServiceProvider.MultipleDeviceMode.Specific,
                          "_specificSerialNumber");

            specifyCustomDrawer("_specificSerialNumber", drawSerialNumberToggle);

            deferProperty("_serverNameSpace");
            deferProperty("_useInterpolation");

            deferProperty("_reconnectionAttempts");
            deferProperty("_reconnectionInterval");

            if (!(LeapServiceProvider is LeapXRServiceProvider))
            {
                addPropertyToFoldout("_trackingOptimization", "Advanced Options");
            }
            else
            {
                hideField("_trackingOptimization");
            }

            addPropertyToFoldout("_useInterpolation", "Advanced Options");
            addPropertyToFoldout("_serverNameSpace", "Advanced Options");

            addPropertyToFoldout("_reconnectionAttempts", "Advanced Options");
            addPropertyToFoldout("_reconnectionInterval", "Advanced Options");
        }

        private void frameOptimizationWarning(SerializedProperty property)
        {
            var mode = (LeapServiceProvider.FrameOptimizationMode)property.intValue;
            string warningText;

            switch (mode)
            {
                case LeapServiceProvider.FrameOptimizationMode.ReuseUpdateForPhysics:
                    warningText = "Reusing update frames for physics introduces a frame of latency "
                                + "for physics interactions.";
                    break;
                case LeapServiceProvider.FrameOptimizationMode.ReusePhysicsForUpdate:
                    warningText = "This optimization REQUIRES physics framerate to match your "
                                + "target framerate EXACTLY.";
                    break;
                default:
                    return;
            }

            EditorGUILayout.HelpBox(warningText, MessageType.Warning);
        }

        private void deviceVisualGizmo(SerializedProperty property)
        {
            EditorGUILayout.PropertyField(property, new GUIContent("Device Visual Gizmo"));
        }

        private void DeviceFOVGizmo(SerializedProperty property)
        {
            EditorGUILayout.PropertyField(property, new GUIContent("Device Field Of View Gizmo"));
        }

        private void IndendProperty(SerializedProperty property)
        {
            using (new EditorGUI.IndentLevelScope())
            {
                EditorGUILayout.PropertyField(property);
            }
        }

        private void multiDeviceToggleWithVersionCheck(SerializedProperty property)
        {
            // this is the minimum service version that supports Multiple devices
            LEAP_VERSION minimumServiceVersion = new LEAP_VERSION { major = 5, minor = 3, patch = 6 };

            if (!ServerStatus.IsServiceVersionValid(minimumServiceVersion) && property.enumValueIndex == (int)LeapServiceProvider.MultipleDeviceMode.Specific)
            {
                property.enumValueIndex = (int)LeapServiceProvider.MultipleDeviceMode.Disabled;
                Debug.LogWarning(String.Format("Your current tracking service does not support 'Multiple Device Mode' = 'Specific' (min version is {0}.{1}.{2}). Please update your service: https://developer.leapmotion.com/tracking-software-download", minimumServiceVersion.major, minimumServiceVersion.minor, minimumServiceVersion.patch));
            }

            EditorGUILayout.PropertyField(property);
        }


        private void drawSerialNumberToggle(SerializedProperty property)
        {
            List<string> SerialNumbers = ServerStatus.GetSerialNumbers().ToList();

            if (SerialNumbers.Count == 0)
            {
                EditorGUILayout.HelpBox("There are no devices connected. Connect a device to use the Multiple Device Mode 'Specific'", MessageType.Warning);
                return;
            }

            // check whether the current SpecificSerialNumber is an empty string
            if (String.IsNullOrEmpty(property.stringValue))
            {
                _chosenDeviceIndex = EditorGUILayout.Popup("Specific Serial Number", 0, new List<string>() { "Select an available Serial Number" }.Concat(SerialNumbers).ToArray());

                if (_chosenDeviceIndex > 0)
                {
                    property.stringValue = SerialNumbers[_chosenDeviceIndex - 1];
                }
                return;
            }

            // try to find the specificSerialNumber in the list of available serial numbers
            _chosenDeviceIndex = SerialNumbers.FindIndex(x => x.Contains(property.stringValue));
            if (_chosenDeviceIndex == -1 || _chosenDeviceIndex >= SerialNumbers.Count)
            {
                // if it couldn't find the specificSerialNumber, display it at the end of the list with 'not available' behind it
                _chosenDeviceIndex = EditorGUILayout.Popup("Specific Serial Number", SerialNumbers.Count, SerialNumbers.Append(property.stringValue + " (not available)").ToArray());
            }
            else
            {
                // display the dropdown with all available serial numbers, selecting the specificSerialNumber
                _chosenDeviceIndex = EditorGUILayout.Popup("Specific Serial Number", _chosenDeviceIndex, SerialNumbers.ToArray());
            }

            // check whether the chosenDeviceIndex is within range of the list of serial numbers
            // It isn't in case a serial number with 'not available' was selected
            if (_chosenDeviceIndex < SerialNumbers.Count)
            {
                // assign the valid chosen serial number to the specificSerialNumber
                property.stringValue = SerialNumbers[_chosenDeviceIndex];
            }
        }

        private LeapServiceProvider LeapServiceProvider
        {
            get
            {

                if (this._leapServiceProvider != null)
                {
                    return this._leapServiceProvider;
                }
                else
                {
                    this._leapServiceProvider = this.target.GetComponent<LeapServiceProvider>();

                    return this._leapServiceProvider;
                }
            }
        }
    }
}