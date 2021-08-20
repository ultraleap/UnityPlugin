/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2020.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using Leap.Unity.Attributes;
using Leap.Unity.RuntimeGizmos;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace Leap.Unity.Animation.Examples {

  [ExecuteInEditMode]
  [AddComponentMenu("")]
  public class TransformPoseSpline : MonoBehaviour, IRuntimeGizmoComponent {

    [QuickButton("Spawn Default", "spawnDefaultSource0")]
    public Transform pose0Source;

    [QuickButton("Spawn Default", "spawnDefaultSource1")]
    public Transform pose1Source;

    [Header("Debug View")]
    [Range(0f, 1f)]
    public float renderPoseAtT = 0f;

    private HermitePoseSpline? maybePoseSpline;

    private void Update() {
      if (pose0Source != null && pose1Source != null) {

        var pose0 = pose0Source.ToPose();
        var pose1 = pose1Source.ToPose();

        var movement0 = Movement.identity;
        if (pose0Source.childCount > 0) {
          var pose0SourceChild = pose0Source.GetChild(0);
          movement0 = new Movement(pose0, pose0SourceChild.ToPose(), 0.1f);
        }

        var movement1 = Movement.identity;
        if (pose1Source.childCount > 0) {
          var pose1SourceChild = pose1Source.GetChild(0);
          movement1 = new Movement(pose1, pose1SourceChild.ToPose(), 0.1f);
        }

        maybePoseSpline = new HermitePoseSpline(pose0, pose1,
                                                movement0, movement1);
      }
    }

    private void spawnDefaultSource0() { spawnDefaultSource(false); }
    private void spawnDefaultSource1() { spawnDefaultSource(true); }
    private void spawnDefaultSource(bool isSecondSource) {
      var newPoseSourceObj = new GameObject("Pose Source " + (isSecondSource ? 1 : 0));
      newPoseSourceObj.transform.parent = this.transform;
      newPoseSourceObj.transform.ResetLocalTransform();
      newPoseSourceObj.transform.localPosition += Vector3.right * 0.2f
                                                  * (isSecondSource ? 1 : -1);
      if (isSecondSource) { pose1Source = newPoseSourceObj.transform; }
      else { pose0Source = newPoseSourceObj.transform; }
    }

    public void OnDrawRuntimeGizmos(RuntimeGizmoDrawer drawer) {
      if (maybePoseSpline.HasValue) {
        var spline = maybePoseSpline.Value;

        drawer.color = LeapColor.brown.WithAlpha(0.4f);

        Vector3? prevPos = null;
        int numSteps = 32;
        int drawPosePer = 8, counter = 0;
        float tStep = 1f / numSteps;
        for (float t = 0f; t <= 1f; t += tStep) {
          var pose = spline.PoseAt(t);

          if (counter % drawPosePer == 0) {
            drawer.DrawPose(pose, 0.02f);
          }

          if (prevPos.HasValue) {
            drawer.DrawLine(prevPos.Value, pose.position);
          }

          prevPos = pose.position;
          counter++;
        }

        var renderPose = spline.PoseAt(renderPoseAtT);
        var len = 0.06f;
        var thick = 0.01f;
        drawer.PushMatrix();
        drawer.matrix = Matrix4x4.TRS(renderPose.position, renderPose.rotation, Vector3.one);
        drawer.color = Color.red;
        drawer.DrawCube(Vector3.right * ((len / 2f) + (thick / 2f)), new Vector3(len, thick, thick));
        drawer.color = Color.green;
        drawer.DrawCube(Vector3.up * ((len / 2f) + (thick / 2f)), new Vector3(thick, len, thick));
        drawer.color = Color.blue;
        drawer.DrawCube(Vector3.forward * ((len / 2f) + (thick / 2f)), new Vector3(thick, thick, len));
      }
    }
  }

}
