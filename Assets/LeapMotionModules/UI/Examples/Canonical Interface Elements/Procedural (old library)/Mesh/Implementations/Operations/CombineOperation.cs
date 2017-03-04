using System.Collections.Generic;

namespace Procedural.DynamicMesh {

  public class CombineOperation : OperationBehaviour<CombineOp> { }

  public struct CombineOp : IMeshOp {
    public void Operate(List<RawMesh> input, out RawMesh output) {
      if (input.Count == 0) {
        output = null;
        return;
      }

      VertexAttributes allAttributes = VertexAttributes.None;
      int totalVerts = 0;
      int totalIndexes = 0;
      for (int i = 0; i < input.Count; i++) {
        RawMesh mesh = input[i];

        if (mesh.normals != null) allAttributes |= VertexAttributes.Normals;
        if (mesh.uv0 != null) allAttributes |= VertexAttributes.Uv0;
        if (mesh.uv1 != null) allAttributes |= VertexAttributes.Uv1;
        if (mesh.uv2 != null) allAttributes |= VertexAttributes.Uv2;
        if (mesh.colors != null) allAttributes |= VertexAttributes.Colors;

        totalIndexes += mesh.indexes.Count;
        totalVerts += mesh.verts.Count;
      }

      output = input[0];
      output.PreallocateVerts(totalVerts, allAttributes);
      output.PreallocateTris(totalIndexes);

      for (int i = 1; i < input.Count; i++) {
        RawMesh mesh = input[i];

        int vertOffset = output.verts.Count;
        for (int j = 0; j < mesh.indexes.Count; j++) {
          output.indexes.Add(mesh.indexes[j] + vertOffset);
        }


        copy(vertOffset, mesh.verts, output.verts);
        copy(vertOffset, mesh.normals, output.normals);
        copy(vertOffset, mesh.uv0, output.uv0);
        copy(vertOffset, mesh.uv1, output.uv1);
        copy(vertOffset, mesh.uv2, output.uv2);
        copy(vertOffset, mesh.colors, output.colors);
      }
    }

    private static void copy<T>(int count, List<T> src, List<T> dst) {
      if (dst == null) {
        return;
      }

      if (src == null) {
        for (int i = 0; i < count; i++) {
          dst.Add(default(T));
        }
      } else {
        dst.AddRange(src);
      }
    }
  }
}
