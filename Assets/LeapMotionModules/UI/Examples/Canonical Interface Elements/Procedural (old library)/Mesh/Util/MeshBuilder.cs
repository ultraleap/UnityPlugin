using UnityEngine;
using System;

namespace Procedural.DynamicMesh {

  [Serializable]
  public class MeshBuilder {
    public bool upload = false;
    public bool infiniteBounds = false;
    public bool generateNormals = true;
    public bool generateLightmapUvs;

    public void Build(RawMesh rawMesh, Mesh mesh) {
      mesh.Clear();

      mesh.SetVertices(rawMesh.verts);
      mesh.SetIndices(rawMesh.indexes.ToArray(), rawMesh.topology, 0);

      if (rawMesh.uv0 != null) {
        mesh.SetUVs(0, rawMesh.uv0);
      }

      if (rawMesh.uv1 != null) {
        mesh.SetUVs(1, rawMesh.uv1);
      }

      if (rawMesh.uv2 != null) {
        mesh.SetUVs(2, rawMesh.uv2);
      }

      if (rawMesh.colors != null) {
        mesh.SetColors(rawMesh.colors);
      }

      if (rawMesh.normals != null) {
        mesh.SetNormals(rawMesh.normals);
      } else if (generateNormals) {
        mesh.RecalculateNormals();
      }
      mesh.RecalculateNormals();

      if (infiniteBounds) {
        mesh.bounds = new Bounds(Vector3.zero, Vector3.one * float.MaxValue);
      }

      if (upload) {
        mesh.UploadMeshData(true);
      }
    }
  }
}
