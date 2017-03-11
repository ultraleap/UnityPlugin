using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.UI {

  using MeshGenHelperExtensions;

  public static partial class MeshGen {

    private static class RoundedRectPrismSupport {

      public static void AddFrontIndices(List<int> outIndices, int startingVertCount, int cornerDivisions, bool flipFacing = false) {
        List<int> i = outIndices;
        int v0 = startingVertCount;

        // Center Quad
        i.AddTri(v0, 0, 1, 2, flipFacing);
        i.AddTri(v0, 0, 2, 3, flipFacing);

        // Top Quad
        i.AddTri(v0, 1, 4, 5, flipFacing);
        i.AddTri(v0, 1, 5, 2, flipFacing);

        // Upper Right Corner
        int cornerStart = v0 + 5;
        for (int v = cornerStart; v < cornerStart + 1 + cornerDivisions; v++) {
          i.AddTri(v0 + 2, v, v + 1, flipFacing);
        }

        // Right Quad
        i.AddTri(v0 + 3, v0 + 2, cornerStart + 1 + cornerDivisions, flipFacing);
        i.AddTri(v0 + 3, cornerStart + 1 + cornerDivisions, cornerStart + 2 + cornerDivisions, flipFacing);

        // Lower Right Corner
        cornerStart = cornerStart + 2 + cornerDivisions;
        for (int v = cornerStart; v < cornerStart + 1 + cornerDivisions; v++) {
          i.AddTri(v0 + 3, v, v + 1, flipFacing);
        }

        // Bottom Quad
        i.AddTri(v0 + 0, v0 + 3, cornerStart + 2 + cornerDivisions, flipFacing);
        i.AddTri(v0 + 3, cornerStart + 1 + cornerDivisions, cornerStart + 2 + cornerDivisions, flipFacing);

        // Lower Left Corner
        cornerStart = cornerStart + 2 + cornerDivisions;
        for (int v = cornerStart; v < cornerStart + 1 + cornerDivisions; v++) {
          i.AddTri(v0 + 0, v, v + 1, flipFacing);
        }

        // Left Quad
        i.AddTri(v0 + 0, cornerStart + 1 + cornerDivisions, v0 + 1, flipFacing);
        i.AddTri(v0 + 1, cornerStart + 1 + cornerDivisions, cornerStart + 2 + cornerDivisions, flipFacing);

        // Upper Left Corner
        cornerStart = cornerStart + 2 + cornerDivisions;
        for (int v = cornerStart; v < cornerStart + 1 + cornerDivisions; v++) {
          i.AddTri(v0 + 1, v, (v == cornerStart + cornerDivisions ? v0 + 4 : v + 1), flipFacing);
        }
      }

      public static void AddFrontVerts(List<Vector3> outVerts, List<Vector3> outNormals, Vector3 extents, float cornerRadius, int cornerDivisions, bool flipNormal = false) {
        List<Vector3> v = outVerts;

        float x = extents.x / 2F, y = extents.y / 2F, z = -extents.z;
        float r = cornerRadius;

        int startVertCount = outVerts.Count; // counting verts added for normals (all will point up).

        // Center Quad
        v.AddVert(-(x - r), -(y - r), z);
        v.AddVert(-(x - r), (y - r), z);
        v.AddVert((x - r), (y - r), z);
        v.AddVert((x - r), -(y - r), z);

        // Top Quad
        v.AddVert(-(x - r), y, z);
        v.AddVert((x - r), y, z);

        // Upper Right Corner
        Vector3 center = new Vector3((x - r), (y - r), z);
        Vector3 radialVector = Vector3.up * r;
        Quaternion rotation = Quaternion.AngleAxis(90F / (1 + cornerDivisions), -Vector3.forward);
        for (int i = 0; i < cornerDivisions; i++) {
          radialVector = rotation * radialVector;
          v.Add(center + radialVector);
        }

        // Right Quad
        v.AddVert(x, (y - r), z);
        v.AddVert(x, -(y - r), z);

        // Lower Right Corner
        center = new Vector3((x - r), -(y - r), z);
        radialVector = rotation * radialVector;
        for (int i = 0; i < cornerDivisions; i++) {
          radialVector = rotation * radialVector;
          v.Add(center + radialVector);
        }

        // Bottom Quad
        v.AddVert((x - r), -y, z);
        v.AddVert(-(x - r), -y, z);

        // Lower Left Corner
        center = new Vector3(-(x - r), -(y - r), z);
        radialVector = rotation * radialVector;
        for (int i = 0; i < cornerDivisions; i++) {
          radialVector = rotation * radialVector;
          v.Add(center + radialVector);
        }

        // Left Quad
        v.AddVert(-x, -(y - r), z);
        v.AddVert(-x, (y - r), z);

        // Upper Left Corner
        center = new Vector3(-(x - r), (y - r), z);
        radialVector = rotation * radialVector;
        for (int i = 0; i < cornerDivisions; i++) {
          radialVector = rotation * radialVector;
          v.Add(center + radialVector);
        }

        for (int i = 0; i < outVerts.Count - startVertCount; i++) {
          outNormals.Add((flipNormal ? -1F : 1F) * new Vector3(0, 0, -1));
        }

      }

      public static void AddSideIndices(List<int> outIndices, int startingVertCount, int cornerDivisions) {
        List<int> i = outIndices;
        int v0 = startingVertCount;

        // Top Quad (Side)
        i.AddTri(v0, 1, 0, 2);
        i.AddTri(v0, 1, 2, 3);

        // Upper Right Corner
        int cornerStart = 2;
        for (int v = cornerStart; v < cornerStart + 2 + cornerDivisions * 2; v += 2) {
          i.AddTri(v0, v + 1, v, v + 2);
          i.AddTri(v0, v + 1, v + 2, v + 3);
        }

        // Right Quad
        int curOffset = cornerStart + 2 + cornerDivisions * 2;
        i.AddTri(v0 + curOffset, 1, 0, 2);
        i.AddTri(v0 + curOffset, 1, 2, 3);

        // Lower Right Corner
        cornerStart = curOffset + 2;
        for (int v = cornerStart; v < cornerStart + 2 + cornerDivisions * 2; v += 2) {
          i.AddTri(v0, v + 1, v, v + 2);
          i.AddTri(v0, v + 1, v + 2, v + 3);
        }

        // Bottom Quad
        curOffset = cornerStart + 2 + cornerDivisions * 2;
        i.AddTri(v0 + curOffset, 1, 0, 2);
        i.AddTri(v0 + curOffset, 1, 2, 3);

        // Lower Left Corner
        cornerStart = curOffset + 2;
        for (int v = cornerStart; v < cornerStart + 2 + cornerDivisions * 2; v += 2) {
          i.AddTri(v0, v + 1, v, v + 2);
          i.AddTri(v0, v + 1, v + 2, v + 3);
        }

        // Left Quad
        curOffset = cornerStart + 2 + cornerDivisions * 2;
        i.AddTri(v0 + curOffset, 1, 0, 2);
        i.AddTri(v0 + curOffset, 1, 2, 3);

        // Upper Left Corner
        cornerStart = curOffset + 2;
        //int numSideVerts = 16 + 4 * cornerDivisions;
        for (int v = cornerStart; v < cornerStart + 2 + cornerDivisions * 2; v += 2) {
          i.AddTri(v0, v + 1, v, (v == cornerStart + cornerDivisions * 2 ? 0 : v + 2));
          i.AddTri(v0, v + 1, (v == cornerStart + cornerDivisions * 2 ? 0 : v + 2), (v == cornerStart + cornerDivisions * 2 ? 1 : v + 3));
        }
      }

      public static void AddSideVerts(List<Vector3> outVerts, List<Vector3> outNormals, Vector3 extents, float cornerRadius, int cornerDivisions) {
        List<Vector3> v = outVerts;
        List<Vector3> n = outNormals;
        float x = extents.x / 2F, y = extents.y / 2F, zFront = 0F, zBack = -extents.z;
        float r = cornerRadius;

        // Top Quad (Side)
        v.AddVert(-(x - r), y, zFront);
        v.AddVert(-(x - r), y, zBack);
        v.AddVert((x - r), y, zFront);
        v.AddVert((x - r), y, zBack);
        for (int i = 0; i < 4; i++) n.Add(Vector3.up);

        // Upper Right Corner
        Vector3 center = new Vector3((x - r), (y - r), zFront);
        Vector3 radial = Vector3.up * r;
        Quaternion rotation = Quaternion.AngleAxis(90F / (1 + cornerDivisions), -Vector3.forward);
        for (int i = 0; i < cornerDivisions; i++) {
          radial = rotation * radial;
          v.Add(center + radial);
          v.Add(center + radial + Vector3.back * (zFront - zBack));
          n.Add(radial);
          n.Add(radial);
        }

        // Right Quad
        v.AddVert(x, (y - r), zFront);
        v.AddVert(x, (y - r), zBack);
        v.AddVert(x, -(y - r), zFront);
        v.AddVert(x, -(y - r), zBack);
        for (int i = 0; i < 4; i++) n.Add(Vector3.right);

        // Lower Right Corner
        center = new Vector3((x - r), -(y - r), zFront);
        radial = rotation * radial;
        for (int i = 0; i < cornerDivisions; i++) {
          radial = rotation * radial;
          v.Add(center + radial);
          v.Add(center + radial + Vector3.back * (zFront - zBack));
          n.Add(radial);
          n.Add(radial);
        }

        // Bottom Quad
        v.AddVert((x - r), -y, zFront);
        v.AddVert((x - r), -y, zBack);
        v.AddVert(-(x - r), -y, zFront);
        v.AddVert(-(x - r), -y, zBack);
        for (int i = 0; i < 4; i++) n.Add(Vector3.down);

        // Lower Left Corner
        center = new Vector3(-(x - r), -(y - r), zFront);
        radial = rotation * radial;
        for (int i = 0; i < cornerDivisions; i++) {
          radial = rotation * radial;
          v.Add(center + radial);
          v.Add(center + radial + Vector3.back * (zFront - zBack));
          n.Add(radial);
          n.Add(radial);
        }

        // Left Quad
        v.AddVert(-x, -(y - r), zFront);
        v.AddVert(-x, -(y - r), zBack);
        v.AddVert(-x, (y - r), zFront);
        v.AddVert(-x, (y - r), zBack);
        for (int i = 0; i < 4; i++) n.Add(Vector3.left);

        // Upper Left Corner
        center = new Vector3(-(x - r), (y - r), zFront);
        radial = rotation * radial;
        for (int i = 0; i < cornerDivisions; i++) {
          radial = rotation * radial;
          v.Add(center + radial);
          v.Add(center + radial + Vector3.back * (zFront - zBack));
          n.Add(radial);
          n.Add(radial);
        }
      }

    }

  }

}