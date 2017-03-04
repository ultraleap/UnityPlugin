using UnityEngine;
using System.Collections.Generic;

namespace Procedural.DynamicMesh {

  public static class QuadIsland {
    public static void AddQuad(RawMesh mesh, int vertOffset, int v0, int v1, int v2, int v3) {
      mesh.indexes.Add(vertOffset + v0);
      mesh.indexes.Add(vertOffset + v1);
      mesh.indexes.Add(vertOffset + v3);

      mesh.indexes.Add(vertOffset + v1);
      mesh.indexes.Add(vertOffset + v2);
      mesh.indexes.Add(vertOffset + v3);
    }

    public static void AddQuads(RawMesh mesh, int quadNumber) {
      var list = mesh.indexes;
      int v = mesh.verts.Count;
      for (int i = quadNumber; i-- != 0;) {
        list.Add(v);
        list.Add(v + 1);
        list.Add(v + 3);

        list.Add(v + 1);
        list.Add(v + 2);
        list.Add(v + 3);

        v += 4;
      }
    }

    public static void AddVerts(RawMesh mesh, Vector3 center, Vector3 primaryExtent, Vector3 secondaryExtent) {
      mesh.verts.Add(center + primaryExtent + secondaryExtent);
      mesh.verts.Add(center - primaryExtent + secondaryExtent);
      mesh.verts.Add(center - primaryExtent - secondaryExtent);
      mesh.verts.Add(center + primaryExtent - secondaryExtent);
    }

    private static Vector2[] quadUvs = new Vector2[] { Vector2.zero, Vector2.right, Vector2.one, Vector2.up, Vector2.zero, Vector2.right, Vector2.one };
    public static void AddUvs(RawMesh mesh, int offset) {
      mesh.uv0.Add(quadUvs[offset++]);
      mesh.uv0.Add(quadUvs[offset++]);
      mesh.uv0.Add(quadUvs[offset++]);
      mesh.uv0.Add(quadUvs[offset++]);
    }
  }

  public static class TriMeshExtensions {

    public static void AddTri(this List<int> indices, int idx1, int idx2, int idx3) {
      indices.Add(idx1); indices.Add(idx2); indices.Add(idx3);
    }
    public static void AddTri(this List<int> indices, int vertexOffset, int idx1, int idx2, int idx3) {
      indices.Add(vertexOffset + idx1); indices.Add(vertexOffset + idx2); indices.Add(vertexOffset + idx3);
    }

    public static void AddVert(this List<Vector3> verts, float x, float y, float z) {
      verts.Add(new Vector3(x, y, z));
    }

  }

}
