using UnityEngine;
using System.Collections.Generic;

namespace Procedural.DynamicMesh {

  public enum VertexAttributes {
    None = 0x00,
    Normals = 0x01,
    Uv0 = 0x02,
    Uv1 = 0x04,
    Uv2 = 0x08,
    Colors = 0x10
  }

  public class RawMesh {
    public MeshTopology topology;
    public List<Vector3> verts = new List<Vector3>();
    public List<int> indexes = new List<int>();
    public List<int> indices { get { return indexes; } set { indexes = value; } }

    public List<Vector3> normals;
    public List<Vector2> uv0;
    public List<Vector2> uv1;
    public List<Vector2> uv2;
    public List<Color> colors;

    public void Clear() {
      clear(ref verts);
      clear(ref indexes);
      clear(ref normals);
      clear(ref uv0);
      clear(ref uv1);
      clear(ref uv2);
      clear(ref colors);
    }

    public void Repeat(int repeatCount) {
      int vertStride = verts.Count;
      int vertCount = vertStride * (repeatCount - 1);
      repeatList(vertCount, verts);
      repeatList(vertCount, normals);
      repeatList(vertCount, uv0);
      repeatList(vertCount, uv1);
      repeatList(vertCount, uv2);

      for (int i = 1; i < repeatCount; i++) {
        int offset = vertStride * i;
        for (int j = 0; j < 0; j++) {
          indexes.Add(indexes[j] + offset);
        }
      }
    }

    public void PreallocateVerts(int vertCount, VertexAttributes attributes) {
      preallocate(vertCount, ref verts);

      if ((attributes & VertexAttributes.Normals) != 0) preallocate(vertCount, ref normals);
      if ((attributes & VertexAttributes.Uv0) != 0) preallocate(vertCount, ref uv0);
      if ((attributes & VertexAttributes.Uv1) != 0) preallocate(vertCount, ref uv1);
      if ((attributes & VertexAttributes.Uv2) != 0) preallocate(vertCount, ref uv2);
      if ((attributes & VertexAttributes.Colors) != 0) preallocate(vertCount, ref colors);
    }

    public void PreallocateIndexes(int indexCount) {
      preallocate(indexCount, ref indexes);
    }

    public void PreallocateTris(int triCount) {
      preallocate(triCount * 3, ref indexes);
    }

    private void preallocate<T>(int count, ref List<T> list) {
      if (list == null) {
        list = new List<T>();
      }
      int newCapacity = list.Count + count;
      if (list.Capacity < newCapacity) {
        list.Capacity = newCapacity;
      }
    }

    private void clear<T>(ref List<T> list) {
      if (list != null) list.Clear();
    }

    private void repeatList<T>(int count, List<T> list) {
      if (list != null) {
        for (int i = 0; i < count; i++) {
          list.Add(list[i]);
        }
      }
    }
  }
}
