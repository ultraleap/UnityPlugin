using System;
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
    Vector4 border = Vector4.zero;

    if (_nineSliced) {
      var spriteData = meshFeature.element.Sprite();
      if (spriteData == null || spriteData.sprite == null) {
        mesh = null;
        remappableChannels = 0;
        return false;
      }

      border = spriteData.sprite.border / spriteData.sprite.pixelsPerUnit;
    }

    List<Vector3> verts = new List<Vector3>();
    List<Vector2> uvs = new List<Vector2>();
    List<int> tris = new List<int>();

    int vertsX = _resolutionX + (_nineSliced ? 4 : 2);
    int vertsY = _resolutionY + (_nineSliced ? 4 : 2);

    float width = 1;
    float height = 1;

    for (int vx = 0; vx < vertsX; vx++) {
      for (int vy = 0; vy < vertsY; vy++) {
        float x = calculateVertAxis(vx, vertsX, width, border.x, border.z);
        float y = calculateVertAxis(vy, vertsY, height, border.y, border.w);

        verts.Add(new Vector3(x, y, 0));
      }
    }



    throw new Exception();
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
        return ((dv - 1.0f) / (vertCount - 2.0f)) * (size - border0 - border1) + border0;
      }
    } else {
      return (dv / (vertCount - 1.0f)) * size;
    }
  }
}
