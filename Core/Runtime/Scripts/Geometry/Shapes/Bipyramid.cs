/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2020.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using UnityEngine;

namespace Leap.Unity.Geometry {

  public struct Bipyramid {

    public Vector3 a;
    public Vector3 b;
    public int polySegments;
    public float lengthFraction;
    public float? absoluteRadius;
    public float? radiusFraction;
    public Transform transform;
    private Matrix4x4? overrideMatrix;
    public Matrix4x4 matrix { get {
      if (overrideMatrix != null) { return overrideMatrix.Value; }
      if (transform != null) { return transform.localToWorldMatrix; }
      return Matrix4x4.identity;
    }}
    public float radius { get {
      if (absoluteRadius != null) { return absoluteRadius.Value; }
      if (radiusFraction != null) { return (b - a).magnitude * radiusFraction.Value; }
      else { return (b - a).magnitude * 0.125f; }
    }}

    public Bipyramid(Vector3 a, Vector3 b, int polySegments = 6,
      float lengthFraction = 0.5f, float? radiusFraction = null,
      float? absoluteRadius = null, Transform transform = null,
      Matrix4x4? overrideMatrix = null)
    {
      this.a = a;
      this.b = b;
      this.polySegments = polySegments;
      this.lengthFraction = lengthFraction;
      this.radiusFraction = radiusFraction;
      this.absoluteRadius = absoluteRadius;
      this.transform = transform;
      this.overrideMatrix = overrideMatrix;
    }

    /// <summary> Rig model bone defaults for a bipyramid. Can override any of
    /// the parameters like a normal bipyramid constructor. </summary>
    public static Bipyramid ModelBone(Vector3 a, Vector3 b, int polySegments = 4,
      float lengthFraction = 0.38f, float? radiusFraction = 0.0125f,
      float? absoluteRadius = null, Transform transform = null,
      Matrix4x4? overrideMatrix = null)
    {
      return new Bipyramid(a, b, polySegments: polySegments,
        lengthFraction: lengthFraction, radiusFraction: radiusFraction,
        absoluteRadius: absoluteRadius, transform: transform,
        overrideMatrix: overrideMatrix);
    }

    /// <summary> Thin directed arrow, good for adding directionality to an edge
    /// between two points. Can override any of the parameters like a normal
    /// bipyramid constructor. </summary>
    public static Bipyramid ThinArrow(Vector3 a, Vector3 b, int polySegments = 4,
      float lengthFraction = 0.3f, float? radiusFraction = null,
      float? absoluteRadius = 0.005f, Transform transform = null,
      Matrix4x4? overrideMatrix = null)
    {
      return new Bipyramid(a, b, polySegments: polySegments,
        lengthFraction: lengthFraction, radiusFraction: radiusFraction,
        absoluteRadius: absoluteRadius, transform: transform,
        overrideMatrix: overrideMatrix);
    }

    /// <summary> Arrowhead defaults for a bipyramid. Can override any of
    /// the parameters like a normal bipyramid constructor. </summary>
    public static Bipyramid Arrowhead(Vector3 a, Vector3 b, int polySegments = 6,
      float lengthFraction = 0.38f, float? radiusFraction = 0.16f,
      float? absoluteRadius = null, Transform transform = null,
      Matrix4x4? overrideMatrix = null)
    {
      return new Bipyramid(a, b, polySegments: polySegments,
        lengthFraction: lengthFraction, radiusFraction: radiusFraction,
        absoluteRadius: absoluteRadius, transform: transform,
        overrideMatrix: overrideMatrix);
    }

    public void DrawLines(System.Action<Vector3, Vector3> drawLine) {
      var matrix = this.matrix;
      var radius = this.radius;
      var localLength = (b - a).magnitude;
      if (localLength == 0f) { localLength = 0.0001f; }
      var localDirection = (b - a) / localLength;

      var circle = new Geometry.Circle(
        center: a + localDirection * localLength * lengthFraction,
        direction: localDirection,
        radius: radius,
        overrideMatrix: matrix
      );
      var startPos = matrix.MultiplyPoint3x4(a);
      var endPos = matrix.MultiplyPoint3x4(b);

      foreach (var point in circle.Points(polySegments)) {
        drawLine(startPos, point);
        drawLine(point, endPos);
      }
      foreach (var edge in circle.Segments(polySegments)) {
        drawLine(edge.a, edge.b);
      }
    }

  }

}
