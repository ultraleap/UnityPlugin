using UnityEngine;
using System;

namespace Procedural.DynamicMesh {

  public class CubeMesh : MeshBehaviour<CubeDef> { }

  [Serializable]
  public struct CubeDef : IMeshDef {
    public Vector3 extents;

    public void Generate(RawMesh mesh) {
      QuadIsland.AddQuads(mesh, 6);

      mesh.PreallocateVerts(4 * 6, VertexAttributes.Uv0);

      Vector3 x = new Vector3(extents.x, 0, 0);
      Vector3 y = new Vector3(0, extents.y, 0);
      Vector3 z = new Vector3(0, 0, extents.z);
      QuadIsland.AddVerts(mesh, x, y, z);
      QuadIsland.AddVerts(mesh, -x, z, y);
      QuadIsland.AddVerts(mesh, y, z, x);
      QuadIsland.AddVerts(mesh, -y, x, z);
      QuadIsland.AddVerts(mesh, z, x, y);
      QuadIsland.AddVerts(mesh, -z, y, x);

      QuadIsland.AddUvs(mesh, 0);
      QuadIsland.AddUvs(mesh, 0);
      QuadIsland.AddUvs(mesh, 0);
      QuadIsland.AddUvs(mesh, 0);
      QuadIsland.AddUvs(mesh, 0);
      QuadIsland.AddUvs(mesh, 0);
    }
  }
}
