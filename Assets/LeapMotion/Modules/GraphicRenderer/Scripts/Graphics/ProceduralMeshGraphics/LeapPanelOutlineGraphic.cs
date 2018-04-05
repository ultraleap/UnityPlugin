/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2018.                                 *
 * Leap Motion proprietary and confidential.                                  *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using Leap.Unity.Query;
using Leap.Unity.Attributes;

namespace Leap.Unity.GraphicalRenderer {

  /// <summary>
  /// The Panel Outline Graphic acts just like a Panel Graphic, but only produces quads
  /// for the outermost edges of the panel.
  /// </summary>
  [DisallowMultipleComponent]
  public class LeapPanelOutlineGraphic : LeapSlicedGraphic {

    #region Inspector

    [Header("Outline Thickness")]

    [Tooltip("Check this option to override the thickness of the sprite's borders with "
           + "a custom thickness. This will stretch the sprite's borders to match the "
           + "custom thickness.")]
    [SerializeField, EditTimeOnly]
    private bool _overrideSpriteBorders = false;
    /// <summary>
    /// Gets whether the Panel Outline Graphic is overriding the borders of its sprite
    /// data source. This property only has an effect if the graphic has a sprite set as
    /// its data source (instead of a texture).
    /// </summary>
    public bool overrideSpriteBorders {
      get {
        return _overrideSpriteBorders;
      }
    }

    [Tooltip("The local-space thickness of the panel edges that are constructed. If the"
           + "source data for the panel is a Sprite, this value is overridden to reflect "
           + "the sprite's nine-slicing.")]
    [MinValue(0f)]
    [SerializeField, EditTimeOnly]
    private Vector2 _thickness = new Vector2(0.01F, 0.01F);
    /// <summary>
    /// Gets the thickness of the panel edges.
    /// </summary>
    public Vector2 thickness {
      get {
        return _thickness;
      }
    }

    #endregion

    #region Unity Events

    protected override void OnValidate() {
      base.OnValidate();

      RefreshMeshData();
    }

    #endregion

    public override void RefreshSlicedMeshData(Vector2i    resolution,
                                               RectMargins meshMargins,
                                               RectMargins uvMargins) {
      resolution.x = Mathf.Max(resolution.x, 4);
      resolution.y = Mathf.Max(resolution.y, 4);

      if (overrideSpriteBorders || !nineSliced) {
        // For this calculation, we ignore the provided meshMargins because the Outline
        // graphic allows the user to override its default nine-slicing behaviour for
        // borders.
        meshMargins = new RectMargins(_thickness.x, _thickness.y, _thickness.x, _thickness.y);
      }
      if (!nineSliced) {
        float xRatio = _thickness.x / rect.width;
        float yRatio = _thickness.y / rect.height;
        xRatio = Mathf.Clamp(xRatio, 0F, 0.5F);
        yRatio = Mathf.Clamp(yRatio, 0F, 0.5F);
        uvMargins = new RectMargins(left: xRatio, right: xRatio,
                                    top: yRatio, bottom: yRatio);
      }

      List<Vector3> verts = new List<Vector3>();
      List<Vector2> uvs = new List<Vector2>();
      List<int> tris = new List<int>();

      for (int vy = 0; vy < resolution.y; vy++) {
        for (int vx = 0; vx < resolution.x; vx++) {
          // Outline verts only.
          if ((vy > 1 && vy < resolution.y - 2) && (vx > 1 && vx < resolution.x - 2)) continue;

          Vector2 vert;
          vert.x = calculateVertAxis(vx, resolution.x, rect.width, meshMargins.left, meshMargins.right, true);
          vert.y = calculateVertAxis(vy, resolution.y, rect.height, meshMargins.top, meshMargins.bottom, true);
          verts.Add(vert + new Vector2(rect.x, rect.y));

          Vector2 uv;
          uv.x = calculateVertAxis(vx, resolution.x, 1, uvMargins.left, uvMargins.right, true);
          uv.y = calculateVertAxis(vy, resolution.y, 1, uvMargins.top, uvMargins.bottom, true);
          uvs.Add(uv);
        }
      }

      int indicesSkippedPerRow = resolution.x - 4;
      int indicesSkipped = 0;
      for (int vy = 0; vy < resolution.y; vy++) {
        for (int vx = 0; vx < resolution.x; vx++) {

          if (vx == resolution.x - 1 || vy == resolution.y - 1) {
            continue;
          }
          if ((vx == 1 && (vy > 0 && vy < resolution.y - 2)) || (vy == 1 && (vx > 0 && vx < resolution.x - 2))) {
            continue;
          }
          if ((vx > 1 && vx < resolution.x - 2) && (vy > 1 && vy < resolution.y - 2)) {
            indicesSkipped += 1;
            continue;
          }

          int vertIndex = vy * resolution.x + vx - indicesSkipped;

          int right = 1;
          int down;
          if (vy == 0 || (vy == 1 && vx < 2) || (vy == resolution.y - 3 && vx > resolution.x - 3) || (vy == resolution.y - 2)) {
            down = resolution.x;
          }
          else {
            down = resolution.x - indicesSkippedPerRow;
          }

          // Add quad
          tris.Add(vertIndex);
          tris.Add(vertIndex + right + down);
          tris.Add(vertIndex + right);

          tris.Add(vertIndex);
          tris.Add(vertIndex + down);
          tris.Add(vertIndex + right + down);
        }
      }

      if (mesh == null) {
        mesh = new Mesh();
      }

      mesh.name = "Panel Outline Mesh";
      mesh.hideFlags = HideFlags.HideAndDontSave;

      mesh.Clear(keepVertexLayout: false);
      mesh.SetVertices(verts);
      mesh.SetTriangles(tris, 0);
      mesh.SetUVs(uvChannel.Index(), uvs);
      mesh.RecalculateBounds();

      remappableChannels = UVChannelFlags.UV0;
    }

  }
}
