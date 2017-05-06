/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2017.                                 *
 * Leap Motion proprietary and  confidential.                                 *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

using UnityEngine;
using Leap.Unity.Query;

namespace Leap.Unity.GraphicalRenderer {

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

      mesh = new Mesh();
      mesh.name = "Sprite Mesh";
      mesh.hideFlags = HideFlags.HideAndDontSave;
      mesh.vertices = sprite.vertices.Query().Select(v => (Vector3)v).ToArray();
      mesh.triangles = sprite.triangles.Query().Select(i => (int)i).ToArray();
      mesh.uv = SpriteAtlasUtil.GetAtlasedUvs(sprite);
      mesh.RecalculateBounds();

      //We are using atlas uvs, so no remapping allowed!
      remappableChannels = 0;
    }
  }
}
