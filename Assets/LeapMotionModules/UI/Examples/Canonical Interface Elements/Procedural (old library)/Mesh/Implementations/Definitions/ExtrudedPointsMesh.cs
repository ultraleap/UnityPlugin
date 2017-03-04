using UnityEngine;
using System;
using System.Collections.Generic;
using Leap.Unity.Attributes;

namespace Procedural.DynamicMesh {

  public class ExtrudedPointsMesh : MeshBehaviour<ExtrudedPointsDef>, IPreGenerate {

    [SerializeField]
    private Transform _pointAnchor;

    public bool OnPreGenerate() {
      if (_pointAnchor == null) {
        return false;
      }

      if (_meshDef.points == null) {
        _meshDef.points = new List<Vector2>();
      }

      _meshDef.points.Clear();
      recursivelyAddPoints(_pointAnchor);
      return true;
    }

    private void recursivelyAddPoints(Transform anchor) {
      bool anyChildren = false;
      foreach (Transform child in anchor) {
        anyChildren = true;
        recursivelyAddPoints(child);
      }

      if (!anyChildren) {
        _meshDef.points.Add(anchor.localPosition);
      }
    }
  }

  [Serializable]
  public struct ExtrudedPointsDef : IMeshDef {
    public List<Vector2> points;

    public float extrudeForward;

    public float extrudeBack;

    [MinValue(2)]
    public int resolution;

    [Range(0, 180)]
    public float smoothAngle;

    public FillMode fillMode;

    public void Generate(RawMesh mesh) {
      if (points == null || points.Count < 2) {
        return;
      }

      if (resolution < 2) {
        return;
      }

      List<List<Vector2>> segments = new List<List<Vector2>>();
      List<Vector2> currPointList = new List<Vector2>();
      segments.Add(currPointList);
      for (int i = 0; i < points.Count; i++) {
        Vector2 point = points[i];
        if (currPointList.Count >= 2) {
          Vector2 prev0 = currPointList[currPointList.Count - 1];
          Vector2 prev1 = currPointList[currPointList.Count - 2];

          if (Vector2.Angle(prev0 - prev1, point - prev0) > smoothAngle) {
            currPointList = new List<Vector2>();
            currPointList.Add(prev0);
            segments.Add(currPointList);
          }
        }

        currPointList.Add(point);
      }

      if (fillMode != FillMode.None) {
        List<int> edgeTriangulation = new List<int>();
        MeshUtility2D.Get2DConcaveTriangulation(points, edgeTriangulation);

        if (fillMode != FillMode.Left) {
          addEdgeFace(mesh, extrudeForward, points, edgeTriangulation);
        }

        if (fillMode != FillMode.Right) {
          MeshUtility.FlipTris(edgeTriangulation);
          addEdgeFace(mesh, extrudeBack, points, edgeTriangulation);
        }
      }

      for (int i = 0; i < segments.Count; i++) {
        List<Vector2> segment = segments[i];

        for (int j = 0; j < segment.Count; j++) {
          Vector2 point = segment[j];

          for (int k = 0; k < resolution; k++) {
            if (j != 0 && k != 0) {
              mesh.indexes.Add(mesh.verts.Count);
              mesh.indexes.Add(mesh.verts.Count - 1 - resolution);
              mesh.indexes.Add(mesh.verts.Count - 1);

              mesh.indexes.Add(mesh.verts.Count);
              mesh.indexes.Add(mesh.verts.Count - resolution);
              mesh.indexes.Add(mesh.verts.Count - 1 - resolution);
            }

            float percentX = k / (resolution - 1.0f);

            float x = point.x;
            float z = Mathf.Lerp(extrudeBack, extrudeForward, percentX);
            float y = point.y;

            mesh.verts.Add(new Vector3(x, y, z));
          }
        }
      }
    }

    private void addEdgeFace(RawMesh mesh, float z, List<Vector2> edgePoints, List<int> triangulation) {
      for (int i = 0; i < triangulation.Count; i++) {
        mesh.indexes.Add(triangulation[i] + mesh.verts.Count);
      }

      for (int i = 0; i < edgePoints.Count; i++) {
        Vector3 pos = edgePoints[i];
        pos.z = z;

        mesh.verts.Add(pos);
      }
    }

    public enum FillMode {
      None,
      Left,
      Right,
      Both
    }
  }
}
