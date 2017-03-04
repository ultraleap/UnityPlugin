using UnityEngine;
using System;
using Leap.Unity.Attributes;
using System.Collections.Generic;

namespace Procedural.DynamicMesh {

  // modified from http://catlikecoding.com/unity/tutorials/rounded-cube/

  public class RoundedCubeMesh : MeshBehaviour<RoundedCubeDef> {

    void OnValidate() {
      int smallestSize = Mathf.Min(Mathf.Min(_meshDef.xSize, _meshDef.ySize), _meshDef.zSize);
      int maxRoundness = Mathf.RoundToInt((smallestSize + 0.2F) / 2F);
      if (_meshDef.roundness > maxRoundness) {
        _meshDef.roundness = maxRoundness;
      }
    }

  }

  [Serializable]
  public struct RoundedCubeDef : IMeshDef {

    [MinValue(2)]
    public int xSize;
    [MinValue(2)]
    public int ySize;
    [MinValue(2)]
    public int zSize;
    [MinValue(0)]
    public int roundness;
    [MinValue(0)]
    public float scale;

    public void Generate(RawMesh mesh) {

      mesh.Clear();

      int numVerts = GetNumVerts();
      mesh.PreallocateVerts(numVerts, VertexAttributes.Normals);

      // Indices
      AddIndices(mesh);

      // Verts
      for (int i = 0; i < numVerts; i++) {
        mesh.verts.Add(Vector3.zero);
        mesh.normals.Add(Vector3.zero);
      }
      SetVerts(mesh);

      // UVs
      // TODO: add UVs.
    }

    public void SetVerts(RawMesh mesh) {
      int v = 0;
      for (int y = 0; y <= ySize; y++) {
        for (int x = 0; x <= xSize; x++) {
          SetVertex(mesh, v++, x, y, 0, scale);
        }
        for (int z = 1; z <= zSize; z++) {
          SetVertex(mesh, v++, xSize, y, z, scale);
        }
        for (int x = xSize - 1; x >= 0; x--) {
          SetVertex(mesh, v++, x, y, zSize, scale);
        }
        for (int z = zSize - 1; z > 0; z--) {
          SetVertex(mesh, v++, 0, y, z, scale);
        }
      }
      for (int z = 1; z < zSize; z++) {
        for (int x = 1; x < xSize; x++) {
          SetVertex(mesh, v++, x, ySize, z, scale);
        }
      }
      for (int z = 1; z < zSize; z++) {
        for (int x = 1; x < xSize; x++) {
          SetVertex(mesh, v++, x, 0, z, scale);
        }
      }
    }

    private void SetVertex(RawMesh mesh, int i, int x, int y, int z, float scale) {
      Vector3 inner = mesh.verts[i] = new Vector3(x, y, z);

      if (x < roundness) {
        inner.x = roundness;
      }
      else if (x > xSize - roundness) {
        inner.x = xSize - roundness;
      }
      if (y < roundness) {
        inner.y = roundness;
      }
      else if (y > ySize - roundness) {
        inner.y = ySize - roundness;
      }
      if (z < roundness) {
        inner.z = roundness;
      }
      else if (z > zSize - roundness) {
        inner.z = zSize - roundness;
      }

      mesh.normals[i] = (mesh.verts[i] - inner).normalized;
      mesh.verts[i] = ((inner + mesh.normals[i] * roundness)
                        - (Vector3.right * xSize / 2)
                        - (Vector3.up * ySize / 2)
                        - (Vector3.forward * zSize / 2))
                      * scale;
    }

    private int GetNumVerts() {
      return GetNumVerts(xSize, ySize, zSize);
    }

    private static int GetNumVerts(int xSize, int ySize, int zSize) {
      int cornerVertices = 8;
      int edgeVertices = (xSize + ySize + zSize - 3) * 4;
      int faceVertices = (
        (xSize - 1) * (ySize - 1) +
        (xSize - 1) * (zSize - 1) +
        (ySize - 1) * (zSize - 1)) * 2;
      return cornerVertices + edgeVertices + faceVertices;
    }

    private void AddIndices(RawMesh mesh) {
      int[] trianglesZ = new int[(xSize * ySize) * 12];
      int[] trianglesX = new int[(ySize * zSize) * 12];
      int[] trianglesY = new int[(xSize * zSize) * 12];
      int ring = (xSize + zSize) * 2;
      int tZ = 0, tX = 0, tY = 0, v = 0;

      for (int y = 0; y < ySize; y++, v++) {
        for (int q = 0; q < xSize; q++, v++) {
          tZ = SetQuad(trianglesZ, tZ, v, v + 1, v + ring, v + ring + 1);
        }
        for (int q = 0; q < zSize; q++, v++) {
          tX = SetQuad(trianglesX, tX, v, v + 1, v + ring, v + ring + 1);
        }
        for (int q = 0; q < xSize; q++, v++) {
          tZ = SetQuad(trianglesZ, tZ, v, v + 1, v + ring, v + ring + 1);
        }
        for (int q = 0; q < zSize - 1; q++, v++) {
          tX = SetQuad(trianglesX, tX, v, v + 1, v + ring, v + ring + 1);
        }
        tX = SetQuad(trianglesX, tX, v, v - ring + 1, v + ring, v + 1);
      }

      tY = CreateTopFace(trianglesY, tY, ring);
      tY = CreateBottomFace(trianglesY, tY, ring);

      List<int> indices = new List<int>(trianglesX.Length + trianglesY.Length + trianglesZ.Length);
      for (int i = 0; i < trianglesX.Length; i++) {
        indices.Add(trianglesX[i]);
      }
      for (int i = 0; i < trianglesY.Length; i++) {
        indices.Add(trianglesY[i]);
      }
      for (int i = 0; i < trianglesZ.Length; i++) {
        indices.Add(trianglesZ[i]);
      }

      mesh.indices = indices;
    }

    private int CreateTopFace(int[] triangles, int t, int ring) {
      int v = ring * ySize;
      for (int x = 0; x < xSize - 1; x++, v++) {
        t = SetQuad(triangles, t, v, v + 1, v + ring - 1, v + ring);
      }
      t = SetQuad(triangles, t, v, v + 1, v + ring - 1, v + 2);

      int vMin = ring * (ySize + 1) - 1;
      int vMid = vMin + 1;
      int vMax = v + 2;

      for (int z = 1; z < zSize - 1; z++, vMin--, vMid++, vMax++) {
        t = SetQuad(triangles, t, vMin, vMid, vMin - 1, vMid + xSize - 1);
        for (int x = 1; x < xSize - 1; x++, vMid++) {
          t = SetQuad(
            triangles, t,
            vMid, vMid + 1, vMid + xSize - 1, vMid + xSize);
        }
        t = SetQuad(triangles, t, vMid, vMax, vMid + xSize - 1, vMax + 1);
      }

      int vTop = vMin - 2;
      t = SetQuad(triangles, t, vMin, vMid, vTop + 1, vTop);
      for (int x = 1; x < xSize - 1; x++, vTop--, vMid++) {
        t = SetQuad(triangles, t, vMid, vMid + 1, vTop, vTop - 1);
      }
      t = SetQuad(triangles, t, vMid, vTop - 2, vTop, vTop - 1);

      return t;
    }

    private int CreateBottomFace(int[] triangles, int t, int ring) {
      int v = 1;
      int vMid = GetNumVerts() - (xSize - 1) * (zSize - 1);
      t = SetQuad(triangles, t, ring - 1, vMid, 0, 1);
      for (int x = 1; x < xSize - 1; x++, v++, vMid++) {
        t = SetQuad(triangles, t, vMid, vMid + 1, v, v + 1);
      }
      t = SetQuad(triangles, t, vMid, v + 2, v, v + 1);

      int vMin = ring - 2;
      vMid -= xSize - 2;
      int vMax = v + 2;

      for (int z = 1; z < zSize - 1; z++, vMin--, vMid++, vMax++) {
        t = SetQuad(triangles, t, vMin, vMid + xSize - 1, vMin + 1, vMid);
        for (int x = 1; x < xSize - 1; x++, vMid++) {
          t = SetQuad(
            triangles, t,
            vMid + xSize - 1, vMid + xSize, vMid, vMid + 1);
        }
        t = SetQuad(triangles, t, vMid + xSize - 1, vMax + 1, vMid, vMax);
      }

      int vTop = vMin - 1;
      t = SetQuad(triangles, t, vTop + 1, vTop, vTop + 2, vMid);
      for (int x = 1; x < xSize - 1; x++, vTop--, vMid++) {
        t = SetQuad(triangles, t, vTop, vTop - 1, vMid, vMid + 1);
      }
      t = SetQuad(triangles, t, vTop, vTop - 1, vMid, vTop - 2);

      return t;
    }

    private static int SetQuad(int[] triangles, int i, int v00, int v10, int v01, int v11) {
      triangles[i] = v00;
      triangles[i + 1] = triangles[i + 4] = v01;
      triangles[i + 2] = triangles[i + 3] = v10;
      triangles[i + 5] = v11;
      return i + 6;
    }

  }
}
