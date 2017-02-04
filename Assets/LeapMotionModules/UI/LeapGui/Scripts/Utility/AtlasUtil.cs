using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using Leap.Unity.Query;

public static class AtlasUtil {
  public static List<UVChannelFlags> allUvChannels;
  static AtlasUtil() {
    allUvChannels = new List<UVChannelFlags>();
    allUvChannels.Add(UVChannelFlags.UV0);
    allUvChannels.Add(UVChannelFlags.UV1);
    allUvChannels.Add(UVChannelFlags.UV2);
    allUvChannels.Add(UVChannelFlags.UV3);
  }

  public static void ClearCache() {
    _cachedBorderedTextures.Clear();
  }

  public static void DoAtlas(List<LeapGuiTextureFeature> textureFeatures,
                             int borderAmount,
                         out Texture2D[] atlasTextures,
                         out Dictionary<UVChannelFlags, Rect[]> channelMapping) {
    ClearCache();

    atlasTextures = new Texture2D[textureFeatures.Count];
    channelMapping = new Dictionary<UVChannelFlags, Rect[]>();

    Texture2D[] textureArray = new Texture2D[textureFeatures[0].data.Count];

    foreach (var channel in allUvChannels) {
      var mainTexture = textureFeatures.Query().FirstOrDefault(f => f.channel == channel);
      if (mainTexture == null) continue;

      Texture2D defaultTexture, atlasTexture;
      prepFeatureForAtlas(mainTexture, borderAmount, textureArray, out defaultTexture, out atlasTexture);

      var atlasedRects = atlasTexture.PackTextures(textureArray,
                                                   padding: 0,
                                                   maximumAtlasSize: 4096,
                                                   makeNoLongerReadable: true);

      //Correct uvs to account for the added border
      for (int i = 0; i < atlasedRects.Length; i++) {
        float dx = 1.0f / atlasTexture.width;
        float dy = 1.0f / atlasTexture.height;
        Rect r = atlasedRects[i];

        if (textureArray[i] != defaultTexture) {
          dx *= borderAmount;
          dy *= borderAmount;
        }

        r.x += dx;
        r.y += dy;
        r.width -= dx * 2;
        r.height -= dy * 2;
        atlasedRects[i] = r;
      }

      atlasTextures[textureFeatures.IndexOf(mainTexture)] = atlasTexture;
      channelMapping[channel] = atlasedRects;

      //All texture features that are NOT the main texture do not get their own atlas step
      //They are simply copied into a new texture
      //var nonMainTextures = textureFeatures.Query().Where(f => f.channel == channel).Skip(1).ToList();
      //TODO: much lower priority ;)
    }
  }

  private static void prepFeatureForAtlas(LeapGuiTextureFeature feature,
                                          int borderAmount,
                                          Texture2D[] textureArray,
                                      out Texture2D defaultTexture,
                                      out Texture2D atlasTexture) {
    feature.data.Query().Select(dataObj => getBordered(dataObj.texture, borderAmount)).FillArray(textureArray);

    var nonNullTexture = feature.data.Query().Select(d => d.texture).FirstOrDefault(t => t != null);
    TextureFormat format;
    if (nonNullTexture == null) { //welp
      format = TextureFormat.ARGB32;
    } else {
      format = nonNullTexture.format;
    }

    defaultTexture = getDefaultTexture(format);
    for (int i = 0; i < textureArray.Length; i++) {
      if (textureArray[i] == null) {
        textureArray[i] = defaultTexture;
      }
    }

    atlasTexture = new Texture2D(1, 1, format, mipmap: false);
  }

  private static Dictionary<BorderKey, Texture2D> _cachedBorderedTextures = new Dictionary<BorderKey, Texture2D>();
  private static Texture2D getBordered(Texture2D source, int border) {
    if (source == null) {
      return null;
    }

    Texture2D bordered;
    BorderKey key = new BorderKey() { texture = source, border = border };
    if (!_cachedBorderedTextures.TryGetValue(key, out bordered)) {
      source.EnsureReadWriteEnabled();
      bordered = Object.Instantiate(source);
      bordered.AddBorder(border);
      _cachedBorderedTextures[key] = bordered;
    }
    return bordered;
  }

  private static Dictionary<TextureFormat, Texture2D> _cachedDefaultTextures = new Dictionary<TextureFormat, Texture2D>();
  private static Texture2D getDefaultTexture(TextureFormat format) {
    Texture2D texture;
    if (!_cachedDefaultTextures.TryGetValue(format, out texture)) {
      texture = new Texture2D(3, 3, format, mipmap: false);
      texture.SetPixels(new Color[3 * 3].Fill(Color.white));
      _cachedDefaultTextures[format] = texture;
    }
    return texture;
  }

  private struct BorderKey {
    public Texture2D texture;
    public int border;
  }
}
