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
        protected Quaternion deviceRotation = Quaternion.identity;
        protected bool isVRProvider = false;

        protected Vector3 controllerOffset = Vector3.zero;

        private const float LMC_BOX_RADIUS = 0.45f;
        private const float LMC_BOX_WIDTH = 0.965f;
        private const float LMC_BOX_DEPTH = 0.6671f;

        private LeapServiceProvider _leapServiceProvider;
        private Controller _leapController;

        private List<string> _serialNumbers;
        private int _chosenDeviceIndex;

        private VisualFOV _visualFOV;
        private LeapFOVInfos leapFOVInfos;
        private Mesh optimalFOVMesh;
        private Mesh noTrackingFOVMesh;
        private Mesh maxFOVMesh;

        protected override void OnEnable()
        {

            base.OnEnable();

            LoadFOVData();

            specifyConditionalDrawing("FOV_Visualization",
                                        "OptimalFOV_Visualization",
                                        "NoTrackingFOV_Visualization",
                                        "MaxFOV_Visualization");

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

                // deviceOrigin is set if camera follows a transform
                if (xrProvider.deviceOffsetMode == LeapXRServiceProvider.DeviceOffsetMode.Transform && xrProvider.deviceOrigin != null)
                {
                    targetTransform = xrProvider.deviceOrigin;
                }
            }

            switch (GetSelectedInteractionVolume())
            {
                case LeapServiceProvider.InteractionVolumeVisualization.None:
                    if (target.transform.Find("DeviceModel") != null)
                    {
                        GameObject.DestroyImmediate(target.transform.Find("DeviceModel").gameObject);
                    }
                    break;
                case LeapServiceProvider.InteractionVolumeVisualization.LeapMotionController:
                    DrawTrackingDevice(targetTransform, "Leap Motion Controller");
                    DrawInteractionZone(targetTransform);
                    break;
                case LeapServiceProvider.InteractionVolumeVisualization.StereoIR170:
                    DrawTrackingDevice(targetTransform, "Stereo IR 170");
                    DrawInteractionZone(targetTransform);
                    break;
                case LeapServiceProvider.InteractionVolumeVisualization.Device_3Di:
                    DrawTrackingDevice(targetTransform, "3Di");
                    DrawInteractionZone(targetTransform);
                    break;
                case LeapServiceProvider.InteractionVolumeVisualization.Automatic:
                    DetectConnectedDevice(targetTransform);
                    break;
                default:
                    break;
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
            
            if (LeapController?.Devices?.Count >= 1)
            {
                Device currentDevice = target.CurrentDevice;
                if (currentDevice == null || (target.CurrentMultipleDeviceMode == LeapServiceProvider.MultipleDeviceMode.Specific && currentDevice.SerialNumber != target.SpecificSerialNumber))
                {
                    foreach (Device d in LeapController.Devices)
                    {
                        if (d.SerialNumber.Contains(target.SpecificSerialNumber))
                        {
                            currentDevice = d;
                            break;
                        }
                    }
                }

                if(currentDevice == null || (target.CurrentMultipleDeviceMode == LeapServiceProvider.MultipleDeviceMode.Specific && currentDevice.SerialNumber != target.SpecificSerialNumber))
                {
                    if (targetTransform.Find("DeviceModel") != null)
                    {
                        GameObject.DestroyImmediate(targetTransform.Find("DeviceModel").gameObject);
                    }
                    return;
                }

                Device.DeviceType deviceType = currentDevice.Type;
                if (deviceType == Device.DeviceType.TYPE_RIGEL || deviceType == Device.DeviceType.TYPE_SIR170)
                {
                    DrawTrackingDevice(targetTransform, "Stereo IR 170");
                    DrawInteractionZone(targetTransform);
                    return;
                }
                else if (deviceType == Device.DeviceType.TYPE_3DI)
                {
                    DrawTrackingDevice(targetTransform, "3Di");
                    DrawInteractionZone(targetTransform);
                    return;
                }
                else if (deviceType == Device.DeviceType.TYPE_PERIPHERAL)
                {
                    DrawTrackingDevice(targetTransform, "Leap Motion Controller");
                    DrawInteractionZone(targetTransform);
                    return;
                }
            }

            // if no devices connected, no serial number selected or the connected device type isn't matching one of the above,
            // delete any device model that is currently displayed
            if (targetTransform.Find("DeviceModel") != null)
            {
                GameObject.DestroyImmediate(targetTransform.Find("DeviceModel").gameObject);
            }
        }

        private LeapServiceProvider.InteractionVolumeVisualization? GetSelectedInteractionVolume()
        {

            return LeapServiceProvider?.SelectedInteractionVolumeVisualization;
        }

        private void DrawTrackingDevice(Transform targetTransform, string deviceType)
        {
            LeapXRServiceProvider xrProvider = target as LeapXRServiceProvider;
            Transform deviceModelParent = target.transform.Find("DeviceModel");
            if(deviceModelParent == null)
            {
                deviceModelParent = new GameObject("DeviceModel").transform;
                deviceModelParent.SetParent(target.transform, false);
                deviceModelParent.gameObject.AddComponent<UnityEngine.Animations.ParentConstraint>();
            }

            UnityEngine.Animations.ParentConstraint parentConstraint = deviceModelParent.GetComponent<UnityEngine.Animations.ParentConstraint>();
            UnityEngine.Animations.ConstraintSource constraintSource = new UnityEngine.Animations.ConstraintSource();
            constraintSource.sourceTransform = targetTransform;
            constraintSource.weight = 1;
            if (parentConstraint.sourceCount > 0)
            {
                parentConstraint.RemoveSource(0);
                parentConstraint.AddSource(constraintSource);
            }
            else
            {
                parentConstraint.AddSource(constraintSource);
            }
            

            //deviceModelParent.SetWorldPose(targetTransform.ToWorldPose());

            if (deviceModelParent.childCount > 0)
            {
                var child = deviceModelParent.GetChild(0).gameObject;

                // if the name is the device type and the FOV mesh exists if needed,
                // this object was already the same type last frame, and doesn't need to be re instantiated
                if(child.name == deviceType + "(Clone)" && (!target.FOV_Visualization || optimalFOVMesh != null))
                {
                    // rotation and translation should be updated to fit the deviceOffsets specified in the XRServiceProvider, 
                    // if the provider is an XRServiceProvider
                    if (xrProvider != null && xrProvider.deviceOffsetMode != LeapXRServiceProvider.DeviceOffsetMode.Transform)
                    {
                        parentConstraint.SetTranslationOffset(0, new Vector3(0, xrProvider.deviceOffsetYAxis, xrProvider.deviceOffsetZAxis));
                        parentConstraint.SetRotationOffset(0, new Vector3(-90 - xrProvider.deviceTiltXAxis, 180, 0));
                    }
                    return;
                }

                GameObject.DestroyImmediate(child);
            }

            LeapFOVInfo info = null;
            GameObject newDevice = null;
            foreach (var leapInfo in leapFOVInfos.SupportedDevices)
            {
                if (leapInfo.Name == deviceType)
                {
                    info = leapInfo;
                    newDevice = Instantiate(Resources.Load("TrackingDevices/" + deviceType)) as GameObject;
                    break;
                }
            }
            if (info != null)
            {
                SetDeviceInfo(info);
            }
            else
            {
                Debug.LogError("Tried to load invalid device type: " + deviceType);
            }

            // newDevice needs to be rotated and translated to fit the deviceOffsets specified in the XRServiceProvider, 
            // if the provider is an XRServiceProvider
            if (xrProvider != null && xrProvider.deviceOffsetMode != LeapXRServiceProvider.DeviceOffsetMode.Transform)
            {
                parentConstraint.SetTranslationOffset(0, new Vector3(0, xrProvider.deviceOffsetYAxis, xrProvider.deviceOffsetZAxis));
                parentConstraint.SetRotationOffset(0, new Vector3(-90 - xrProvider.deviceTiltXAxis, 180, 0));
            }

            newDevice.transform.SetParent(deviceModelParent, false);
            newDevice.transform.localScale = Vector3.one * 0.01f;


            parentConstraint.locked = true;
            parentConstraint.constraintActive = true;

        }

        private void DrawInteractionZone(Transform targetTransform)
        {
            if(!target.FOV_Visualization)
            {
                return;
            }

            _visualFOV.UpdateFOVS();

            optimalFOVMesh = _visualFOV._optimalFOVMesh;
            noTrackingFOVMesh = _visualFOV._noTrackingFOVMesh;
            maxFOVMesh = _visualFOV._maxFOVMesh;

            Transform deviceModelParent = target.transform.Find("DeviceModel");
            if (deviceModelParent == null)
            {
                return;
            }


            if (target.OptimalFOV_Visualization && optimalFOVMesh != null)
            {
                Material mat = Resources.Load("OptimalFOVMat_Volume") as Material;
                mat.SetPass(0);

                Graphics.DrawMeshNow(optimalFOVMesh, deviceModelParent.localToWorldMatrix *
                       Matrix4x4.Scale(Vector3.one * 0.01f));
            }
            if (target.NoTrackingFOV_Visualization && noTrackingFOVMesh != null)
            {
                Material mat = Resources.Load("UntrackableFOVMat_Volume") as Material;
                mat.SetPass(0);

                Graphics.DrawMeshNow(noTrackingFOVMesh, deviceModelParent.localToWorldMatrix *
                       Matrix4x4.Scale(Vector3.one * 0.01f));
            }
            if (target.MaxFOV_Visualization && maxFOVMesh != null)
            {
                Material mat = Resources.Load("MaxFOVMat_Volume") as Material;
                mat.SetPass(0);

                Graphics.DrawMeshNow(maxFOVMesh, deviceModelParent.localToWorldMatrix *
                       Matrix4x4.Scale(Vector3.one * 0.01f));
            }
        }

        
        private void LoadFOVData()
        {
            //Debug.Log(Resources.Load<TextAsset>("SupportedTrackingDevices").text);
            leapFOVInfos = Newtonsoft.Json.JsonConvert.DeserializeObject<LeapFOVInfos>(Resources.Load<TextAsset>("SupportedTrackingDevices").text);

            if (_visualFOV == null) _visualFOV = new VisualFOV();
        }

        public void SetDeviceInfo(LeapFOVInfo leapInfo)
        {
            _visualFOV.HorizontalFOV = leapInfo.HorizontalFOV;
            _visualFOV.VerticalFOV = leapInfo.VerticalFOV;
            _visualFOV.MinDistance = leapInfo.MinDistance;
            _visualFOV.MaxDistance = leapInfo.MaxDistance;
            _visualFOV.OptimalMaxDistance = leapInfo.OptimalDistance;
        }

        public class LeapFOVInfos
        {
            public List<LeapFOVInfo> SupportedDevices;
        }

        public class LeapFOVInfo
        {
            public string Name;
            public float HorizontalFOV;
            public float VerticalFOV;
            public float OptimalDistance;
            public float MinDistance;
            public float MaxDistance;
        }
    }
}