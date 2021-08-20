/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2020.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using Leap.Unity.Infix;
using Leap.Unity.Swizzle;
using System;
using UnityEngine;

namespace Leap.Unity.Geometry {

  public static class Collision {

    #region Short-hand

    private static float Dot(Vector3 a, Vector3 b) {
      return Vector3.Dot(a, b);
    }

    private static float Dot(Vector2 a, Vector2 b) {
      return Vector2.Dot(a, b);
    }

    #endregion

    #region Box-Sphere

    public static bool DoesOverlap(Box box, Sphere sphere) {
      return DoesOverlap(sphere, box);
    }
    public static bool DoesOverlap(Sphere sphere, Box box) {
      var sphereCenter_world = sphere.matrix.MultiplyPoint3x4(Vector3.zero);
      var sphereCenter_box = box.matrix.inverse
                                .MultiplyPoint3x4(sphereCenter_world).Abs();

      float x = sphereCenter_box.x,
            y = sphereCenter_box.y,
            z = sphereCenter_box.z;

      float bx = box.radii.x.Abs(),
            by = box.radii.y.Abs(),
            bz = box.radii.z.Abs();

      float dx = x - Mathf.Min(bx, x),
            dy = y - Mathf.Min(by, y),
            dz = z - Mathf.Min(bz, z);

      if (dx == 0 && dy == 0 && dz == 0) return true;

      var boxSurfaceToSphereCenter_box = new Vector3(dx, dy, dz);
      var boxSurfaceToSphereCenter_world
        = box.matrix.MultiplyVector(boxSurfaceToSphereCenter_box);
      var boxSurfaceToSphereCenter_sphere
        = sphere.matrix.inverse.MultiplyVector(boxSurfaceToSphereCenter_world);

      return boxSurfaceToSphereCenter_sphere.sqrMagnitude < sphere.radius * sphere.radius;
    }

    #endregion

    #region Rect-Sphere

    /// <summary>
    /// Returns the distance between the closest points on the Rect and the Sphere,
    /// or 0 if the two overlap.
    /// </summary>
    public static float DistanceTo(Rect rect, Sphere sphere) {
      return DistanceBetween(sphere, rect);
    }
    /// <summary>
    /// Returns the distance between the closest points on the Rect and the Sphere,
    /// or 0 if the two overlap.
    /// </summary>
    public static float DistanceBetween(Sphere sphere, Rect rect) {
      var sphereCenter_world = sphere.matrix.MultiplyPoint3x4(Vector3.zero);
      var sphereCenter_rect = rect.matrix.inverse
                                  .MultiplyPoint3x4(sphereCenter_world).Abs();

      float x = sphereCenter_rect.x,
            y = sphereCenter_rect.y,
            z = sphereCenter_rect.z;

      float rx = rect.radii.x.Abs(),
            ry = rect.radii.y.Abs(),
            rz = 0f;

      float dx = x - Mathf.Min(rx, x),
            dy = y - Mathf.Min(ry, y),
            dz = z - Mathf.Min(rz, z);

      if (dx == 0 && dy == 0 && dz == 0) return 0f;

      var rectSurfaceToSphereCenter_rect = new Vector3(dx, dy, dz);
      var rectSurfaceToSphereCenter_world
        = rect.matrix.MultiplyVector(rectSurfaceToSphereCenter_rect);

      return Mathf.Max(0f, rectSurfaceToSphereCenter_world.magnitude - sphere.radius);
    }

    public static bool DoesOverlap(Rect rect, Sphere sphere) {
      return DoesOverlap(sphere, rect);
    }
    public static bool DoesOverlap(Sphere sphere, Rect rect) {
      var sphereCenter_world = sphere.matrix.MultiplyPoint3x4(Vector3.zero);
      var sphereCenter_rect = rect.matrix.inverse
                                  .MultiplyPoint3x4(sphereCenter_world).Abs();

      float x = sphereCenter_rect.x,
            y = sphereCenter_rect.y,
            z = sphereCenter_rect.z;

      float rx = rect.radii.x.Abs(),
            ry = rect.radii.y.Abs(),
            rz = 0f;

      float dx = x - Mathf.Min(rx, x),
            dy = y - Mathf.Min(ry, y),
            dz = z - Mathf.Min(rz, z);

      if (dx == 0 && dy == 0 && dz == 0) return true;

      var rectSurfaceToSphereCenter_rect = new Vector3(dx, dy, dz);
      var rectSurfaceToSphereCenter_world
        = rect.matrix.MultiplyVector(rectSurfaceToSphereCenter_rect);
      var rectSurfaceToSphereCenter_sphere
        = sphere.matrix.inverse.MultiplyVector(rectSurfaceToSphereCenter_world);

      return rectSurfaceToSphereCenter_sphere.sqrMagnitude < sphere.radius * sphere.radius;
    }

    #endregion

    #region Rect-Segment3

    //[ThreadStatic]
    //private static LocalSegment3[] _rectEdgesBuffer = new LocalSegment3[4];

    public static float Intersect(LocalSegment3 segment, Rect rect) {
      return Intersect(rect, segment);
    }
    public static float Intersect(Rect rect, LocalSegment3 segment) {
      Vector3 closestPointOnSegment_unused, closestPointOnRect_unused;
      return Intersect(rect, segment,
        out closestPointOnRect_unused, out closestPointOnSegment_unused);
    }
    public static float Intersect(LocalSegment3 segment, Rect rect,
                                  out Vector3 closestPointOnSegment,
                                  out Vector3 closestPointOnRect) {
      return Intersect(rect, segment, out closestPointOnRect, out closestPointOnSegment);
    }
    public static float Intersect(Rect rect, LocalSegment3 segment,
                                  out Vector3 closestPointOnRect,
                                  out Vector3 closestPointOnSegment) {
      float closestSqrDist = float.PositiveInfinity;
      closestPointOnRect = closestPointOnSegment = default(Vector3);

      Vector3 segmentA_rect;
      bool aProjectsInside = rect.ContainsProjectedPoint(segment.a, out segmentA_rect);
      Vector3 segmentB_rect;
      bool bProjectsInside = rect.ContainsProjectedPoint(segment.b, out segmentB_rect);

      // Test if the segment intersects with the rect plane and is inside the rect.
      var plane_rect = rect.ToLocalPlane();
      var segment_rect = new LocalSegment3(segmentA_rect, segmentB_rect);
      Vector3 pointOnPlane_rect; float amountAlongSegment; bool isLineCoplanar;
      var intersectsPlane = Intersect(plane_rect, segment_rect,
                                      out pointOnPlane_rect, out amountAlongSegment,
                                      out isLineCoplanar);
      if (intersectsPlane) {
        var absPointOnPlane_rect = pointOnPlane_rect.Abs();
        var absRadii = rect.radii.Abs();
        if (absPointOnPlane_rect.x <= absRadii.x && absPointOnPlane_rect.y <= absRadii.y) {
          closestPointOnRect = rect.matrix.MultiplyPoint3x4(pointOnPlane_rect);
          closestPointOnSegment = segment.Evaluate(amountAlongSegment);
          return 0f;
        }
      }

      // Must test the rect segments and plane-projection distances to find the closest
      // pair of points and return their squared-distance.
      
      // Segment point A <> plane, when A projects inside rect.
      if (aProjectsInside) {
        var testPointRect = rect.matrix.MultiplyPoint3x4(segmentA_rect.WithZ(0f));
        var testSqrDist = (testPointRect - segment.a).sqrMagnitude;
        if (testSqrDist < closestSqrDist) {
          closestSqrDist = testSqrDist;
          closestPointOnRect = testPointRect;
          closestPointOnSegment = segment.a;
        }
      }

      // Segment point B <> plane, when B projects inside rect.
      if (bProjectsInside) {
        var testPointRect = rect.matrix.MultiplyPoint3x4(segmentB_rect.WithZ(0f));
        var testSqrDist = (testPointRect - segment.b).sqrMagnitude;
        if (testSqrDist < closestSqrDist) {
          closestSqrDist = testSqrDist;
          closestPointOnRect = testPointRect;
          closestPointOnSegment = segment.b;
        }
      }

      if (!aProjectsInside || !bProjectsInside) {
        // Closest point segment <> {AB, BC, CD, DA}.
        foreach (var rectSegment in rect.segments) {
          Vector3 testClosestPointRectSegment;
          Vector3 testClosestPointSegment;
          float unusedT1, unusedT2;
          var testSqrDist = Intersect(rectSegment, segment,
                                    out unusedT1, out unusedT2,
                                    out testClosestPointRectSegment,
                                    out testClosestPointSegment);

          // Debug
          //RuntimeGizmos.RuntimeGizmoDrawer drawer;
          //if (RuntimeGizmos.RuntimeGizmoManager.TryGetGizmoDrawer(out drawer)) {
          //  drawer.DrawLine(testClosestPointRectSegment, testClosestPointSegment);
          //}

          if (testSqrDist < closestSqrDist) {
            closestSqrDist = testSqrDist;
            closestPointOnRect = testClosestPointRectSegment;
            closestPointOnSegment = testClosestPointSegment;
          }
        }
      }

      return closestSqrDist;
    }

    #endregion

    #region Plane-Segment3

    // https://stackoverflow.com/a/18543221 for line-plane intersection.

    /// <summary>
    /// Returns true if the line segment intersects with the plane, or false otherwise.
    /// 
    /// Specify output parameters to receive more detailed information about the
    /// intersection, such as the point at which it occurs and whether the line lies on
    /// the plane.
    /// </summary>
    public static bool Intersect(LocalPlane plane, LocalSegment3 line) {
      Vector3 pointOnPlane_unused;
      float amountAlongSegment_unused;
      bool isLineCoplanar_unused;
      return Intersect(plane, line,
                       out pointOnPlane_unused,
                       out amountAlongSegment_unused,
                       out isLineCoplanar_unused);
    }


    /// <summary>
    /// Returns true if the line segment intersects with the plane, or false otherwise.
    /// 
    /// If intersectInfiniteLine is specified, returns true if the line defined by the
    /// line segment intersects with the plane, or false otherwise.
    /// 
    /// If this method returns true, no out parameters are set to NaN, and reasonable
    /// defaults are chosen in edge-cases (such as a coplanar line). If this method
    /// returns false, some or all output parameters may have no reasonable value and
    /// are set to NaN.
    /// 
    /// pointOnPlane is the point along the line defined by the segment that intersects
    /// with the plane. If the line segment is parallel to the plane and _on_ the plane,
    /// pointOnPlane will be set to the center of the line segment as a convenience.
    /// Otherwise, it will be set to a Vector3 containing all NaNs.
    /// 
    /// amountAlongSegment is the normalized amount from A to B along the line segment
    /// that intersects with the plane. The line segment intersects with the plane only
    /// if this value is between 0 and 1 (inclusive). If the line segment is parallel to
    /// the plane and _on_ the plane, amountAlongSegment will be set to 0.5f. Otherwise,
    /// amountAlongSegment will be set to NaN.
    /// 
    /// isLineCoplanar is true if the line defined by the line segment is wholly on
    /// the plane, or false otherwise.
    /// </summary>
    public static bool Intersect(LocalPlane plane, LocalSegment3 line,
                                 out Vector3 pointOnPlane,
                                 out float amountAlongSegment,
                                 out bool isLineCoplanar,
                                 bool intersectInfiniteLine = false) {
      var planePosition = plane.position;
      var planeNormal = plane.normal;

      var a = line.a;
      var b = line.b;

      var u = b - a;
      var uDotN = planeNormal.Dot(u);
      if (uDotN.Abs() > float.Epsilon) {
        isLineCoplanar = false;

        var w = a - planePosition;
        amountAlongSegment = -(planeNormal.Dot(w)) / uDotN;
        pointOnPlane = a + u * amountAlongSegment;

        return intersectInfiniteLine
               || (amountAlongSegment >= 0f && amountAlongSegment <= 1f);
      }
      else {
        var pa = a - planePosition;
        if (planeNormal.Dot(pa).Abs() < float.Epsilon) {
          isLineCoplanar = true;
        }
        else {
          isLineCoplanar = false;
        }

        if (isLineCoplanar) {
          pointOnPlane = (a + b) / 2f;
          amountAlongSegment = 0.5f;
          return true;
        }
        else {
          pointOnPlane = new Vector3(float.NaN, float.NaN, float.NaN);
          amountAlongSegment = float.NaN;
          return false;
        }
      }
    }

    #endregion

    #region Segment-Segment

    /// <summary>
    /// Returns 2 times the signed triangle area. The result is positive if abc is
    /// counter-clockwise, negative if abc is clockwise, and zero if abc is degenerate.
    /// </summary>
    private static float Signed2DTriArea(Vector2 a, Vector2 b, Vector2 c) {
      return (a.x - c.x) * (b.y - c.y)
           - (a.y - c.y) * (b.x - c.x);
    }

    /// <summary>
    /// Returns whether seg1 and seg2 overlap. If they do, computes and outputs the
    /// intersection t value along seg1 and outputs the intersection point p. Otherwise,
    /// outputs default values.
    /// </summary>
    public static bool Intersect(LocalSegment2 seg1, LocalSegment2 seg2,
                                 out float t, out Vector2 p) {
      Vector2 a = seg1.a, b = seg1.b, c = seg2.a, d = seg2.b;

      // Sign of areas correspond to which side of ab the points c and d are.
      float a1 = Signed2DTriArea(a, b, d); // Compute winding of abd (+ or -).
      float a2 = Signed2DTriArea(a, b, c); // To intersect, must have opposite sign.

      // If c and d are on different sides of ab, areas have different signs.
      // Also verify that a1 and a2 are non-zero (areas are zero for degenerate triangles).
      if (a1 * a2 < 0f && a1 != 0f && a2 != 0f) {
        // Compute signs for a and b with respect to cd.
        float a3 = Signed2DTriArea(c, d, a); // Winding of cda (+ or -).
        // Since area is constant, a1 - a2 = a3 - a4, or a4 = a3 + a2 - a1
        // float a4 = Signed2DTriArea(c, d, b);
        float a4 = a3 + a2 - a1;
        // The points a and b are on different sides of cd if areas have different signs.
        if (a3 * a4 < 0f) {
          // The segments intersect. Find intersection point along L(t) = a + t * (b - a).
          // Given the height h1 of a over cd and height h2 of b over cd,
          // t = h1 / (h1 - h2) = (b*h1/2) / (b*h1/2 - b*h2/2) = a3 / (a3 - a4),
          // where b (the base of the triangles cda and cdb, i.e., the length of cd)
          // cancels out.
          t = a3 / (a3 - a4);
          p = a + t * (b - a);
          return true;
        }
      }

      // Segments don't intersect, or are collinear.
      t = default(float); p = default(Vector2);
      return false;
    }
    
    /// <summary>
    /// Computes the squared distance between two 3D line segments, outputting the amount
    /// along each segment (0 to 1) corresponding to the closest points (t1 and t2), and
    /// outputting the closest points themselves (c1 and c2).
    /// </summary>
    public static float Intersect(LocalSegment3 seg1,   LocalSegment3 seg2,
                                  out float     t1,     out float     t2,
                                  out Vector3   c1,     out Vector3   c2) {
      Vector3 d1 = seg1.b - seg1.a; // Direction vector of seg1.
      Vector3 d2 = seg2.b - seg2.a; // Direction vector of seg2.
      Vector3 r = seg1.a - seg2.a;
      float a = Dot(d1, d1); // Squared length of seg1.
      float e = Dot(d2, d2); // Squared length of seg2.
      float f = Dot(d2, r);

      // Check if either or both segments degenerate into points.
      if (a <= float.Epsilon && e <= float.Epsilon) {
        // Both segments degenerate into points.
        t1 = t2 = 0f;
        c1 = seg1.a; c2 = seg2.a;
        return Dot(c1 - c2, c1 - c2);
      }
      if (a <= float.Epsilon) {
        // First segment degenerates into a point.
        t1 = 0f;
        t2 = (f / e).Clamped01();
      }
      else {
        float c = Dot(d1, r);
        if (e <= float.Epsilon) {
          // Second segment degenerates into a point.
          t2 = 0f;
          t1 = (-c / a).Clamped01();
        }
        else {
          // General, non-degenerate case.
          float b = Dot(d1, d2);
          float denom = a*e - b*b; // Always non-negative.

          // If the segments are not parallel, compute the closest point on seg1 to seg2
          // and clamp to seg1. Otherwise, pick an arbitrary t1 (here 0).
          if (denom != 0f) {
            t1 = ((b*f - c*e) / denom).Clamped01();
          }
          else {
            t1 = 0f;
          }
          // Compute the point on seg2 closest to t1.
          float t2nom = b*t1 + f; // avoid dividing e until later.

          // If t2 in [0, 1], we're done. Otherwise, recompute t1 for the new value of
          // t2 and clamp to [0, 1].
          if (t2nom < 0f) {
            t2 = 0f;
            t1 = (-c / a).Clamped01();
          }
          else if (t2nom > e) {
            t2 = 1f;
            t1 = ((b - c) / a).Clamped01();
          }
          else {
            t2 = t2nom / e;
          }
        }
      }

      c1 = seg1.a + d1 * t1;
      c2 = seg2.a + d2 * t2;
      return (c1 - c2).sqrMagnitude;
    }

    #region ARCHIVE

    // 2D intersection:
    // https://stackoverflow.com/questions/563198/how-do-you-detect-where-two-line-segments-intersect
    // https://ideone.com/PnPJgb

    /// <summary>
    /// Returns true if the two segments are not parallel or colinear and intersect in 2D,
    /// ignoring the segments' Z components.
    /// </summary>
    public static bool Intersect2D(LocalSegment3 segment0, LocalSegment3 segment1,
                                   out Vector2 intersectionPoint,
                                   out float amountAlongSegment0,
                                   out float amountAlongSegment1) {

      Vector2 p = segment0.a, q = segment0.b, r = segment1.a, s = segment1.b;

      // t = (q − p) × s / (r × s)
      // u = (q − p) × r / (r × s)

      var denom = Fake2DCross(r, s);

      if (denom == 0) {
        // Lines are collinear or parallel.
        amountAlongSegment0 = float.NaN;
        amountAlongSegment1 = float.NaN;
        intersectionPoint = new Vector2(float.NaN, float.NaN);
        return false;
      }

      var tNumer = Fake2DCross(q - p, s);
      var uNumer = Fake2DCross(q - p, r);

      amountAlongSegment0 = tNumer / denom;
      amountAlongSegment1 = uNumer / denom;

      if (   amountAlongSegment0 < 0 || amountAlongSegment0 > 1
          || amountAlongSegment1 < 0 || amountAlongSegment1 > 1) {
        // Line segments do not intersect within their ranges.
        intersectionPoint = default(Vector2);
        return false;
      }

      intersectionPoint = p + r * amountAlongSegment0;
      return true;
    }

    private static float Fake2DCross(Vector2 a, Vector2 b) {
      return a.x * b.y - a.y * b.x;
    }

    #endregion

    #endregion

    #region Point-Segment

    /// <summary>
    /// Returns the squared-distance of the point p from the segment.
    /// </summary>
    public static float SqrDistPointSegment(LocalSegment3 segment, Vector3 p) {
      return SqrDistPointSegment(segment.a, segment.b, p);
    }
    /// <summary>
    /// Returns the squared-distance of the point c from the segment defined by a, b.
    /// </summary>
    public static float SqrDistPointSegment(Vector3 a, Vector3 b, Vector3 c) {
      Vector3 ab = b - a, ac = c - a, bc = c - b;
      float e = Dot(ac, ab);
      // Handle cases where C projects outside AB.
      if (e <= 0f) return Dot(ac, ac);
      float f = Dot(ab, ab);
      if (e >= f) return Dot(bc, bc);
      // Handle cases where C projects onto AB.
      return Dot(ac, ac) - e * e / f;
    }

    /// <summary>
    /// Returns the closest point to point p on the segment.
    /// </summary>
    public static Vector3 ClosestPtPointSegment(LocalSegment3 segment, Vector3 p) {
      float unusedT;
      return ClosestPtPointSegment(segment.a, segment.b, p, out unusedT);
    }
    /// <summary>
    /// Returns the closest point to point p on the segment. Also outputs the
    /// parameterization from 0 to 1 along ab that results in the closest point:
    /// closestPt(t) = a + t*(b - a).
    /// </summary>
    public static Vector3 ClosestPtPointSegment(LocalSegment3 segment, Vector3 p,
                                                out float t) {
      return ClosestPtPointSegment(segment.a, segment.b, p, out t);
    }
    /// <summary>
    /// Returns the closest point to point c on the segment ab. Also outputs the
    /// parameterization from 0 to 1 along ab that results in the closest point:
    /// closestPt(t) = a + t*(b - a).
    /// </summary>
    public static Vector3 ClosestPtPointSegment(Vector3 a, Vector3 b, Vector3 c,
                                                out float t) {
      var ab = b - a;
      // Project c onto ab, computing parameterized position of the point: a + t*(b - a)
      t = Dot(c - a, ab) / Dot(ab, ab);
      // If outside the segment, clamp the parameterization to the closest endpoint.
      if (t < 0f) t = 0f;
      if (t > 1f) t = 1f;
      // Compute and output the projected position from the clamped parameterization.
      return a + t * ab;
    }

    #endregion

  }

}
