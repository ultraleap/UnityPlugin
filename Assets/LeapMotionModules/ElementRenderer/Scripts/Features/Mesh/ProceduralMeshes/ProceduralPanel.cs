using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using Leap.Unity.Attributes;

public class ProceduralPanel : ProceduralMeshSource {

  [Tooltip("The number of vertices along the X axis.")]
  [MinValue(0)]
  [SerializeField]
  private int _resolutionX = 8;

  [Tooltip("The number of vertices along the Y axis.")]
  [MinValue(0)]
  [SerializeField]
  private int _resolutionY = 8;

  [Tooltip("Uses sprite data to generate a nine sliced panel.")]
  [SerializeField]
  private bool _nineSliced = false;

  public override bool TryGenerateMesh(LeapGuiMeshData meshFeature,
                                   out Mesh mesh,
                                   out UVChannelFlags remappableChannels) {
    Vector4 borderSize = Vector4.zero;
    Vector4 borderUvs = Vector4.zero;

    Rect rect = new Rect();

    RectTransform t = GetComponent<RectTransform>();
    if (t != null) {
      rect = t.rect;
    }

    if (_nineSliced) {
      var spriteData = meshFeature.element.Sprite();
      if (spriteData == null || spriteData.sprite == null) {
        mesh = null;
        remappableChannels = 0;
        return false;
      }

      var sprite = spriteData.sprite;

      Vector4 border = sprite.border;
      borderSize = border / sprite.pixelsPerUnit;

      borderUvs = border;
      borderUvs.x /= sprite.textureRect.width;
      borderUvs.z /= sprite.textureRect.width;
      borderUvs.y /= sprite.textureRect.height;
      borderUvs.w /= sprite.textureRect.height;
    }

    List<Vector3> verts = new List<Vector3>();
    List<Vector2> uvs = new List<Vector2>();
    List<int> tris = new List<int>();

    int vertsX = _resolutionX + (_nineSliced ? 4 : 2);
    int vertsY = _resolutionY + (_nineSliced ? 4 : 2);

    for (int vy = 0; vy < vertsY; vy++) {
      for (int vx = 0; vx < vertsX; vx++) {
        Vector2 vert;
        vert.x = calculateVertAxis(vx, vertsX, rect.width, borderSize.x, borderSize.z);
        vert.y = calculateVertAxis(vy, vertsY, rect.height, borderSize.y, borderSize.w);
        verts.Add(vert + new Vector2(rect.x, rect.y));

        Vector2 uv;
        uv.x = calculateVertAxis(vx, vertsX, 1, borderUvs.x, borderUvs.z);
        uv.y = calculateVertAxis(vy, vertsY, 1, borderUvs.y, borderUvs.w);
        uvs.Add(uv);
      }
    }

    for (int vy = 0; vy < vertsY - 1; vy++) {
      for (int vx = 0; vx < vertsX - 1; vx++) {
        int vertIndex = vy * vertsX + vx;

        tris.Add(vertIndex);
        tris.Add(vertIndex + 1 + vertsX);
        tris.Add(vertIndex + 1);

        tris.Add(vertIndex);
        tris.Add(vertIndex + vertsX);
        tris.Add(vertIndex + 1 + vertsX);
      }
    }

    mesh = new Mesh();
    mesh.name = "Panel Mesh";
    mesh.hideFlags = HideFlags.HideAndDontSave;
    mesh.SetVertices(verts);
    mesh.SetTriangles(tris, 0);
    mesh.SetUVs(0, uvs); //TODO, how to get correct channel??
    mesh.RecalculateBounds();

    remappableChannels = UVChannelFlags.UV0;

    return true;
  }

  private float calculateVertAxis(int dv, int vertCount, float size, float border0, float border1) {
    if (_nineSliced) {
      if (dv == 0) {
        return 0;
      } else if (dv == (vertCount - 1)) {
        return size;
      } else if (dv == 1) {
        return border0;
      } else if (dv == (vertCount - 2)) {
        return size - border1;
      } else {
        return ((dv - 1.0f) / (vertCount - 3.0f)) * (size - border0 - border1) + border0;
      }
    } else {
      return (dv / (vertCount - 1.0f)) * size;
    }
  }
}
