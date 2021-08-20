/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2020.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using Leap.Unity.Infix;
using UnityEngine;

namespace Leap.Unity.Geometry {

  public struct SphericalSection {

    public float angle;
    public float angleRad { get { return angle * Mathf.Deg2Rad; }}
    public float radius;
    public float minRadius;
    public Transform transform;
    public Matrix4x4? overrideMatrix;

    public Matrix4x4 matrix { get {
      if (overrideMatrix != null) {
        return overrideMatrix.Value;
      }
      if (transform != null) {
        return transform.WorldFromLocal();
      }
      return Matrix4x4.identity;
    }}

    public SphericalSection(float angle, float radius, float minRadius = 0,
      Transform transform = null)
    {
      this.angle = angle; this.radius = radius; this.minRadius = minRadius;
      this.transform = transform;
      this.overrideMatrix = null;
    }

    /// <summary> Converts a sample point from the unit cube (XYZ [-1, 1]) to
    /// a sample point in the spherical section. X and Y are converted from the 
    /// unit square [-1, 1] to the unit circle for the circule section of the
    /// cone section, and Z is used to sample from the base vertex out to the
    /// maximum section radius. </summary>
    public Vector3 SampleFromUnitCube(Vector3 unitCubePoint) {
      var circlePoint = Swizzle.Swizzle.xy(unitCubePoint).Square2Circle();
      var circlePointMag = circlePoint.magnitude;

      var zFrac = unitCubePoint.z.Map(-1f, 1f, 0f, 1f);
      var rForward = zFrac.Map(0f, 1f, minRadius, radius) * Vector3.forward;
      
      var rot = Quaternion.identity;
      var circleAngle = 0f;
      if (circlePointMag > 0f) {
        var circleDir = circlePoint / circlePointMag;
        circleAngle = Vector3.up.SignedAngle(circleDir, -Vector3.forward);
      }
      rot = Quaternion.AngleAxis(circleAngle, -Vector3.forward) * rot;
      
      var coneAngle = circlePointMag.Map(0f, 1f, 0f, angle / 2f);
      rot = Quaternion.AngleAxis(coneAngle, rot.GetRight()) * rot;

      return matrix.MultiplyPoint3x4(rot * rForward);
    }

    public void Draw(Drawer drawer, Color? color = null,
      Matrix4x4? overrideMatrix = null)
    {
      var useMatrix = overrideMatrix ?? this.transform.WorldFromLocal();
      var section = this;
      section.overrideMatrix = useMatrix;

      var unitBox = Box.unit;
      var useColor = drawer.color = color ?? Color.white;
      var alpha = useColor.a;
      unitBox.DrawLines(divisions: 16, drawLineFunc: (a, b) => {
        var a2 = section.SampleFromUnitCube(a);
        var b2 = section.SampleFromUnitCube(b);
        drawer.Line(a2, b2);
      });

      var rect = new Rect(center: Vector3.forward, radii: Vector3.one);
      var prevR = 1f;
      for (var r = 0.8f; r > 0f; r -= 0.15f) {
        drawer.color = useColor.WithAlpha(r * r * alpha);
        rect.radii = Vector3.one * r;
        rect.DrawLines(divisions: 16, drawLineFunc: (a, b) => {
          var a2 = section.SampleFromUnitCube(a);
          var b2 = section.SampleFromUnitCube(b);
          drawer.Line(a2, b2);
        });
        for (var n0 = -1; n0 <= 1; n0 += 1) {
          for (var n1 = -1; n1 <= 1; n1 += 1) {
            var s = new Vector3(n0 * prevR, n1 * prevR, 1f);
            var t = new Vector3(n0 *     r, n1 *     r, 1f);
            drawer.Line(section.SampleFromUnitCube(s),
                        section.SampleFromUnitCube(t));
          }
        }
        prevR = r;
      }
    }
    
  }

}
