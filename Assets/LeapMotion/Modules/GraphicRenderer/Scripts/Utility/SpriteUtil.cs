/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2018.                                 *
 * Leap Motion proprietary and confidential.                                  *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Sprites;
#endif
using System;
using System.Collections.Generic;

namespace Leap.Unity.GraphicalRenderer {

  public static class SpriteAtlasUtil {

#if UNITY_EDITOR
    public static void ShowInvalidSpriteWarning(IList<LeapGraphicFeatureBase> features) {
      bool anyRectsInvalid = false;
      foreach (var feature in features) {
        var spriteFeature = feature as LeapSpriteFeature;
        if (spriteFeature == null) continue;

        foreach (var spriteData in spriteFeature.featureData) {
          var sprite = spriteData.sprite;
          if (sprite == null) continue;

          Rect rect;
          if (TryGetAtlasedRect(sprite, out rect)) {
            if (rect.Area() == 0) {
              anyRectsInvalid = true;
            }
          }
        }
      }

      if (anyRectsInvalid) {
        EditorGUILayout.HelpBox("Due to a Unity bug, packed sprites may be invalid until " +
                                "PlayMode has been entered at least once.", MessageType.Warning);
      }
    }
#endif

    public static bool TryGetAtlasedRect(Sprite sprite, out Rect rect) {
      Vector2[] uvs;
      if (!TryGetAtlasedUvs(sprite, out uvs)) {
        rect = default(Rect);
        return false;
      }

      float minX, minY, maxX, maxY;
      minX = maxX = uvs[0].x;
      minY = maxY = uvs[0].y;

      for (int j = 1; j < uvs.Length; j++) {
        minX = Mathf.Min(minX, uvs[j].x);
        minY = Mathf.Min(minY, uvs[j].y);
        maxX = Mathf.Max(maxX, uvs[j].x);
        maxY = Mathf.Max(maxY, uvs[j].y);
      }

      rect = Rect.MinMaxRect(minX, minY, maxX, maxY);
      return true;
    }

    public static bool TryGetAtlasedUvs(Sprite sprite, out Vector2[] uvs) {
#if UNITY_EDITOR
      if (!Application.isPlaying)
        return tryGetAtlasedUvsEditor(sprite, out uvs);
      else
#endif
        return tryGetAtlasedUvs(sprite, out uvs);
    }

    private static bool tryGetAtlasedUvs(Sprite sprite, out Vector2[] uvs) {
      if (sprite.packed) {
        uvs = sprite.uv;
        return true;
      } else {
        uvs = null;
        return false;
      }
    }

#if UNITY_EDITOR
    private static bool tryGetAtlasedUvsEditor(Sprite sprite, out Vector2[] uvs) {
      try {
        uvs = SpriteUtility.GetSpriteUVs(sprite, getAtlasData: true);
        return true;
      } catch (Exception) {
        uvs = null;
        return false;
      }
    }
#endif
  }
}
