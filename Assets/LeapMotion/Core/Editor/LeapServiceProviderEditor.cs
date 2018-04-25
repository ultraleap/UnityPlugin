/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2018.                                 *
 * Leap Motion proprietary and confidential.                                  *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

using UnityEngine;
using UnityEditor;

namespace Leap.Unity {

  [CustomEditor(typeof(LeapServiceProvider))]
  public class LeapServiceProviderEditor : CustomEditorBase<LeapServiceProvider> {

    protected Quaternion deviceRotation = Quaternion.identity;
    protected bool isVRProvider = false;

    protected Vector3 controllerOffset = Vector3.zero;

    private const float BOX_RADIUS = 0.45f;
    private const float BOX_WIDTH = 0.965f;
    private const float BOX_DEPTH = 0.6671f;

    protected override void OnEnable() {
      base.OnEnable();

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
      if (UnityEditor.PlayerSettings.virtualRealitySupported && !isVRProvider) {
        EditorGUILayout.HelpBox(
          "VR support is enabled. If your Leap is mounted to your headset, you should be "
          + "using LeapXRServiceProvider instead of LeapServiceProvider. (If your Leap "
          + "is not mounted to your headset, you can safely ignore this warning.)",
          MessageType.Warning);
      }

      base.OnInspectorGUI();
    }

    public virtual void OnSceneGUI() {
      Vector3 origin = target.transform.TransformPoint(controllerOffset);

      Vector3 local_top_left, top_left, local_top_right, top_right,
              local_bottom_left, bottom_left, local_bottom_right, bottom_right;
      getLocalGlobalPoint(-1, 1, 1, out local_top_left, out top_left);
      getLocalGlobalPoint(1, 1, 1, out local_top_right, out top_right);
      getLocalGlobalPoint(-1, 1, -1, out local_bottom_left, out bottom_left);
      getLocalGlobalPoint(1, 1, -1, out local_bottom_right, out bottom_right);

      Handles.DrawLine(origin, top_left);
      Handles.DrawLine(origin, top_right);
      Handles.DrawLine(origin, bottom_left);
      Handles.DrawLine(origin, bottom_right);

      drawControllerEdge(origin, local_top_left, local_top_right);
      drawControllerEdge(origin, local_bottom_left, local_top_left);
      drawControllerEdge(origin, local_bottom_left, local_bottom_right);
      drawControllerEdge(origin, local_bottom_right, local_top_right);

      drawControllerArc(origin, local_top_left, local_bottom_left, local_top_right,
                        local_bottom_right);
      drawControllerArc(origin, local_top_left, local_top_right, local_bottom_left,
                        local_bottom_right);
    }

    private void getLocalGlobalPoint(int x, int y, int z,
                                     out Vector3 local, out Vector3 global) {
      local = deviceRotation * new Vector3(x * BOX_WIDTH, y * BOX_RADIUS, z * BOX_DEPTH);
      global = target.transform.TransformPoint(controllerOffset
                                               + BOX_RADIUS * local.normalized);
    }

    private void drawControllerEdge(Vector3 origin,
                                    Vector3 edge0, Vector3 edge1) {
      Vector3 right_normal = target.transform
                                   .TransformDirection(Vector3.Cross(edge0, edge1));
      float right_angle = Vector3.Angle(edge0, edge1);

      Handles.DrawWireArc(origin, right_normal, target.transform.TransformDirection(edge0),
                          right_angle, target.transform.lossyScale.x * BOX_RADIUS);
    }

    private void drawControllerArc(Vector3 origin,
                                   Vector3 edgeA0, Vector3 edgeA1,
                                   Vector3 edgeB0, Vector3 edgeB1) {
      Vector3 faceA = target.transform.rotation * Vector3.Lerp(edgeA0, edgeA1, 0.5f);
      Vector3 faceB = target.transform.rotation * Vector3.Lerp(edgeB0, edgeB1, 0.5f);

      float resolutionIncrement = 1f / 50f;
      for (float i = 0f; i < 1f; i += resolutionIncrement) {
        Vector3 begin = Vector3.Lerp(faceA, faceB, i).normalized
                        * target.transform.lossyScale.x * BOX_RADIUS;
        Vector3 end = Vector3.Lerp(faceA, faceB, i + resolutionIncrement).normalized 
                      * target.transform.lossyScale.x * BOX_RADIUS;

        Handles.DrawLine(origin + begin, origin + end);
      }
    }
  }
}
