using UnityEngine;
using System;
using Leap.Unity.Attributes;
using System.Collections.Generic;

namespace Procedural.DynamicMesh {

  public class SideRoundedCubeMesh : MeshBehaviour<SideRoundedCubeDef> {

    public static void AddFrontIndices(RawMesh mesh, int cornerDivisions) {
      List<int> i = mesh.indexes;

      // Center Quad
      i.AddTri(0, 1, 2);
      i.AddTri(0, 2, 3);

      // Top Quad
      i.AddTri(1, 4, 5);
      i.AddTri(1, 5, 2);

      // Upper Right Corner
      int cornerStart = 5;
      for (int v = cornerStart; v < cornerStart + 1 + cornerDivisions; v++) {
        i.AddTri(2, v, v + 1);
      }

      // Right Quad
      i.AddTri(3, 2, cornerStart + 1 + cornerDivisions);
      i.AddTri(3, cornerStart + 1 + cornerDivisions, cornerStart + 2 + cornerDivisions);

      // Lower Right Corner
      cornerStart = cornerStart + 2 + cornerDivisions;
      for (int v = cornerStart; v < cornerStart + 1 + cornerDivisions; v++) {
        i.AddTri(3, v, v + 1);
      }

      // Bottom Quad
      i.AddTri(0, 3, cornerStart + 2 + cornerDivisions);
      i.AddTri(3, cornerStart + 1 + cornerDivisions, cornerStart + 2 + cornerDivisions);

      // Lower Left Corner
      cornerStart = cornerStart + 2 + cornerDivisions;
      for (int v = cornerStart; v < cornerStart + 1 + cornerDivisions; v++) {
        i.AddTri(0, v, v + 1);
      }

      // Left Quad
      i.AddTri(0, cornerStart + 1 + cornerDivisions, 1);
      i.AddTri(1, cornerStart + 1 + cornerDivisions, cornerStart + 2 + cornerDivisions);

      // Upper Left Corner
      cornerStart = cornerStart + 2 + cornerDivisions;
      for (int v = cornerStart; v < cornerStart + 1 + cornerDivisions; v++) {
        i.AddTri(1, v, (v == cornerStart + cornerDivisions ? 4 : v + 1));
      }
    }

    public static void AddFrontVerts(RawMesh mesh, Vector3 extents, float cornerRadius, int cornerDivisions) {
      List<Vector3> v = mesh.verts;
      float x = extents.x / 2F, y = extents.y / 2F, z = -extents.z;
      float r = cornerRadius;

      // Center Quad
      v.AddVert(-(x - r), -(y - r), z);
      v.AddVert(-(x - r),  (y - r), z);
      v.AddVert( (x - r),  (y - r), z);
      v.AddVert( (x - r), -(y - r), z);

      // Top Quad
      v.AddVert(-(x - r), y, z);
      v.AddVert( (x - r), y, z);

      // Upper Right Corner
      Vector3 center = new Vector3((x - r), (y - r), z);
      Vector3 radialVector = Vector3.up * r;
      Quaternion rotation = Quaternion.AngleAxis(90F / (1 + cornerDivisions), -Vector3.forward);
      for (int i = 0; i < cornerDivisions; i++) {
        radialVector = rotation * radialVector;
        v.Add(center + radialVector);
      }

      // Right Quad
      v.AddVert(x,  (y - r), z);
      v.AddVert(x, -(y - r), z);

      // Lower Right Corner
      center = new Vector3((x - r), -(y - r), z);
      radialVector = rotation * radialVector;
      for (int i = 0; i < cornerDivisions; i++) {
        radialVector = rotation * radialVector;
        v.Add(center + radialVector);
      }

      // Bottom Quad
      v.AddVert( (x - r), -y, z);
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
    }

    public static void AddSideIndices(RawMesh mesh, int cornerDivisions) {
      List<int> i = mesh.indices;
      int v0 = mesh.verts.Count;

      // Top Quad (Side)
      i.AddTri(v0, 1, 0, 2);
      i.AddTri(v0, 1, 2, 3);

      // Upper Right Corner
      int cornerStart = 2;
      for (int v = cornerStart; v < cornerStart + 2 + cornerDivisions * 2; v += 2) {
        i.AddTri(v0, v + 1, v,     v + 2);
        i.AddTri(v0, v + 1, v + 2, v + 3);
      }

      // Right Quad
      int curOffset = cornerStart + 2 + cornerDivisions * 2;
      i.AddTri(v0 + curOffset, 1, 0, 2);
      i.AddTri(v0 + curOffset, 1, 2, 3);

      // Lower Right Corner
      cornerStart = curOffset + 2;
      for (int v = cornerStart; v < cornerStart + 2 + cornerDivisions * 2; v += 2) {
        i.AddTri(v0, v + 1, v,     v + 2);
        i.AddTri(v0, v + 1, v + 2, v + 3);
      }

      // Bottom Quad
      curOffset = cornerStart + 2 + cornerDivisions * 2;
      i.AddTri(v0 + curOffset, 1, 0, 2);
      i.AddTri(v0 + curOffset, 1, 2, 3);

      // Lower Left Corner
      cornerStart = curOffset + 2;
      for (int v = cornerStart; v < cornerStart + 2 + cornerDivisions * 2; v += 2) {
        i.AddTri(v0, v + 1, v,     v + 2);
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
        i.AddTri(v0, v + 1, v,                                                    (v == cornerStart + cornerDivisions * 2 ? 0 : v + 2));
        i.AddTri(v0, v + 1, (v == cornerStart + cornerDivisions * 2 ? 0 : v + 2), (v == cornerStart + cornerDivisions * 2 ? 1 : v + 3));
      }
    }

    public static void AddSideVerts(RawMesh mesh, Vector3 extents, float cornerRadius, int cornerDivisions) {
      List<Vector3> v = mesh.verts;
      float x = extents.x / 2F, y = extents.y / 2F, zFront = 0F, zBack = -extents.z;
      float r = cornerRadius;

      // Top Quad (Side)
      v.AddVert(-(x - r), y, zFront);
      v.AddVert(-(x - r), y, zBack);
      v.AddVert( (x - r), y, zFront);
      v.AddVert( (x - r), y, zBack);

      // Upper Right Corner
      Vector3 center = new Vector3((x - r), (y - r), zFront);
      Vector3 radial = Vector3.up * r;
      Quaternion rotation = Quaternion.AngleAxis(90F / (1 + cornerDivisions), -Vector3.forward);
      for (int i = 0; i < cornerDivisions; i++) {
        radial = rotation * radial;
        v.Add(center + radial);
        v.Add(center + radial + Vector3.back * (zFront - zBack));
      }

      // Right Quad
      v.AddVert(x,  (y - r), zFront);
      v.AddVert(x,  (y - r), zBack);
      v.AddVert(x, -(y - r), zFront);
      v.AddVert(x, -(y - r), zBack);

      // Lower Right Corner
      center = new Vector3((x - r), -(y - r), zFront);
      radial = rotation * radial;
      for (int i = 0; i < cornerDivisions; i++) {
        radial = rotation * radial;
        v.Add(center + radial);
        v.Add(center + radial + Vector3.back * (zFront - zBack));
      }

      // Bottom Quad
      v.AddVert( (x - r), -y, zFront);
      v.AddVert( (x - r), -y, zBack);
      v.AddVert(-(x - r), -y, zFront);
      v.AddVert(-(x - r), -y, zBack);

      // Lower Left Corner
      center = new Vector3(-(x - r), -(y - r), zFront);
      radial = rotation * radial;
      for (int i = 0; i < cornerDivisions; i++) {
        radial = rotation * radial;
        v.Add(center + radial);
        v.Add(center + radial + Vector3.back * (zFront - zBack));
      }

      // Left Quad
      v.AddVert(-x, -(y - r), zFront);
      v.AddVert(-x, -(y - r), zBack);
      v.AddVert(-x,  (y - r), zFront);
      v.AddVert(-x,  (y - r), zBack);

      // Upper Left Corner
      center = new Vector3(-(x - r), (y - r), zFront);
      radial = rotation * radial;
      for (int i = 0; i < cornerDivisions; i++) {
        radial = rotation * radial;
        v.Add(center + radial);
        v.Add(center + radial + Vector3.back * (zFront - zBack));
      }
    }

  }

  [Serializable]
  public struct SideRoundedCubeDef : IMeshDef {
    public Vector3 extents;
    [MinValue(0)]
    public float cornerRadius;
    [MinValue(0)]
    public int cornerDivisions;

    public void Generate(RawMesh mesh) {
      mesh.PreallocateVerts(12 + 4 * cornerDivisions + 16 + 8 * cornerDivisions, VertexAttributes.None /* TODO: Add vert UVs */);

      // Front
      SideRoundedCubeMesh.AddFrontIndices(mesh, cornerDivisions);
      SideRoundedCubeMesh.AddFrontVerts(mesh, extents, cornerRadius, cornerDivisions);
      //SideRoundedCubeMesh.AddFrontUVs(mesh, cornerDivisions);

      // Side
      SideRoundedCubeMesh.AddSideIndices(mesh, cornerDivisions);
      SideRoundedCubeMesh.AddSideVerts(mesh, extents, cornerRadius, cornerDivisions);
      //SideRoundedCubeMesh.AddSideUVs(mesh, cornerDivisions);
    }
  }
}
