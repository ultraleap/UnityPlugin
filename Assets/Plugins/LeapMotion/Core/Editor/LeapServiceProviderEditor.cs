/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2020.                                   *
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

namespace Leap.Unity {

  [CustomEditor(typeof(LeapServiceProvider))]
  public class LeapServiceProviderEditor : CustomEditorBase<LeapServiceProvider> {

    internal const float INTERACTION_VOLUME_MODEL_IMPORT_SCALE_FACTOR = 0.001f;

    protected Quaternion deviceRotation = Quaternion.identity;
    protected bool isVRProvider = false;

    protected Vector3 controllerOffset = Vector3.zero;

    private const float LMC_BOX_RADIUS = 0.45f;
    private const float LMC_BOX_WIDTH = 0.965f;
    private const float LMC_BOX_DEPTH = 0.6671f;

    private Mesh _rigelInteractionZoneMesh;
    private Material _rigelInteractionMaterial;
    private readonly Vector3 _rigelInteractionZoneMeshOffset = new Vector3(0.0523f, 0, 0.005f);

    private LeapServiceProvider _leapServiceProvider;
    private Controller _leapController;


    protected override void OnEnable() {

      base.OnEnable();

      ParseRigelInteractionMeshData();

      specifyCustomDecorator("_frameOptimization", frameOptimizationWarning);

      specifyConditionalDrawing("_frameOptimization",
                                (int)LeapServiceProvider.FrameOptimizationMode.None,
                                "_physicsExtrapolation",
                                "_physicsExtrapolationTime");

      specifyConditionalDrawing("_physicsExtrapolation",
                                (int)LeapServiceProvider.PhysicsExtrapolationMode.Manual,
                                "_physicsExtrapolationTime");

      deferProperty("_workerThreadProfiling");
    }

    private void frameOptimizationWarning(SerializedProperty property) {
      var mode = (LeapServiceProvider.FrameOptimizationMode)property.intValue;
      string warningText;

      switch (mode) {
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

    public override void OnInspectorGUI() {

#if UNITY_2019_3_OR_NEWER
      // Easily tracking VR-enabled-or-not requires an XR package installed, so remove this warning for now.
#else
      if (UnityEditor.PlayerSettings.virtualRealitySupported && !isVRProvider) {
        EditorGUILayout.HelpBox(
          "VR support is enabled. If your Leap is mounted to your headset, you should be "
          + "using LeapXRServiceProvider instead of LeapServiceProvider. (If your Leap "
          + "is not mounted to your headset, you can safely ignore this warning.)",
          MessageType.Warning);
      }
#endif

      base.OnInspectorGUI();
    }

    public virtual void OnSceneGUI() {

      switch (GetSelectedInteractionVolume()) {
        case LeapServiceProvider.InteractionVolumeVisualization.None:
          break;
        case LeapServiceProvider.InteractionVolumeVisualization.LeapMotionController:
          DrawLeapMotionControllerInteractionZone(LMC_BOX_WIDTH, LMC_BOX_DEPTH, LMC_BOX_RADIUS, Color.white);
          break;
        case LeapServiceProvider.InteractionVolumeVisualization.Rigel:
          DrawRigelInteractionZoneMesh();
          break;
        case LeapServiceProvider.InteractionVolumeVisualization.Automatic:
          DetectConnectedDevice();
          break;
        default:
          break;
      }

    }

    private void ParseRigelInteractionMeshData() {

      if (_rigelInteractionZoneMesh == null) {
        _rigelInteractionZoneMesh = (Mesh)AssetDatabase.LoadAssetAtPath(Path.Combine("Assets", "UnityModules", "Assets", "Plugins", "LeapMotion", "Core", "Models", "Rigel-interaction-cone-placeholder.obj"), typeof(Mesh));

        // There is a be where the asset database appears to cache changes to assets, so loading an
        // asset again causes it to retain the flipped normals and will then reflip them. There appears
        // to be no way to clear this cache, so we check the first normal in the mesh to see if we
        // need to flip it.
        if (_rigelInteractionZoneMesh.normals[0].ApproxEquals(new Vector3(-1.0f, 0.1f, 0)))
          ReverseNormals();
      }

      if (_rigelInteractionMaterial == null) {
        _rigelInteractionMaterial = (Material)AssetDatabase.LoadAssetAtPath(Path.Combine("Assets", "UnityModules", "Assets", "Plugins", "LeapMotion", "Core", "Materials", "RigelInteractionVolume.mat"), typeof(Material));
      }

    }

    private void ReverseNormals() {
      Vector3[] normals = _rigelInteractionZoneMesh.normals;
      for (int i = 0; i < normals.Length; i++)
        normals[i] = -normals[i];
      _rigelInteractionZoneMesh.normals = normals;
    }

    private LeapServiceProvider LeapServiceProvider {
      get {

        if (this._leapServiceProvider != null) { 
          return this._leapServiceProvider;
        }
        else {
          this._leapServiceProvider = this.target.GetComponent<LeapServiceProvider>();

          return this._leapServiceProvider;
        }
      }
    }

    private Controller LeapController {
      get {

        if (this._leapController != null) {
          return this._leapController;
        }
        else
        {
          this._leapController = LeapServiceProvider?.GetLeapController();

          if (this._leapController != null) {
            this._leapController.Device += _leapController_DeviceChanged;
            this._leapController.DeviceLost += _leapController_DeviceChanged;
          }

          return this._leapController;
        }
      }
    }

    private void _leapController_DeviceChanged(object sender, DeviceEventArgs e) {
      EditorWindow view = EditorWindow.GetWindow<SceneView>();
      view.Repaint();
    }

    private void DetectConnectedDevice() {

      if (LeapController?.Devices?.Count == 1)
      {
        if (LeapController.Devices.First().Type == Device.DeviceType.TYPE_RIGEL) {
          DrawRigelInteractionZoneMesh();
        }
        else if (LeapController.Devices.First().Type == Device.DeviceType.TYPE_PERIPHERAL) {
          DrawLeapMotionControllerInteractionZone(LMC_BOX_WIDTH, LMC_BOX_DEPTH, LMC_BOX_RADIUS, Color.white);
        }
      }
    }

    private LeapServiceProvider.InteractionVolumeVisualization? GetSelectedInteractionVolume() {

      return LeapServiceProvider?.SelectedInteractionVolumeVisualization;
    }

    private void DrawRigelInteractionZoneMesh() {

      if (_rigelInteractionMaterial != null && _rigelInteractionZoneMesh != null) {
        _rigelInteractionMaterial.SetPass(0);
        Graphics.DrawMeshNow(_rigelInteractionZoneMesh, Matrix4x4.TRS(_rigelInteractionZoneMeshOffset, Quaternion.Euler(-90, 0, 0), Vector3.one * 0.001f));
      }
    }

    private void DrawLeapMotionControllerInteractionZone(float box_width, float box_depth, float box_radius, Color interactionZoneColor) {

      Color previousColor = Handles.color;
      Handles.color = interactionZoneColor;

      Vector3 origin = target.transform.TransformPoint(controllerOffset);
      getLocalGlobalPoint(-1, 1, 1, box_width, box_depth, box_radius, out Vector3 local_top_left, out Vector3 top_left);
      getLocalGlobalPoint(1, 1, 1, box_width, box_depth, box_radius, out Vector3 local_top_right, out Vector3 top_right);
      getLocalGlobalPoint(-1, 1, -1, box_width, box_depth, box_radius, out Vector3 local_bottom_left, out Vector3 bottom_left);
      getLocalGlobalPoint(1, 1, -1, box_width, box_depth, box_radius, out Vector3 local_bottom_right, out Vector3 bottom_right);

      Handles.DrawAAPolyLine(origin, top_left);
      Handles.DrawAAPolyLine(origin, top_right);
      Handles.DrawAAPolyLine(origin, bottom_left);
      Handles.DrawAAPolyLine(origin, bottom_right);

      drawControllerEdge(origin, local_top_left, local_top_right, box_radius);
      drawControllerEdge(origin, local_bottom_left, local_top_left, box_radius);
      drawControllerEdge(origin, local_bottom_left, local_bottom_right, box_radius);
      drawControllerEdge(origin, local_bottom_right, local_top_right, box_radius);

      drawControllerArc(origin, local_top_left, local_bottom_left, local_top_right,
                          local_bottom_right, box_radius);
      drawControllerArc(origin, local_top_left, local_top_right, local_bottom_left,
                          local_bottom_right, box_radius);

      Handles.color = previousColor;
    }

    private void getLocalGlobalPoint(int x, int y, int z, float box_width, float box_depth, float box_radius, out Vector3 local, out Vector3 global) {

      local = deviceRotation * new Vector3(x * box_width, y * box_radius, z * box_depth);
      global = target.transform.TransformPoint(controllerOffset
                                               + box_radius * local.normalized);
    }

    private void drawControllerEdge(Vector3 origin,
                                    Vector3 edge0, Vector3 edge1,
                                    float box_radius) {
      Vector3 right_normal = target.transform
                                   .TransformDirection(Vector3.Cross(edge0, edge1));
      float right_angle = Vector3.Angle(edge0, edge1);

      Handles.DrawWireArc(origin, right_normal, target.transform.TransformDirection(edge0),
                          right_angle, target.transform.lossyScale.x * box_radius);
    }

    private void drawControllerArc(Vector3 origin,
                                   Vector3 edgeA0, Vector3 edgeA1,
                                   Vector3 edgeB0, Vector3 edgeB1,
                                   float box_radius) {

      Vector3 faceA = target.transform.rotation * Vector3.Lerp(edgeA0, edgeA1, 0.5f);
      Vector3 faceB = target.transform.rotation * Vector3.Lerp(edgeB0, edgeB1, 0.5f);

      float resolutionIncrement = 1f / 50f;
      for (float i = 0f; i < 1f; i += resolutionIncrement) {
        Vector3 begin = Vector3.Lerp(faceA, faceB, i).normalized
                        * target.transform.lossyScale.x * box_radius;
        Vector3 end = Vector3.Lerp(faceA, faceB, i + resolutionIncrement).normalized
                      * target.transform.lossyScale.x * box_radius;

        Handles.DrawAAPolyLine(origin + begin, origin + end);
      }
    }

    internal enum enumaxis {
      X,
      Y,
      Z
    }
  }
}


