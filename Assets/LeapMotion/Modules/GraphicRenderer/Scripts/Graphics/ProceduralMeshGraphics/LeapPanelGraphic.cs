/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2017.                                 *
 * Leap Motion proprietary and  confidential.                                 *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Leap.Unity.GraphicalRenderer {

  /// <summary>
  /// The Panel Graphic is a type of procedural mesh graphic that can generate flat
  /// panels with a number of useful features:
  ///  - It allows nine slicing when using a sprite as the source for the texture data.
  ///  - It allows automatic tessellation such that it can be correctly warped by a space.
  ///  - It allows automatic resizing based on an attached RectTransform.
  /// </summary>
  [DisallowMultipleComponent]
  public class LeapPanelGraphic : LeapSlicedGraphic {

    public override void RefreshSlicedMeshData(Vector2i    resolution,
                                               RectMargins meshMargins,
                                               RectMargins uvMargins) {
      List<Vector3> verts = new List<Vector3>();
      List<Vector2> uvs = new List<Vector2>();
      List<int> tris = new List<int>();

      for (int vy = 0; vy < resolution.y; vy++) {
        for (int vx = 0; vx < resolution.x; vx++) {
          Vector2 vert;
          vert.x = calculateVertAxis(vx, resolution.x, rect.width, meshMargins.left, meshMargins.right);
          vert.y = calculateVertAxis(vy, resolution.y, rect.height, meshMargins.top, meshMargins.bottom);
          verts.Add(vert + new Vector2(rect.x, rect.y));

          Vector2 uv;
          uv.x = calculateVertAxis(vx, resolution.x, 1, uvMargins.left, uvMargins.right);
          uv.y = calculateVertAxis(vy, resolution.y, 1, uvMargins.top, uvMargins.bottom);
          uvs.Add(uv);
        }
      }

      for (int vy = 0; vy < resolution.y - 1; vy++) {
        for (int vx = 0; vx < resolution.x - 1; vx++) {
          int vertIndex = vy * resolution.x + vx;

          tris.Add(vertIndex);
          tris.Add(vertIndex + 1 + resolution.x);
          tris.Add(vertIndex + 1);

          tris.Add(vertIndex);
          tris.Add(vertIndex + resolution.x);
          tris.Add(vertIndex + 1 + resolution.x);
        }
      }

      mesh = new Mesh();
      mesh.name = "Panel Mesh";
      mesh.hideFlags = HideFlags.HideAndDontSave;
      mesh.SetVertices(verts);
      mesh.SetTriangles(tris, 0);
      mesh.SetUVs(uvChannel.Index(), uvs);
      mesh.RecalculateBounds();

      remappableChannels = UVChannelFlags.UV0;
    }
  }
}
