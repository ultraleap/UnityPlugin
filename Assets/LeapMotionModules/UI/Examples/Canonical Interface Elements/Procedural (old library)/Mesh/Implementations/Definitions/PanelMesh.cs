using UnityEngine;
using System;
using Leap.Unity.Attributes;

namespace Procedural.DynamicMesh {

  public class PanelMesh : MeshBehaviour<PanelDef> { }

  [Serializable]
  public struct PanelDef : IMeshDef {
    [MinValue(0)]
    public float width;

    [MinValue(0)]
    public float height;

    [MinValue(2)]
    public int horizontalResolution;

    [MinValue(2)]
    public int verticalResolution;

    public bool nineSlice;

    [MinValue(0)]
    public float edgeSize;

    [Range(0, 0.5f)]
    public float edgeUvSize;

    public void Generate(RawMesh mesh) {
      mesh.PreallocateTris((horizontalResolution - 1) * (verticalResolution - 1) * 2);
      mesh.PreallocateVerts(horizontalResolution * verticalResolution, VertexAttributes.Uv0 | VertexAttributes.Uv1);

      float left = -width * 0.5f;
      float right = width * 0.5f;
      float bot = -height * 0.5f;
      float top = height * 0.5f;

      for (int dy = 0; dy < verticalResolution; dy++) {
        for (int dx = 0; dx < horizontalResolution; dx++) {
          Vector2 pos;

          if (nineSlice) {
            float uvX, uvY;

            if (dx == 0) {
              pos.x = left;
              uvX = 0;
            } else if (dx == horizontalResolution - 1) {
              pos.x = right;
              uvX = 1;
            } else {
              float xPercent = (dx - 1.0f) / (horizontalResolution - 3.0f);
              pos.x = Mathf.Lerp(left + edgeSize, right - edgeSize, xPercent);
              uvX = Mathf.Lerp(edgeUvSize, 1.0f - edgeUvSize, xPercent);
            }

            if (dy == 0) {
              pos.y = bot;
              uvY = 0;
            } else if (dy == verticalResolution - 1) {
              pos.y = top;
              uvY = 1;
            } else {
              float yPercent = (dy - 1.0f) / (verticalResolution - 3.0f);
              pos.y = Mathf.Lerp(bot + edgeSize, top - edgeSize, yPercent);
              uvY = Mathf.Clamp01(Mathf.Lerp(edgeUvSize, 1.0f - edgeUvSize, yPercent));
            }

            mesh.uv0.Add(new Vector2(uvX, uvY));
            mesh.uv1.Add(new Vector2(Mathf.InverseLerp(left, right, pos.x), Mathf.InverseLerp(bot, top, pos.y)));
          } else {
            float uvX = dx / (horizontalResolution - 1.0f);
            float uvY = dy / (verticalResolution - 1.0f);

            pos.x = width * (uvX - 0.5f);
            pos.y = height * (uvY - 0.5f);

            mesh.uv0.Add(new Vector2(uvX, uvY));
          }

          if (dy != 0 && dx != 0) {
            mesh.indexes.Add(mesh.verts.Count);
            mesh.indexes.Add(mesh.verts.Count - horizontalResolution);
            mesh.indexes.Add(mesh.verts.Count - 1);

            mesh.indexes.Add(mesh.verts.Count - 1);
            mesh.indexes.Add(mesh.verts.Count - horizontalResolution);
            mesh.indexes.Add(mesh.verts.Count - 1 - horizontalResolution);
          }

          mesh.verts.Add(pos);
        }
      }
    }
  }
}
