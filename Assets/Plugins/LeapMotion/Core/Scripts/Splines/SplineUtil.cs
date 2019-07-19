using Leap.Unity.Infix;
using Leap.Unity.Meshing;
using Leap.Unity.RuntimeGizmos;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.Animation {

  using IPositionSpline = ISpline<Vector3, Vector3>;
  using IPoseSpline = ISpline<Pose, Movement>;

  public static class SplineUtil {

    #region Conversion Utils

    public static PoseToPositionSplineWrapper AsPositionSpline(this IPoseSpline poseSpline) {
      return new PoseToPositionSplineWrapper(poseSpline);
    }

    public struct PoseToPositionSplineWrapper : IPositionSpline {

      public IPoseSpline poseSpline;

      public PoseToPositionSplineWrapper(IPoseSpline poseSpline) {
        this.poseSpline = poseSpline;
      }

      #region IPositionSpline

      public float minT { get { return poseSpline.minT; } }

      public float maxT { get { return poseSpline.maxT; } }

      public Vector3 ValueAt(float t) {
        return poseSpline.ValueAt(t).position;
      }

      public Vector3 DerivativeAt(float t) {
        return poseSpline.DerivativeAt(t).velocity;
      }

      public void ValueAndDerivativeAt(float t, out Vector3 value, out Vector3 deltaValuePerSec) {
        Pose pose;
        Movement movement;
        poseSpline.ValueAndDerivativeAt(t, out pose, out movement);

        value = pose.position;
        deltaValuePerSec = movement.velocity;
      }

      #endregion

    }

    #endregion

    #region Mesh Utils

    public static void FillPolyMesh(this IPositionSpline spline,
                                    PolyMesh polyMesh,
                                    Matrix4x4? applyTransform = null,
                                    float? startT = null, float? endT = null,
                                    int? numSegments = null,
                                    float[] radii = null,
                                    float? radius = null,
                                    bool drawDebug = false) {
      float minT, maxT;
      int effNumSegments;
      //bool useRadiusArr = false;
      //float[] effRadii;
      float effRadius;
      bool useTransform;
      Matrix4x4 transform;

      RuntimeGizmos.RuntimeGizmoDrawer drawer = null;
      if (drawDebug) {
        RuntimeGizmos.RuntimeGizmoManager.TryGetGizmoDrawer(out drawer);
      }

      // Assign parameters based on optional inputs
      {
        minT = spline.minT;
        if (startT.HasValue) {
          minT = startT.Value;
        }

        maxT = spline.maxT;
        if (endT.HasValue) {
          maxT = endT.Value;
        }

        effNumSegments = 32;
        if (numSegments.HasValue) {
          effNumSegments = numSegments.Value;
          effNumSegments = Mathf.Max(1, effNumSegments);
        }

        //useRadiusArr = false;
        effRadius = 0.02f;
        if (radius.HasValue) {
          effRadius = radius.Value;
        }

        //effRadii = null;
        //if (radii != null) {
        //  useRadiusArr = true;
        //  effRadii = radii;
        //}

        useTransform = false;
        transform = Matrix4x4.identity;
        if (applyTransform.HasValue) {
          useTransform = true;
          transform = applyTransform.Value;
        }
      }

      // Multiple passes through the spline data will construct all the positions and
      // orientations we need to build the mesh.
      polyMesh.Clear();
      var crossSection = new CircularCrossSection(effRadius, 16);
      float tStep = (maxT - minT) / effNumSegments;
      Vector3 position = Vector3.zero;
      Vector3 dPosition = Vector3.zero;
      Vector3? tangent = null;

      var positions = Pool<List<Vector3>>.Spawn();
      positions.Clear();
      var normals = Pool<List<Vector3>>.Spawn(); // to start, normals contain velocities,
      normals.Clear();                           // but zero velocities are filtered out.
      var binormals = Pool<List<Vector3>>.Spawn();
      binormals.Clear();
      var crossSection0Positions = Pool<List<Vector3>>.Spawn();
      crossSection0Positions.Clear();
      var crossSection1Positions = Pool<List<Vector3>>.Spawn();
      crossSection1Positions.Clear();
      try {
        // Construct a rough list of positions and normals for each cross section. Some
        // of the normals may be zero, so we'll have to fix those.
        for (int i = 0; i <= effNumSegments; i++) {
          var t = minT + i * tStep;

          spline.ValueAndDerivativeAt(t, out position, out dPosition);

          if (useTransform) {
            positions.Add(transform.MultiplyPoint3x4(position));
            normals.Add(transform.MultiplyVector(dPosition).normalized);
          }
          else {
            positions.Add(position);
            normals.Add(dPosition.normalized);
          }

          if (!tangent.HasValue && dPosition.sqrMagnitude > 0.001f * 0.001f) {
            tangent = (transform * dPosition.WithW(1)).ToVector3().normalized.Perpendicular();
          }
        }

        // In case we never got a non-zero velocity, try to construct a tangent based on
        // delta positions.
        if (!tangent.HasValue) {
          if (positions[0] == positions[1]) {
            // No spline mesh possible; there's no non-zero length segment.
            return;
          }
          else {
            var delta = positions[1] - positions[0];

            // Very specific case: Two points, each with zero velocity, use delta for
            // normals
            if (positions.Count == 2) {
              normals[0] = delta; normals[1] = delta;
            }

            tangent = delta.Perpendicular();
          }
        }

        // Try to propagate non-zero normals into any "zero" normals.
        for (int i = 0; i <= effNumSegments; i++) {
          if (normals[i].sqrMagnitude < 0.00001f) {
            if (i == 0) {
              normals[i] = normals[i + 1];
            }
            else if (i == effNumSegments) {
              normals[i] = normals[i - 1];
            }
            else {
              normals[i] = Vector3.Slerp(normals[i - 1], normals[i + 1], 0.5f);
            }
          }

          if (normals[i].sqrMagnitude < 0.00001f) {
            // OK, we tried, but we still have zero normals. Error and fail.
            throw new System.InvalidOperationException(
              "Unable to build non-zero normals for this spline during PolyMesh "
              + "construction");
          }
        }

        // With a set of normals and a starting tangent vector, we can construct all the
        // binormals we need to have an orientation and position for every cross-section.
        Vector3? lastNormal = null;
        Vector3? lastBinormal = null;
        for (int i = 0; i <= effNumSegments; i++) {
          var normal = normals[i];
          Vector3 binormal;
          if (!lastBinormal.HasValue) {
            binormal = Vector3.Cross(normal, tangent.Value);
          }
          else {
            var rotFromLastNormal = Quaternion.FromToRotation(lastNormal.Value, normal);

            binormal = rotFromLastNormal * lastBinormal.Value;
          }
          binormals.Add(binormal);

          lastNormal = normal;
          lastBinormal = binormal;
        }

        // With positions, normals, and binormals for every cross section, add positions
        // and polygons for each cross section and their connections to the PolyMesh.
        int cs0Idx = -1, cs1Idx = -1;
        for (int i = 0; i + 1 <= effNumSegments; i++) {
          var pose0 = new Pose(positions[i],
                               Quaternion.LookRotation(normals[i], binormals[i]));
          var pose1 = new Pose(positions[i + 1],
                               Quaternion.LookRotation(normals[i + 1], binormals[i + 1]));

          if (drawDebug) {
            drawer.PushMatrix();
            drawer.matrix = transform.inverse;

            drawer.color = LeapColor.blue;
            drawer.DrawRay(pose0.position, normals[i] * 0.2f);

            drawer.color = LeapColor.red;
            drawer.DrawRay(pose0.position, binormals[i] * 0.2f);

            drawer.PopMatrix();
          }

          bool addFirstPositions = i == 0;

          // Add positions from Cross Section definition to reused buffers.
          if (addFirstPositions) {
            crossSection.FillPositions(crossSection0Positions, pose0);
          }
          crossSection.FillPositions(crossSection1Positions, pose1);

          // Add positions from buffers into the PolyMesh.
          if (addFirstPositions) {
            cs0Idx = polyMesh.positions.Count;
            polyMesh.AddPositions(crossSection0Positions);
          }
          cs1Idx = polyMesh.positions.Count;
          polyMesh.AddPositions(crossSection1Positions);

          // Add polygons to connect one cross section in the PolyMesh to the other.
          crossSection.AddConnectingPolygons(polyMesh, cs0Idx, cs1Idx);

          Utils.Swap(ref crossSection0Positions, ref crossSection1Positions);
          cs0Idx = cs1Idx;
        }
      }
      finally {
        positions.Clear();
        Pool<List<Vector3>>.Recycle(positions);
        normals.Clear();
        Pool<List<Vector3>>.Recycle(normals);
        binormals.Clear();
        Pool<List<Vector3>>.Recycle(binormals);
        crossSection0Positions.Clear();
        Pool<List<Vector3>>.Recycle(crossSection0Positions);
        crossSection1Positions.Clear();
        Pool<List<Vector3>>.Recycle(crossSection1Positions);
      }

    }

    public struct CircularCrossSection : ICrossSection<CircularCrossSection> {

      private const float DEFAULT_RADIUS = 0.02f;
      private float? _radius;
      public float radius {
        get { return _radius.HasValue ? _radius.Value : DEFAULT_RADIUS; }
        set { _radius = value; }
      }

      private const int DEFAULT_NUM_SEGMENTS = 32;
      private int? _numSegments;
      public int numSegments {
        get { return _numSegments.HasValue ? _numSegments.Value : DEFAULT_NUM_SEGMENTS; }
        set { _numSegments = value; }
      }

      public CircularCrossSection(float radius = 0.02f, int resolution = 16) {
        _radius = radius;
        _numSegments = resolution;
      }

      public int vertCount { get { return numSegments; } }
      public int connectingPolygonsVertCount { get { return 4 /* quads */; } }

      public void FillPositions(List<Vector3> positions, Pose? pose) {
        positions.Clear();

        var effPose = pose.HasValue ? pose.Value : Pose.identity;

        var angle = 360f / numSegments;
        var rot = Quaternion.AngleAxis(angle, effPose.rotation.GetForward());
        var R = effPose.rotation.GetRight() * radius;
        for (int i = 0; i < numSegments; i++) {
          positions.Add(R + effPose.position);
          R = rot * R;
        }
      }

      /// <summary>
      /// Adds Polygons to the argument PolyMesh that connect two cross sections
      /// matching this cross section definition; the first cross section vertex index
      /// is specified by crossSectionStartIdx0 and the second by crossSectionStartIdx1.
      /// </summary>
      public void AddConnectingPolygons(PolyMesh polyMesh,
                                        int crossSectionStartIdx0,
                                        int crossSectionStartIdx1) {
        var cs0 = crossSectionStartIdx0;
        var cs1 = crossSectionStartIdx1;
        for (int i = 0; i < numSegments; i++) {
          var poly = new Polygon();
          poly.verts = Pool<List<int>>.Spawn();
          poly.verts.Clear();

          int iPlusOne = (i + 1) % numSegments;

          poly.verts.Add(cs0 + i);
          poly.verts.Add(cs0 + iPlusOne);
          poly.verts.Add(cs1 + iPlusOne);
          poly.verts.Add(cs1 + i);

          polyMesh.AddPolygon(poly);
        }
      }

    }

    public interface ICrossSection<T> {

      int vertCount { get; }

      int connectingPolygonsVertCount { get; }

      //void AddConnectingPolygons

    }

    #endregion

  }

}