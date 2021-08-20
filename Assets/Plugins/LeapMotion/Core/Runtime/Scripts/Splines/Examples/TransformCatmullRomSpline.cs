/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2020.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using Leap.Unity.Animation;
using Leap.Unity.RuntimeGizmos;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.Splines {

  public class TransformCatmullRomSpline : MonoBehaviour, IRuntimeGizmoComponent {

    private const int RESOLUTION = 12;

    public Transform A;
    public Transform B;
    public Transform C;
    public Transform D;

    public GameObject poseEvaluationObj = null;
    private GameObject[] _evalObjCopies = new GameObject[RESOLUTION + 1];

    public bool fullPoseSpline = false;

    public Color color = Color.white;

    private HermiteSpline3? _spline = null;
    private HermiteQuaternionSpline? _qSpline = null;

    void Update() {
      if (!fullPoseSpline) {
        Vector3 a = A.position, b = B.position, c = C.position, d = D.position;
        _spline = CatmullRom.ToCHS(a, b, c, d);
      }
      else {
        Pose a = A.ToPose(), b = B.ToPose(), c = C.ToPose(), d = D.ToPose();
        _spline = CatmullRom.ToCHS(a.position, b.position, c.position, d.position);
        _qSpline = CatmullRom.ToQuaternionCHS(a.rotation, b.rotation,
                                              c.rotation, d.rotation);

        if (poseEvaluationObj != null) {
          float incr = 1f / RESOLUTION;
          var t = 0f;
          _evalObjCopies[0] = poseEvaluationObj;
          for (int i = 0; i <= RESOLUTION; i++) {
            var obj = _evalObjCopies[i];

            if (obj == null) {
              obj = Instantiate(poseEvaluationObj);
              obj.transform.parent = poseEvaluationObj.transform.parent;
              _evalObjCopies[i] = obj;
            }

            obj.transform.position = _spline.Value.PositionAt(t);
            obj.transform.rotation = _qSpline.Value.RotationAt(t);

            t += incr;
          }
        }
      }
    }
    
    public void OnDrawRuntimeGizmos(RuntimeGizmoDrawer drawer) {
      drawer.color = color;

      if (!_spline.HasValue || (fullPoseSpline && !_qSpline.HasValue)) return;

      int resolution = 16;
      float incr = 1f / resolution;
      Vector3? lastPos = null;
      for (float t = 0; t <= 1f; t += incr) {
        var pos = _spline.Value.PositionAt(t);
        if (fullPoseSpline) {
          var rot = _qSpline.Value.RotationAt(t);

          drawer.DrawPose(new Pose(pos, rot), 0.01f);
        }

        if (lastPos.HasValue) {
          drawer.DrawLine(lastPos.Value, pos);
        }

        lastPos = pos;
      }
    }

  }

}
