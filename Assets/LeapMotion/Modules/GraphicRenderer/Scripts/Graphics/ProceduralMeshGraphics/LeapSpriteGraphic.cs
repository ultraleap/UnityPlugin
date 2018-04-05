/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2018.                                 *
 * Leap Motion proprietary and confidential.                                  *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

using UnityEngine;
using Leap.Unity.Query;

namespace Leap.Unity.GraphicalRenderer {

  /// <summary>
  /// The Sprite Graphic is a type of procedural mesh graphic that allows you to directly
  /// use sprite objects as meshes.  This component grabs mesh data directly from the sprite
  /// itself, and so supports non-rectangular meshes.
  /// </summary>
  [DisallowMultipleComponent]
  public class LeapSpriteGraphic : LeapMeshGraphicBase {

    public override void RefreshMeshData() {
      var spriteData = this.Sprite();
      if (spriteData == null || spriteData.sprite == null) {
        mesh = null;
        remappableChannels = 0;
        return;
      }

      var sprite = spriteData.sprite;

      if (mesh == null) {
        mesh = new Mesh();
      }

      mesh.name = "Sprite Mesh";
      mesh.hideFlags = HideFlags.HideAndDontSave;

      mesh.Clear(keepVertexLayout: false);
      mesh.vertices = sprite.vertices.Query().Select(v => (Vector3)v).ToArray();
      mesh.triangles = sprite.triangles.Query().Select(i => (int)i).ToArray();

      Vector2[] uvs;
      if (SpriteAtlasUtil.TryGetAtlasedUvs(sprite, out uvs)) {
        mesh.uv = uvs;
      }

      mesh.RecalculateBounds();

      //We are using atlas uvs, so no remapping allowed!
      remappableChannels = 0;
    }
  }
}
