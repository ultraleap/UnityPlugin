/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2020.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using System;
using Leap.Unity.Infix;
using Leap.Unity.RuntimeGizmos;
using UnityEngine;

namespace Leap.Unity.Geometry {

  using UnityRect = UnityEngine.Rect;

  [System.Serializable]
  public struct Circle {

    public Transform transform;
    public Vector3 center;
    private Direction3 direction;
    public float radius;

    private Matrix4x4? _overrideMatrix;
    public Matrix4x4? overrideMatrix {
      get { return _overrideMatrix; }
      set { _overrideMatrix = value; }  
    }

    #region Constructors

    public Circle(LocalCircle localCircle, Transform withTransform)
      : this(localCircle.center, localCircle.direction, localCircle.radius,
             withTransform) { }

    public Circle(float radius = 0.5f, Component transformSource = null)
      : this(default(Vector3), default(Direction3), radius, transformSource) { }

    public Circle(Vector3 center = default(Vector3),
      Direction3 direction = default(Direction3), float radius = 0.5f,
      Component transformSource = null)
    {
      this.transform = (transformSource == null ? null : transformSource.transform);
      this.center = center;
      this.direction = direction;
      this.radius = radius;
      this._overrideMatrix = null;
    }

    public Circle(Vector3 center = default(Vector3),
      Direction3 direction = default(Direction3), float radius = 0.5f,
      Transform transform = null) : this(center, direction, radius,
        (Component)transform) { }

    public Circle(Vector3 center = default(Vector3),
      Direction3 direction = default(Direction3), float radius = 0.5f,
      Matrix4x4? overrideMatrix = null) : this(center, direction, radius,
        (Component)null)
    {
      this._overrideMatrix = overrideMatrix;
    }

    #endregion

    #region Accessors

    /// <summary>
    /// Local-to-world matrix for this Circle.
    /// </summary>
    public Matrix4x4 matrix {
      get {
        if (overrideMatrix != null) {
          return overrideMatrix.Value * Matrix4x4.Translate(center);
        }
        else if (transform == null) {
          return Matrix4x4.Translate(center);
        }
        return transform.localToWorldMatrix * Matrix4x4.Translate(center);
      }
    }

    /// <summary>
    /// The world position of the center of this Circle (read only). This is dependent on
    /// the state of its Transform if it has one, as well as its defined local-space
    /// center position.
    /// </summary>
    public Vector3 position {
      get {
        return this.matrix.MultiplyPoint3x4(Vector3.zero);
      }
    }

    #endregion

    #region Debug Rendering

    public void DrawRuntimeGizmos(RuntimeGizmoDrawer drawer) {
      Matrix4x4 m = Matrix4x4.identity;
      if (transform != null) {
        m = transform.localToWorldMatrix;
      }

      drawer.PushMatrix();
      drawer.matrix = m;

      drawer.DrawWireArc(
        center: center,
        normal: direction,
        radialStartDirection: direction.Vec().GetPerpendicular(),
        radius: radius,
        fractionOfCircleToDraw: 1f,
        numCircleSegments: 44
      );

      drawer.PopMatrix();
    }

    public void Draw(Drawer drawer, Color? color = null) {
      if (color != null) { drawer.color = color.Value; }
      DrawWireArc(
        center: center,
        normal: direction,
        radialStartDirection: direction.Vec().GetPerpendicular(),
        radius: radius,
        fractionOfCircleToDraw: 1f,
        numCircleSegments: 44,
        matrix: transform == null ?
          Matrix4x4.identity : transform.localToWorldMatrix,
        lineDrawingFunc: drawer.Line
      );
    }

    public void DrawLines(Action<Vector3, Vector3> lineDrawingFunc) {
      DrawWireArc(
        center: center,
        normal: direction,
        radialStartDirection: direction.Vec().GetPerpendicular(),
        radius: radius,
        fractionOfCircleToDraw: 1f,
        numCircleSegments: 44,
        matrix: transform == null ?
          Matrix4x4.identity : transform.localToWorldMatrix,
        lineDrawingFunc: lineDrawingFunc
      );
    }

    // Welp, RuntimeGizmos might need an overhaul to accept arbitrary drawing
    // functions, because this is super useful.
    public static void DrawWireArc(Vector3 center, Vector3 normal,
                                   float radius, int numCircleSegments,
                                   Action<Vector3, Vector3> lineDrawingFunc,
                                   float fractionOfCircleToDraw = 1.0f,
                                   Matrix4x4? matrix = null,
                                   Vector3? radialStartDirection = null) {
      if (!matrix.HasValue) {
        matrix = Matrix4x4.identity;
      }
      if (!radialStartDirection.HasValue) {
        radialStartDirection = normal.GetPerpendicular();
      }
      
      normal = normal.normalized;
      Vector3 radiusVector = radialStartDirection.Value.normalized * radius;
      Vector3 nextVector;
      int numSegmentsToDraw = (int)(numCircleSegments * fractionOfCircleToDraw);
      Quaternion rotator = Quaternion.AngleAxis(360f / numCircleSegments, normal);
      for (int i = 0; i < numSegmentsToDraw; i++) {
        nextVector = rotator * radiusVector;
        lineDrawingFunc(
          matrix.Value.MultiplyPoint3x4(center + radiusVector),
          matrix.Value.MultiplyPoint3x4(center + nextVector)
        );
        radiusVector = nextVector;
      }
    }

    #endregion

    #region Enumerators

    public CirclePointEnumerator Points(int numPoints) {
      return new CirclePointEnumerator(this, numPoints);
    }

    public CircleSegmentEnumerator Segments(int numLines) {
      return new CircleSegmentEnumerator(this, numLines);
    }

    public struct CirclePointEnumerator {
      Circle circle; int numPoints; int idx;
      Vector3 startRadiusVector; Quaternion rotator;
      Vector3? radiusVector;
      public CirclePointEnumerator(Circle circle, int numPoints) {
        this.circle = circle;
        this.numPoints = Mathf.Max(3, numPoints);
        this.idx = -1;
        this.startRadiusVector = ((Vector3)circle.direction).GetPerpendicular()
          * circle.radius;
        this.rotator = Quaternion.AngleAxis(360f / numPoints, circle.direction);
        this.radiusVector = null;
      }
      public Vector3 Current { get {
        return circle.matrix.MultiplyPoint3x4(radiusVector.GetValueOrDefault());
      }}
      public bool MoveNext() {
        if (idx == numPoints - 1) { return false; }
        idx += 1;
        if (radiusVector == null) { radiusVector = startRadiusVector; }
        else { radiusVector = rotator * radiusVector; }
        return true;
      }
      public CirclePointEnumerator GetEnumerator() { return this; }
    }

    public struct CircleSegmentEnumerator {
      CirclePointEnumerator points;
      Vector3? firstPoint;
      Vector3? lastPointReturned;
      bool reachedLastSegment;
      public CircleSegmentEnumerator(Circle circle, int numLines) {
        numLines = Mathf.Max(1, numLines);
        this.points = new CirclePointEnumerator(circle, numLines + 1);
        firstPoint = null;
        lastPointReturned = null;
        reachedLastSegment = false;
      }
      public LocalSegment3 Current { get {
        if (reachedLastSegment) {
          return new LocalSegment3(lastPointReturned.Value, firstPoint.Value);
        }
        if (!lastPointReturned.HasValue) {
          return new LocalSegment3(points.Current, points.Current);
        }
        else {
          return new LocalSegment3(lastPointReturned.Value, points.Current);
        }
      }}
      public bool MoveNext() {
        if (!lastPointReturned.HasValue) {
          if (!points.MoveNext()) { return false; }
          else { firstPoint = points.Current; }
        }
        lastPointReturned = points.Current;

        if (!points.MoveNext()) {
          if (!reachedLastSegment) {
            reachedLastSegment = true;
            return true;
          }
          return false;
        }
        return true;
      }
      public CircleSegmentEnumerator GetEnumerator() { return this; }
    }

    #endregion

  }

  public static class CircleExtensions {
    
    public static Vector3 GetPerpendicular(this Vector3 v) {
      return Utils.Perpendicular(v);
    }

  }

}
