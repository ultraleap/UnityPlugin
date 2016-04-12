/******************************************************************************\
* Copyright (C) Leap Motion, Inc. 2011-2014.                                   *
* Leap Motion proprietary. Licensed under Apache 2.0                           *
* Available at http://www.apache.org/licenses/LICENSE-2.0.html                 *
\******************************************************************************/

using UnityEditor;
using UnityEngine;
using System.Collections;
using Leap;

namespace Leap.Unity {
  [CustomEditor(typeof(LeapHandController))]
  public class LeapHandControllerEditor : CustomEditorBase {
    private const float BOX_RADIUS = 0.45f;
    private const float BOX_WIDTH = 0.965f;
    private const float BOX_DEPTH = 0.6671f;

    private LeapHandController controller;

    protected override void OnEnable() {
      base.OnEnable();

      controller = target as LeapHandController;
    }

    public void OnSceneGUI() {
      Vector3 origin = controller.transform.TransformPoint(Vector3.zero);

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

      drawControllerArc(origin, local_top_left, local_bottom_left, local_top_right, local_bottom_right, -Vector3.forward);
      drawControllerArc(origin, local_top_left, local_top_right, local_bottom_left, local_bottom_right, -Vector3.right);
    }

    private void getLocalGlobalPoint(int x, int y, int z, out Vector3 local, out Vector3 global) {
      local = new Vector3(x * BOX_WIDTH, y * BOX_RADIUS, z * BOX_DEPTH);
      global = controller.transform.TransformPoint(BOX_RADIUS * local.normalized);
    }

    private void drawControllerEdge(Vector3 origin,
                                    Vector3 edge0, Vector3 edge1) {
      Vector3 right_normal = controller.transform.TransformDirection(Vector3.Cross(edge0, edge1));
      float right_angle = Vector3.Angle(edge0, edge1);
      Handles.DrawWireArc(origin, right_normal,
                          controller.transform.TransformDirection(edge0),
                          right_angle, controller.transform.lossyScale.x * BOX_RADIUS);
    }

    private void drawControllerArc(Vector3 origin,
                                    Vector3 edgeA0, Vector3 edgeA1,
                                    Vector3 edgeB0, Vector3 edgeB1,
                                    Vector3 direction) {
      Vector3 faceA = Vector3.Lerp(edgeA0, edgeA1, 0.5f);
      Vector3 faceB = Vector3.Lerp(edgeB0, edgeB1, 0.5f);

      Vector3 depth_normal = controller.transform.TransformDirection(direction);
      float angle = Vector3.Angle(faceA, faceB);
      Handles.DrawWireArc(origin, depth_normal,
                          controller.transform.TransformDirection(faceA),
                          angle, controller.transform.lossyScale.x * BOX_RADIUS);
    }
  }
}
