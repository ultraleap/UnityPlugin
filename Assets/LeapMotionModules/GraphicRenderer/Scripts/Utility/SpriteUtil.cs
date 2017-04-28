using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Sprites;
#endif
using System.Collections.Generic;
using Leap.Unity.Query;

namespace Leap.Unity.GraphicalRenderer {

  public static class SpriteAtlasUtil {

#if UNITY_EDITOR
    public static void ShowInvalidSpriteWarning(List<LeapGraphicFeatureBase> features) {
      var anyRectsInvalid = features.Query().
                                     OfType<LeapSpriteFeature>().
                                     SelectMany(f => f.featureData.Query().
                                                                   Select(d => d.sprite).
                                                                   NonNull()).
                                     Select(s => SpriteAtlasUtil.GetAtlasedRect(s)).
                                     Any(r => r.Area() == 0);

      if (anyRectsInvalid) {
        EditorGUILayout.HelpBox("Due to a Unity bug, packed sprites may be invalid until " +
                                "PlayMode has been entered at least once.", MessageType.Warning);
      }
    }
#endif

    public static Rect GetAtlasedRect(Sprite sprite) {
      Vector2[] uvs = GetAtlasedUvs(sprite);

      float minX, minY, maxX, maxY;
      minX = maxX = uvs[0].x;
      minY = maxY = uvs[0].y;

      for (int j = 1; j < uvs.Length; j++) {
        minX = Mathf.Min(minX, uvs[j].x);
        minY = Mathf.Min(minY, uvs[j].y);
        maxX = Mathf.Max(maxX, uvs[j].x);
        maxY = Mathf.Max(maxY, uvs[j].y);
      }

      return Rect.MinMaxRect(minX, minY, maxX, maxY);
    }

    public static Vector2[] GetAtlasedUvs(Sprite sprite) {
#if UNITY_EDITOR
      if (!Application.isPlaying)
        return getAtlasedUvsEditor(sprite);
      else
#endif
        return getAtlasedUvsRuntime(sprite);
    }

    private static Vector2[] getAtlasedUvsRuntime(Sprite sprite) {
      return sprite.uv;
    }

#if UNITY_EDITOR
    private static Vector2[] getAtlasedUvsEditor(Sprite sprite) {
      return SpriteUtility.GetSpriteUVs(sprite, getAtlasData: true);
    }
#endif
  }
}
