/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2021.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using LeapInternal;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace Leap.Unity
{

    [CustomEditor(typeof(LeapServiceProvider))]
    public class LeapServiceProviderEditor : CustomEditorBase<LeapServiceProvider>
    {

        internal const float INTERACTION_VOLUME_MODEL_IMPORT_SCALE_FACTOR = 0.001f;

        protected Quaternion deviceRotation = Quaternion.identity;
        protected bool isVRProvider = false;

        protected Vector3 controllerOffset = Vector3.zero;

        private const float LMC_BOX_RADIUS = 0.45f;
        private const float LMC_BOX_WIDTH = 0.965f;
        private const float LMC_BOX_DEPTH = 0.6671f;

        private Mesh _stereoIR170InteractionZoneMesh;
        private Material _stereoIR170InteractionMaterial;
        private readonly Vector3 _stereoIR170InteractionZoneMeshOffset = new Vector3(0.0523f, 0, 0.005f);

        private LeapServiceProvider _leapServiceProvider;
        private Controller _leapController;

        private List<string> _serialNumbers;
        private int _chosenDeviceIndex;

        protected override void OnEnable()
        {

            base.OnEnable();

            ParseStereoIR170InteractionMeshData();

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
            deferProperty("_workerThreadProfiling");

            if (!(LeapServiceProvider is LeapXRServiceProvider))
            {
                addPropertyToFoldout("_trackingOptimization", "Advanced Options");
            }
            else
            {
                hideField("_trackingOptimization");
            }
            addPropertyToFoldout("_workerThreadProfiling", "Advanced Options");
            addPropertyToFoldout("_serverNameSpace", "Advanced Options");
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

        private void multiDeviceToggleWithVersionCheck(SerializedProperty property)
        {
            // this is the minimum service version that supports Multiple devices
            LEAP_VERSION minimumServiceVersion = new LEAP_VERSION { major = 5, minor = 3, patch = 6 };

            if (LeapController.IsConnected && !LeapController.CheckRequiredServiceVersion(minimumServiceVersion) && property.enumValueIndex == (int)LeapServiceProvider.MultipleDeviceMode.Specific)
            {
                property.enumValueIndex = (int)LeapServiceProvider.MultipleDeviceMode.Disabled;
                Debug.LogWarning(String.Format("Your current tracking service does not support 'Multiple Device Mode' = 'Specific' (min version is {0}.{1}.{2}). Please update your service: https://developer.leapmotion.com/tracking-software-download", minimumServiceVersion.major, minimumServiceVersion.minor, minimumServiceVersion.patch));
            }

            EditorGUILayout.PropertyField(property);
        }


        private void drawSerialNumberToggle(SerializedProperty property)
        {
            if (LeapController != null)
            {
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
        }


        public override void OnInspectorGUI()
        {

#if UNITY_2019_3_OR_NEWER
      // Easily tracking VR-enabled-or-not requires an XR package installed, so remove this warning for now.
#else
            if (UnityEditor.PlayerSettings.virtualRealitySupported && !isVRProvider)
            {
                EditorGUILayout.HelpBox(
                  "VR support is enabled. If your Leap is mounted to your headset, you should be "
                  + "using LeapXRServiceProvider instead of LeapServiceProvider. (If your Leap "
                  + "is not mounted to your headset, you can safely ignore this warning.)",
                  MessageType.Warning);
            }
#endif

            base.OnInspectorGUI();
        }

        public virtual void OnSceneGUI()
        {
            if (target == null)
            {
                return;
            }

            Transform targetTransform = target.transform;
            LeapXRServiceProvider xrProvider = target as LeapXRServiceProvider;
            if (xrProvider != null)
            {
                targetTransform = xrProvider.mainCamera.transform;

                if (xrProvider.deviceOrigin != null)
                {
                    targetTransform.InverseTransformPoint(xrProvider.deviceOrigin.position);
                }
            }

            switch (GetSelectedInteractionVolume())
            {
                case LeapServiceProvider.InteractionVolumeVisualization.None:
                    break;
                case LeapServiceProvider.InteractionVolumeVisualization.LeapMotionController:
                    DrawLeapMotionControllerInteractionZone(LMC_BOX_WIDTH, LMC_BOX_DEPTH, LMC_BOX_RADIUS, Color.white, targetTransform);
                    break;
                case LeapServiceProvider.InteractionVolumeVisualization.StereoIR170:
                    DrawStereoIR170InteractionZoneMesh(targetTransform);
                    break;
                case LeapServiceProvider.InteractionVolumeVisualization.Automatic:
                    DetectConnectedDevice(targetTransform);
                    break;
                default:
                    break;
            }

        }

        private void ParseStereoIR170InteractionMeshData()
        {

            if (_stereoIR170InteractionZoneMesh == null)
            {
                _stereoIR170InteractionZoneMesh = (Mesh)Resources.Load("StereoIR170-interaction-cone", typeof(Mesh));
            }

            if (_stereoIR170InteractionMaterial == null)
            {
                _stereoIR170InteractionMaterial = (Material)Resources.Load("StereoIR170InteractionVolume", typeof(Material));
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

        private Controller LeapController
        {
            get
            {

                if (this._leapController != null)
                {
                    return this._leapController;
                }
                else
                {
                    this._leapController = LeapServiceProvider?.GetLeapController();

                    if (this._leapController != null)
                    {
                        this._leapController.Device += _leapController_DeviceAdded;
                        this._leapController.DeviceLost += _leapController_DeviceLost;
                    }

                    return this._leapController;
                }
            }
        }

        private List<string> SerialNumbers
        {
            get
            {
                if (this._serialNumbers != null)
                {
                    return this._serialNumbers;
                }
                else
                {
                    this._serialNumbers = new List<string>();
                    List<Device> connectedDevices = LeapController.Devices;
                    foreach (Device d in connectedDevices)
                    {
                        this._serialNumbers.Add(d.SerialNumber);
                    }
                    return this._serialNumbers;
                }
            }
        }


        private void _leapController_DeviceAdded(object sender, DeviceEventArgs e)
        {
            SerialNumbers.Add(e.Device.SerialNumber);
            _leapController_DeviceChanged(sender, e);
        }

        private void _leapController_DeviceLost(object sender, DeviceEventArgs e)
        {
            SerialNumbers.Remove(e.Device.SerialNumber);
            _leapController_DeviceChanged(sender, e);
        }

        private void _leapController_DeviceChanged(object sender, DeviceEventArgs e)
        {
            if (!Application.isPlaying || EditorWindow.focusedWindow.GetType() == typeof(SceneView))
            {
                EditorWindow view = EditorWindow.GetWindow<SceneView>();
                view.Repaint();
            }

            // Repaint the inspector windows if the devices have changed
            Repaint();
        }

        private void DetectConnectedDevice(Transform targetTransform)
        {

            if (LeapController?.Devices?.Count == 1)
            {
                Device.DeviceType deviceType = LeapController.Devices.First().Type;
                if (deviceType == Device.DeviceType.TYPE_RIGEL || deviceType == Device.DeviceType.TYPE_SIR170 || deviceType == Device.DeviceType.TYPE_3DI)
                {
                    DrawStereoIR170InteractionZoneMesh(targetTransform);
                }
                else if (deviceType == Device.DeviceType.TYPE_PERIPHERAL)
                {
                    DrawLeapMotionControllerInteractionZone(LMC_BOX_WIDTH, LMC_BOX_DEPTH, LMC_BOX_RADIUS, Color.white, targetTransform);
                }
            }
        }

        private LeapServiceProvider.InteractionVolumeVisualization? GetSelectedInteractionVolume()
        {

            return LeapServiceProvider?.SelectedInteractionVolumeVisualization;
        }

        private void DrawStereoIR170InteractionZoneMesh(Transform targetTransform)
        {
            if (_stereoIR170InteractionMaterial != null && _stereoIR170InteractionZoneMesh != null)
            {
                _stereoIR170InteractionMaterial.SetPass(0);

                Graphics.DrawMeshNow(_stereoIR170InteractionZoneMesh,
                   targetTransform.localToWorldMatrix *
                   Matrix4x4.TRS(controllerOffset + _stereoIR170InteractionZoneMeshOffset, deviceRotation * Quaternion.Euler(-90, 0, 0), Vector3.one * 0.001f));
            }
        }

        private void DrawLeapMotionControllerInteractionZone(float box_width,
            float box_depth,
            float box_radius,
            Color interactionZoneColor,
            Transform targetTransform)
        {

            Color previousColor = Handles.color;
            Handles.color = interactionZoneColor;

            Vector3 origin = targetTransform.TransformPoint(controllerOffset);
            Vector3 local_top_left, top_left, local_top_right, top_right, local_bottom_left, bottom_left, local_bottom_right, bottom_right;
            getLocalGlobalPoint(-1, 1, 1, box_width, box_depth, box_radius, out local_top_left, out top_left, targetTransform);
            getLocalGlobalPoint(1, 1, 1, box_width, box_depth, box_radius, out local_top_right, out top_right, targetTransform);
            getLocalGlobalPoint(-1, 1, -1, box_width, box_depth, box_radius, out local_bottom_left, out bottom_left, targetTransform);
            getLocalGlobalPoint(1, 1, -1, box_width, box_depth, box_radius, out local_bottom_right, out bottom_right, targetTransform);

            Handles.DrawAAPolyLine(origin, top_left);
            Handles.DrawAAPolyLine(origin, top_right);
            Handles.DrawAAPolyLine(origin, bottom_left);
            Handles.DrawAAPolyLine(origin, bottom_right);

            drawControllerEdge(origin, local_top_left, local_top_right, box_radius, targetTransform);
            drawControllerEdge(origin, local_bottom_left, local_top_left, box_radius, targetTransform);
            drawControllerEdge(origin, local_bottom_left, local_bottom_right, box_radius, targetTransform);
            drawControllerEdge(origin, local_bottom_right, local_top_right, box_radius, targetTransform);

            drawControllerArc(origin, local_top_left, local_bottom_left, local_top_right,
                              local_bottom_right, box_radius, targetTransform);
            drawControllerArc(origin, local_top_left, local_top_right, local_bottom_left,
                              local_bottom_right, box_radius, targetTransform);

            Handles.color = previousColor;
        }

        private void getLocalGlobalPoint(int x, int y, int z, float box_width, float box_depth,
            float box_radius, out Vector3 local, out Vector3 global, Transform targetTransform)
        {

            local = deviceRotation * new Vector3(x * box_width, y * box_radius, z * box_depth);
            global = targetTransform.TransformPoint(controllerOffset
                                                     + box_radius * local.normalized);
        }

        private void drawControllerEdge(Vector3 origin,
                                        Vector3 edge0, Vector3 edge1,
                                        float box_radius,
                                        Transform targetTransform)
        {

            Vector3 right_normal = targetTransform.TransformDirection(Vector3.Cross(edge0, edge1));
            float right_angle = Vector3.Angle(edge0, edge1);

            Handles.DrawWireArc(origin, right_normal, targetTransform.TransformDirection(edge0),
                                right_angle, targetTransform.lossyScale.x * box_radius);
        }

        private void drawControllerArc(Vector3 origin,
                                       Vector3 edgeA0, Vector3 edgeA1,
                                       Vector3 edgeB0, Vector3 edgeB1,
                                       float box_radius,
                                       Transform targetTransform)
        {

            Vector3 faceA = targetTransform.rotation * Vector3.Lerp(edgeA0, edgeA1, 0.5f);
            Vector3 faceB = targetTransform.rotation * Vector3.Lerp(edgeB0, edgeB1, 0.5f);

            float resolutionIncrement = 1f / 50f;
            for (float i = 0f; i < 1f; i += resolutionIncrement)
            {
                Vector3 begin = Vector3.Lerp(faceA, faceB, i).normalized
                                * targetTransform.lossyScale.x * box_radius;
                Vector3 end = Vector3.Lerp(faceA, faceB, i + resolutionIncrement).normalized
                              * targetTransform.lossyScale.x * box_radius;

                Handles.DrawAAPolyLine(origin + begin, origin + end);
            }
        }
    }
}