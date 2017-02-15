using UnityEngine;
#if UNITY_EDITOR
using UnityEditor.Sprites;
#endif


public static class SpriteAtlasUtil {

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
