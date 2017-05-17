/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2017.                                 *
 * Leap Motion proprietary and  confidential.                                 *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

using System;
using UnityEngine;
using UnityEngine.Rendering;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Sprites;
#endif
using Leap.Unity.Attributes;

namespace Leap.Unity.GraphicalRenderer {

  [LeapGraphicTag("Sprite", 20)]
  [Serializable]
  public class LeapSpriteFeature : LeapGraphicFeature<LeapSpriteData> {

    [Delayed]
    [EditTimeOnly]
    public string propertyName = "_MainTex";

    [EditTimeOnly]
    public UVChannelFlags channel = UVChannelFlags.UV0;

#if UNITY_EDITOR
    public bool AreAllSpritesPacked() {
      foreach (var dataObj in featureData) {
        if (dataObj.sprite == null) continue;

        if (!dataObj.sprite.packed) {
          return false;
        }
      }
      return true;
    }

    public bool AreAllSpritesOnSameTexture() {
      Texture2D mainTex = null;
      foreach (var dataObj in featureData) {
        if (dataObj.sprite == null) continue;

        string atlasName;
        Texture2D atlasTex;
        Packer.GetAtlasDataForSprite(dataObj.sprite, out atlasName, out atlasTex);

        if (mainTex == null) {
          mainTex = atlasTex;
        } else {
          if (mainTex != atlasTex) {
            return false;
          }
        }
      }

      return true;
    }
#endif
  }
}
