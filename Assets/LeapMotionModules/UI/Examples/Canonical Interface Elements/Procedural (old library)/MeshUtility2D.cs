using UnityEngine;
using System;
using System.Collections.Generic;

public static class MeshUtility2D {

  private static List<IndexedPoint> _points = new List<IndexedPoint>();

  public static void Get2DConvexTriangulation(List<Vector2> points, List<int> triangles) {
    for (int i = 2; i < points.Count; i++) {
      triangles.Add(i);
      triangles.Add(i - 1);
      triangles.Add(0);
    }
  }

  public static void GetDelaunayTriangulation(List<Vector2> points, List<int> triangles) {
    throw new NotImplementedException();
  }

  public static void Get2DConcaveTriangulation(List<Vector2> points, List<int> triangles) {
    _points.Clear();
    for (int i = 0; i < points.Count; i++) {
      IndexedPoint point;
      point.position = points[i];
      point.index = _points.Count;
      _points.Add(point);
    }

    int index0 = 1;
    int lastIndex0 = 0;
    while (_points.Count > 2) {
      if (index0 == lastIndex0) {
        triangles.Clear();
        return;
      }

      int index1 = (index0 + 1) % _points.Count;
      int index2 = (index0 + 2) % _points.Count;

      IndexedPoint v0 = _points[index0];
      IndexedPoint v1 = _points[index1];
      IndexedPoint v2 = _points[index2];

      int edgeA = (index2 + 1) % _points.Count;
      int edgeB = (index0 - 1 + _points.Count) % _points.Count;
      IndexedPoint edgePointA = _points[edgeA];
      IndexedPoint edgePointB = _points[edgeB];

      bool doesIntersect = false;

      if (cross(v2.position - v1.position, v1.position - v0.position) <= 0) {
        doesIntersect = true;
      }

      if (_points.Count > 3) {
        if (doesEdgeIntersect(v1.position, v2.position, v0.position, edgePointA.position)) {
          doesIntersect = true;
        }

        if (doesEdgeIntersect(v1.position, v0.position, v2.position, edgePointB.position)) {
          doesIntersect = true;
        }

        while (edgeA != edgeB) {
          int edgeA1 = (edgeA + 1) % _points.Count;

          IndexedPoint s0 = _points[edgeA];
          IndexedPoint s1 = _points[edgeA1];

          if (Do2DSegmentsIntersect(s0.position, s1.position, v0.position, v2.position)) {
            doesIntersect = true;
            break;
          }

          edgeA = edgeA1;
        }
      }

      if (doesIntersect) {
        index0 = (index0 + 1) % _points.Count;
        continue;
      }

      triangles.Add(v0.index);
      triangles.Add(v1.index);
      triangles.Add(v2.index);

      _points.RemoveAt(index1);

      index0 = index0 % _points.Count;
      lastIndex0 = (index0 - 1 + _points.Count) % _points.Count;
    }
  }

  public static bool Do2DSegmentsIntersect(Vector2 p1, Vector2 q1, Vector2 p2, Vector2 q2) {
    // Find the four orientations needed for general and
    // special cases
    int o1 = orientation(p1, q1, p2);
    int o2 = orientation(p1, q1, q2);
    int o3 = orientation(p2, q2, p1);
    int o4 = orientation(p2, q2, q1);

    // General case
    if (o1 != o2 && o3 != o4) {
      return true;
    }

    // Special Cases
    // p1, q1 and p2 are colinear and p2 lies on segment p1q1
    if (o1 == 0 && onSegment(p1, p2, q1)) return true;

    // p1, q1 and p2 are colinear and q2 lies on segment p1q1
    if (o2 == 0 && onSegment(p1, q2, q1)) return true;

    // p2, q2 and p1 are colinear and p1 lies on segment p2q2
    if (o3 == 0 && onSegment(p2, p1, q2)) return true;

    // p2, q2 and q1 are colinear and q1 lies on segment p2q2
    if (o4 == 0 && onSegment(p2, q1, q2)) return true;

    return false; // Doesn't fall in any of the above cases
  }

  private static bool doesEdgeIntersect(Vector2 internalPoint, Vector2 cornerPoint, Vector2 crossPoint, Vector2 edgePoint) {
    Vector2 edge = edgePoint - cornerPoint;
    Vector2 hypo = crossPoint - cornerPoint;
    Vector2 leg = cornerPoint - internalPoint;

    float crossA = Mathf.Sign(cross(leg, edge));
    float crossB = Mathf.Sign(cross(leg, hypo));
    if (crossA != crossB) {
      return false;
    }

    if (Vector2.Angle(leg, edge) < Vector2.Angle(leg, hypo)) {
      return false;
    }

    return true;
  }

  private struct IndexedPoint {
    public Vector2 position;
    public int index;
  }

  private static float cross(Vector2 a, Vector2 b) {
    return a.x * b.y - a.y * b.x;
  }

  // Given three colinear points p, q, r, the function checks if
  // point q lies on line segment 'pr'
  private static bool onSegment(Vector2 p, Vector2 q, Vector2 r) {
    if (q.x <= Mathf.Max(p.x, r.x) && q.x >= Mathf.Min(p.x, r.x) &&
        q.y <= Mathf.Max(p.y, r.y) && q.y >= Mathf.Min(p.y, r.y))
      return true;

    return false;
  }

  // To find orientation of ordered triplet (p, q, r).
  // The function returns following values
  // 0 --> p, q and r are colinear
  // 1 --> Clockwise
  // 2 --> Counterclockwise
  private static int orientation(Vector2 p, Vector2 q, Vector2 r) {
    // See http://www.geeksforgeeks.org/orientation-3-ordered-points/
    // for details of below formula.
    float val = (q.y - p.y) * (r.x - q.x) -
                (q.x - p.x) * (r.y - q.y);

    if (val < float.Epsilon) return 0;  // colinear

    return (val > 0) ? 1 : 2; // clock or counterclock wise
  }
}
