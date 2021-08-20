/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2020.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using UnityEngine;

namespace Leap.Unity.Geometry {

  public struct Bezier {

    public Vector3 a;
    public Vector3 b;
    public Vector3 c;

    public Bezier(Vector3 a, Vector3 b, Vector3 c) {
      this.a = a; this.b = b; this.c = c;
    }

    public BezierPointEnumerator Points(int numPoints) {
      return new BezierPointEnumerator(this, numPoints);
    }

    public BezierLineEnumerator Lines(int numLines) {
      return new BezierLineEnumerator(this, numLines);
    }

    public struct BezierPointEnumerator {
      Bezier bez; int numPoints; int idx;
      public BezierPointEnumerator(Bezier bez, int numPoints) {
        this.bez = bez;
        this.numPoints = Mathf.Max(2, numPoints);
        this.idx = -1;
      }
      public Vector3 Current { get {
        var t = Mathf.Max(0, idx) / (float)numPoints;
        return Vector3.Lerp(
          Vector3.Lerp(bez.a, bez.b, t),
          Vector3.Lerp(bez.b, bez.c, t),
          t
        );
      }}
      public bool MoveNext() {
        if (idx == numPoints) { return false; }
        idx += 1;
        return true;
      }
      public BezierPointEnumerator GetEnumerator() { return this; }
    }

    public struct BezierLineEnumerator {
      BezierPointEnumerator points;
      Vector3? lastPoint;
      public BezierLineEnumerator(Bezier bez, int numLines) {
        numLines = Mathf.Max(1, numLines);
        this.points = new BezierPointEnumerator(bez, numLines + 1);
        lastPoint = null;
      }
      public LocalSegment3 Current { get {
        if (!lastPoint.HasValue) {
          return new LocalSegment3(points.Current, points.Current);
        }
        else {
          return new LocalSegment3(lastPoint.Value, points.Current);
        }
      }}
      public bool MoveNext() {
        if (!lastPoint.HasValue) {
          if (!points.MoveNext()) { return false; }
        }
        lastPoint = points.Current;
        if (!points.MoveNext()) { return false; }
        return true;
      }
      public BezierLineEnumerator GetEnumerator() { return this; }
    }

  }

}
