using System;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.Splines {

  /// <summary>
  /// Container class with useful utilities for working with splines.
  /// </summary>
  public static class zzOldSplineUtility {

    /// <summary>
    /// Generate mesh data suitable for Unity Mesh objects using ISplines.
    /// </summary>
    public static class MeshGen {

      private struct SplineMeshPoint {
        public Vector3 position;
        public Vector3 normal;
        public Vector3 binormal;
      }

      [ThreadStatic]
      private static List<SplineMeshPoint> s_meshPoints = new List<SplineMeshPoint>();
      [ThreadStatic]
      private static RingBuffer<Vector3> s_threePositionsBuffer = new RingBuffer<Vector3>(3);
      [ThreadStatic]
      private static RingBuffer<SplineMeshPoint> s_twoPointsBuffer = new RingBuffer<SplineMeshPoint>(2);

      /// <summary>
      /// Generates a quad-based spline mesh based on the input spline and provided spline thickness.
      /// The spline will be oriented so that its first segment faces towards initNormalFacingReference.
      /// Subsequent segments will rotate their normal/binormal directions based on the FromToRotation
      /// from one segment to the next.
      /// </summary>
      public static void GenerateQuadSplineMesh(ref List<Vector3> verts, ref List<int> indices,
                                            zzOldISpline spline, float splineThickness, Vector3 initNormalFacingReference,
                                            float minSplineSegmentDotProduct = 0.9F) {
        s_meshPoints.Clear();
        s_threePositionsBuffer.Clear();
        s_twoPointsBuffer.Clear();

        float halfThickness = splineThickness * 0.5F;

        // Traverse the spline, constructing SplineMeshPoints with precalculated normals and binormals
        // to make mesh construction more straight-forward. SplineMeshPoints will be converted into 
        // connected quads for the spline mesh in the next step.
        Vector3 normal = Vector3.zero;
        Vector3 binormal = Vector3.zero;
        bool firstPointAdded = false;
        int count = 0;
        foreach (Vector3 pointOnSpline in spline.Traverse(minSplineSegmentDotProduct)) {
          // Add a placeholder SplineMeshPoint, to be fixed later using information from neighboring points.
          s_threePositionsBuffer.Add(pointOnSpline);
          count++;

          // With a buffer of three positions, we have enough information for the center point.
          if (s_threePositionsBuffer.IsFull) {
            var p0 = s_threePositionsBuffer.Get(0);
            var p1 = s_threePositionsBuffer.Get(1);
            var p2 = s_threePositionsBuffer.Get(2);

            // (Special case for the first position in the spline.)
            if (!firstPointAdded) {
              normal = (initNormalFacingReference - p0).normalized;
              Vector3 tangent = (p1 - p0).normalized;
              binormal = (Vector3.Cross(tangent, normal)).normalized;
              normal = (Vector3.Cross(binormal, tangent)).normalized; // Ensure basis directions are orthogonal.

              // Add the first spline mesh point, with a good normal (using the direction towards the camera
              // as a reasonable basis).
              s_meshPoints.Add(new SplineMeshPoint() { position = p0, normal = normal, binormal = binormal });

              firstPointAdded = true;
            }

            // Use neighbor information to fill in information for the center point in the buffer.
            Vector3 p0Normal = s_meshPoints[s_meshPoints.Count - 1].normal;
            Vector3 p0Binormal = s_meshPoints[s_meshPoints.Count - 1].binormal;
            Quaternion segmentRotation = Quaternion.FromToRotation((p1 - p0), (p2 - p1));
            normal = segmentRotation * p0Normal;
            binormal = segmentRotation * p0Binormal;
            s_meshPoints.Add(new SplineMeshPoint() { position = p1, normal = normal, binormal = binormal });
          }
        }
        // (Finally, add the last spline mesh point.)
        s_meshPoints.Add(new SplineMeshPoint() { position = s_threePositionsBuffer.Get(2), normal = normal, binormal = binormal });

        // Return early if there aren't enough spline mesh points.
        if (s_meshPoints.Count < 2) return;

        // Using a prepared list of SplineMeshPoints, construct the mesh by building connected quads.
        firstPointAdded = false;
        foreach (SplineMeshPoint meshPoint in s_meshPoints) {
          s_twoPointsBuffer.Add(meshPoint);

          if (s_twoPointsBuffer.IsFull) {
            SplineMeshPoint p0 = s_twoPointsBuffer.Get(0);
            SplineMeshPoint p1 = s_twoPointsBuffer.Get(1);

            // (Special case for the first position in the spline.)
            if (!firstPointAdded) {
              binormal = p0.binormal;

              verts.Add(p0.position + binormal * halfThickness);
              verts.Add(p0.position - binormal * halfThickness);

              firstPointAdded = true;
            }

            binormal = (p0.binormal + p1.binormal).normalized;
            addIncrementalQuad(verts, indices,
                               p1.position + binormal * halfThickness,
                               p1.position - binormal * halfThickness);
          }
        }
        // (Finally, add the last quad.)
        addIncrementalQuad(verts, indices,
                           s_meshPoints[s_meshPoints.Count - 1].position + binormal * halfThickness,
                           s_meshPoints[s_meshPoints.Count - 1].position - binormal * halfThickness);
      }

      /// <summary>
      /// Adds a quad using the previous two points in the verts list and two additionally-supplied verts.
      /// </summary>
      private static void addIncrementalQuad(List<Vector3> verts, List<int> indices, Vector3 p2, Vector3 p3) {
        int p0Idx = verts.Count - 2;
        int p1Idx = verts.Count - 1;
        Vector3 p0 = verts[p0Idx];
        Vector3 p1 = verts[p1Idx];

        verts.Add(p2);
        verts.Add(p3);

        int p2Idx = verts.Count - 2;
        int p3Idx = verts.Count - 1;
        addTri(indices, p0Idx, p2Idx, p1Idx);
        addTri(indices, p1Idx, p2Idx, p3Idx);
      }

      private static void addTri(List<int> indices, int a, int b, int c) {
        indices.Add(a);
        indices.Add(b);
        indices.Add(c);
      }

    }

  }

  /// <summary>
  /// Enumerator struct for traversing splines. SplineUtility.cs provides an extension
  /// method for ISplines that constructs these enumerators automatically; see Traverse() in
  /// SplineExtensions.
  /// </summary>
  public struct SplineInterpolatorEnumerator {
    private float minStepProduct; // minimum dot product value; effectively, a maximum angle between step directions

    public zzOldISpline spline;
    public int curControlAIdx; // enumerator moves along the spline,
    public int curControlBIdx; // two control points at a time

    private bool started;
    private Vector3 curPos;

    private float curProgress;
    private bool hasLastStepPos;
    private Vector3 lastStepPos;
    private float lastStep;

    public SplineInterpolatorEnumerator(zzOldISpline spline, float minStepProduct) {
      started = false;
      this.spline = spline;
      this.minStepProduct = Mathf.Max(minStepProduct, -0.5F, Mathf.Min(minStepProduct, 0.98F));
      curControlAIdx = -1;
      curControlBIdx = -1;
      curPos = Vector3.zero;
      curProgress = 0F;
      hasLastStepPos = false;
      lastStepPos = Vector3.zero;
      lastStep = 0.33F;
    }

    public SplineInterpolatorEnumerator GetEnumerator() { return this; }
    public bool MoveNext() {
      if (!started) {
        if (spline.numControlPoints == 0) {
          return false;
        }
        else if (spline.numControlPoints == 1) {
          started = true;
          curPos = spline.GetControlPosition(0);
          return true;
        }
        else {
          started = true;
          curPos = spline.GetControlPosition(0);
          curControlAIdx = 0;
          curControlBIdx = 1;
          curProgress = 0F;
          return true;
        }
      }
      else {
        // Move on to the next segment if we've reached it
        if (curProgress >= 0.999F) {
          if (curControlBIdx >= spline.numControlPoints) {
            return false;
          }
          else {
            curControlAIdx += 1;
            curControlBIdx += 1;
            curProgress = 0F;
          }
        }

        // Special case for end-of-spline.
        curControlBIdx = Mathf.Min(spline.numControlPoints - 1, curControlBIdx);
        if (curControlAIdx == curControlBIdx) {
          return false;
        }

        // Step, but check how sharp of a corner our step causes and shorten the
        // step if it's too sharp
        float step = hasLastStepPos ? Mathf.Max(lastStep * 3F, 0.2F) : 0.2F;
        float startProgress = curProgress;
        Vector3 startPos = curPos;
        int loops = 0;
        do {
          step *= 0.5F;
          curProgress = Mathf.Min(startProgress + step, 1F);
          curPos = spline.Evaluate(curControlAIdx, curControlBIdx, curProgress);
        } while (hasLastStepPos
                 && Vector3.Dot((startPos - lastStepPos).normalized, (curPos - startPos).normalized) < minStepProduct
                 && (++loops < 10));

        // Remember the last step position for sharpness checking in the future
        if (!hasLastStepPos) hasLastStepPos = true;
        lastStepPos = startPos;
        lastStep = step;

        return true;
      }
    }
    public Vector3 Current { get { return curPos; } }
  }

  /// <summary>
  /// Structure representing the rendered lines that make up a spline.
  /// Also contains a reference to the spline itself, and indices pointingto the control
  /// points that surround this fragment.
  /// </summary>
  public struct SplineFragment {
    /// <summary>
    /// The spline associated with this spline fragment.
    /// </summary>
    public zzOldISpline spline;

    /// <summary>
    /// The index of the control point that defines the
    /// beginning of the segment that contains this fragment.
    /// </summary>
    public int controlPointAIdx;

    /// <summary>
    /// The index of the control point that defines the end
    /// of the segment that contains this fragment.
    /// </summary>
    public int controlPointBIdx;

    /// <summary>
    /// The begin-position of this fragment of the spline.
    /// Two control points will surround one or more fragments
    /// depending on the curvature of the spline.
    /// </summary>
    public Vector3 a;

    /// <summary>
    /// The end-position of this fragment of the spline.
    /// Two control points will surround one or more fragments
    /// depending on the curvature of the spline.
    /// </summary>
    public Vector3 b;
  }

  /// <summary>
  /// Enumerator struct for traversing spline fragments (wraps around
  /// SplineInterpolatorEnumerator). SplineUtility.cs provides an extension method for
  /// ISplines that constructs these enumerators automatically; see TraverseSegments()
  /// in SplineExtensions.
  /// <see cref="SplineFragment"/>
  /// </summary>
  public struct SplineFragmentEnumerator {
    private SplineInterpolatorEnumerator interpolator;
    private bool started;
    private Vector3 curPos;
    private Vector3 lastPos;

    public SplineFragmentEnumerator(zzOldISpline spline, float minStepProduct) {
      interpolator = new SplineInterpolatorEnumerator(spline, minStepProduct);
      started = false;
      curPos = Vector3.zero;
      lastPos = Vector3.zero;
    }

    public SplineFragmentEnumerator GetEnumerator() { return this; }
    public bool MoveNext() {
      if (!started) {
        // At the beginning, we must ensure there are at least
        // two points to enumerate, and set lastPos/curPos appropriately.

        bool hasAtLeastOne = interpolator.MoveNext();
        if (!hasAtLeastOne) return false;
        lastPos = interpolator.Current;

        bool hasAtLeastTwo = interpolator.MoveNext();
        if (!hasAtLeastTwo) return false;
        curPos = interpolator.Current;

        started = true;
        return true;
      }
      else {
        // Beyond the first segment, we only need one new point,
        // and to set the segment points appropriately.

        bool hasAnotherSegment = interpolator.MoveNext();
        if (!hasAnotherSegment) return false;
        lastPos = curPos;
        curPos = interpolator.Current;

        return true;
      }
    }
    public SplineFragment Current {
      get { return new SplineFragment() { spline = interpolator.spline,
                                          controlPointAIdx = interpolator.curControlAIdx,
                                          controlPointBIdx = interpolator.curControlBIdx,
                                          a = lastPos,
                                          b = curPos }; }
    }
  }

  /// <summary>
  /// Extensions for ISplines. Notably, provides a shortcut to stepping along an ISpline
  /// via ISpline.Traverse().
  /// </summary>
  public static class ISplineExtensions {

    /// <summary>
    /// Returns an enumerator that will return points along an ISpline. For smoother splines, choose a
    /// minInterstepDotProduct that is closer to 1 (maximum 0.98F). For rougher splines, choose a
    /// minInterstepDotProduct that is closer to -1 (minimum -0.5F). A choice of 0 would allow angles
    /// up to but no larger than 90 degrees between adjacent spline segments.
    /// </summary>
    /// <remarks>
    /// The segment formed by each step returned and the previous step returned will have a dot product
    /// no smaller than minInterstepDotProduct with the segment formed by the previous step returned and
    /// the step before that. This enforces a maximum on cos(theta) where theta is the angle between any
    /// two adjacent segments in the spline. Specifically, the maximum angle could be calculated as
    /// arccos(minInterstepDotProduct).
    /// </remarks>
    public static SplineInterpolatorEnumerator Traverse(this zzOldISpline spline, float minInterstepDotProduct = 0.9F) {
      return new SplineInterpolatorEnumerator(spline, minInterstepDotProduct);
    }

    /// <summary>
    /// Returns an enumerator that returns segments along an ISpline. For smoother splines, choose a
    /// minIntersegmentDotProduct that is closer to 1 (maximum 0.98F). For rougher splines, choose a
    /// minIntersegmentDotProduct that is closer to -1 (minimum -0.5F). A choise of 0 would allow angles
    /// up to but no larger than 90 degrees between adjacent spline segments.
    /// </summary>
    /// <remarks>
    /// SplineSegmentEnumerator is a small wrapper around SplineInterpolatorEnumerator that remembers
    /// the last position traversed to two points at a time.
    /// </remarks>
    public static SplineFragmentEnumerator TraverseFragments(this zzOldISpline spline, float minIntersegmentDotProduct = 0.9F) {
      return new SplineFragmentEnumerator(spline, minIntersegmentDotProduct);
    }

  }

}