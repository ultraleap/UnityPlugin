using UnityEngine;
using UnityEngine.Rendering;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Sprites;
#endif
using Leap.Unity.Attributes;

[AddComponentMenu("")]
[LeapGraphicTag("Sprite")]
public class LeapSpriteFeature : LeapGraphicFeature<LeapSpriteData> {
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

  public override void DrawFeatureEditor(Rect rect, bool isActive, bool isFocused) {
    Rect line = rect.SingleLine();

    propertyName = EditorGUI.TextField(line, "Property Name", propertyName);
    line = line.NextLine();

    channel = (UVChannelFlags)EditorGUI.EnumPopup(line, "Uv Channel", channel);
  }

  public override float GetEditorHeight() {
    return EditorGUIUtility.singleLineHeight * 2;
  }
#endif
}