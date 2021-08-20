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
  public struct Sphere {

    public Transform transform;
    public Vector3 center;
    public float radius;
    public Matrix4x4? overrideMatrix;

    #region Constructors

    public Sphere(LocalSphere localSphere, Transform withTransform)
      : this(localSphere.center, localSphere.radius, withTransform) { }

    public Sphere(float radius = 0.10f, Component transformSource = null)
      : this(default(Vector3), radius, transformSource) { }

    public Sphere(float radius = 0.10f)
      : this(default(Vector3), radius, null) { }

    public Sphere(float radius = 0.10f, Matrix4x4? overrideMatrix = null)
      : this(default(Vector3), radius, null, overrideMatrix) { }

    public Sphere(Vector3 center = default(Vector3), float radius = 0.10f) :
      this(center, radius, (Component)null) { }

    public Sphere(Vector3 center = default(Vector3), float radius = 0.10f,
      Component transformSource = null, Matrix4x4? overrideMatrix = null)
    {
      this.transform = (transformSource == null ? null : transformSource.transform);
      this.center = center;
      this.radius = radius;
      this.overrideMatrix = overrideMatrix;
    }

    public Sphere(Vector3 center = default(Vector3), float radius = 0.10f,
      Transform transform = null) : this(center, radius, (Component)transform) { }

    public Sphere(Sphere other) : this(other.center, other.radius, other.transform) { }

    #endregion

    #region Accessors

    /// <summary>
    /// Local-to-world matrix for this Sphere.
    /// </summary>
    public Matrix4x4 matrix {
      get {
        if (overrideMatrix != null) {
          return overrideMatrix.Value * Matrix4x4.Translate(center);
        }
        if (transform == null) {
          return Matrix4x4.Translate(center);
        }
        return transform.localToWorldMatrix * Matrix4x4.Translate(center);
      }
    }

    /// <summary>
    /// The world position of the center of this sphere (read only). This is dependent on
    /// the state of its Transform if it has one, as well as its defined local-space
    /// center position.
    /// </summary>
    public Vector3 position {
      get {
        return this.matrix.MultiplyPoint3x4(Vector3.zero);
      }
    }

    #endregion

    #region Chain Calls

    public Sphere WithCenter(Vector3 center) {
      var copy = new Sphere(this); copy.center = center; return copy;
    }

    #endregion

    #region Collision

    public bool Overlaps(Box box) {
      return Collision.DoesOverlap(this, box);
    }

    /// <summary>
    /// Returns the distance between the closest points on the Rect and the Sphere,
    /// or 0 if the two overlap.
    /// </summary>
    public float DistanceTo(Rect rect) {
      return Collision.DistanceBetween(this, rect);
    }

    public bool Overlaps(Rect rect) {
      return Collision.DoesOverlap(this, rect);
    }

    #endregion

    #region Debug Rendering

    public void DrawLines(Action<Vector3, Vector3> lineDrawingFunc,
                          int latitudinalDivisions = 5,
                          int longitudinalDivisions = 5,
                          int numCircleSegments = 7,
                          Matrix4x4? matrixOverride = null) {
      Matrix4x4 m = Matrix4x4.identity;
      if (transform != null) {
        m = transform.localToWorldMatrix;
      }
      if (matrixOverride.HasValue) {
        m = matrixOverride.Value;
      }

      // Vector3 center = m.MultiplyPoint3x4(this.center);
      // float radius = m.MultiplyPoint3x4(Vector3.right).magnitude * this.radius;
      // Vector3 x = m.MultiplyVector(Vector3.right);
      // Vector3 y = m.MultiplyVector(Vector3.up);
      //Vector3 z = m.MultiplyVector(Vector3.forward); // unused
      var center = this.center;
      var radius = this.radius;
      var x = Vector3.right;
      var y = Vector3.up;

      // Wire lat-long sphere
      int latDiv = latitudinalDivisions;
      float latAngle = 180f / latDiv; float accumLatAngle = 0f;
      int lonDiv = longitudinalDivisions;
      float lonAngle = 180f / lonDiv;
      Quaternion lonRot = Quaternion.AngleAxis(lonAngle, y);
      Vector3 lonNormal = x;
      for (int i = 0; i < latDiv; i++) {
        accumLatAngle += latAngle;
        Circle.DrawWireArc(
          center: center + y * Mathf.Cos(accumLatAngle * Mathf.Deg2Rad) * radius,
          normal: y,
          radius: Mathf.Sin(accumLatAngle * Mathf.Deg2Rad) * radius,
          numCircleSegments: numCircleSegments,
          lineDrawingFunc: lineDrawingFunc,
          fractionOfCircleToDraw: 1.0f,
          radialStartDirection: x,
          matrix: m
        );
      }
      for (int i = 0; i < lonDiv; i++) {
        Circle.DrawWireArc(
          center: center,
          normal: lonNormal,
          radius: radius,
          numCircleSegments: numCircleSegments,
          lineDrawingFunc: lineDrawingFunc,
          fractionOfCircleToDraw: 1.0f,
          radialStartDirection: y,
          matrix: m
        );
        lonNormal = lonRot * lonNormal;
      }
    }

    public void DrawRuntimeGizmos(RuntimeGizmoDrawer drawer) {
      Matrix4x4 m = Matrix4x4.identity;
      if (transform != null) {
        m = transform.localToWorldMatrix;
      }

      var origDrawerColor = drawer.color;

      Vector3 center = m.MultiplyPoint3x4(this.center);
      float radius = m.MultiplyPoint3x4(Vector3.right).magnitude * this.radius;
      Vector3 x = m.MultiplyVector(Vector3.right);
      Vector3 y = m.MultiplyVector(Vector3.up);
      //Vector3 z = m.MultiplyVector(Vector3.forward); // unused

      // Sphere
      drawer.color = drawer.color.WithAlpha(origDrawerColor.a * 0.05f);
      drawer.DrawSphere(center, radius);

      // Wire lat-long sphere
      drawer.color = drawer.color.WithAlpha(origDrawerColor.a * 0.2f);
      int latDiv = 6;
      float latAngle = 180f / latDiv; float accumLatAngle = 0f;
      int lonDiv = 6;
      float lonAngle = 180f / lonDiv;
      Quaternion lonRot = Quaternion.AngleAxis(lonAngle, y);
      Vector3 lonNormal = x;
      for (int i = 0; i < latDiv; i++) {
        accumLatAngle += latAngle;
        drawer.DrawWireArc(center: center + y * Mathf.Cos(accumLatAngle * Mathf.Deg2Rad) * radius,
                           normal: y,
                           radialStartDirection: x,
                           radius: Mathf.Sin(accumLatAngle * Mathf.Deg2Rad) * radius,
                           fractionOfCircleToDraw: 1.0f,
                           numCircleSegments: 22);
      }
      for (int i = 0; i < latDiv; i++) {
        drawer.DrawWireArc(center: center,
                           normal: lonNormal,
                           radialStartDirection: y,
                           radius: radius,
                           fractionOfCircleToDraw: 1.0f,
                           numCircleSegments: 22);
        lonNormal = lonRot * lonNormal;
      }

      drawer.color = origDrawerColor;
    }

    #endregion

  }

  public static class SphereExtensions {
    
    /// <summary>
    /// Defines a Sphere at this position with the argument radius.
    /// </summary>
    public static Sphere ToSphere(this Vector3 vec3, float radius) {
      return new Sphere(vec3, radius);
    }

  }

}
