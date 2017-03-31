using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using Leap.Unity.Query;
using Leap.Unity.Attributes;

namespace Leap.Unity.GraphicalRenderer {

  [Serializable]
  public class AtlasBuilder {

    [MinValue(0)]
    [EditTimeOnly, SerializeField]
    private int _border = 1;

    [MinValue(0)]
    [EditTimeOnly, SerializeField]
    private int _padding = 0;

    [EditTimeOnly, SerializeField]
    private bool _mipMap = true;

    [EditTimeOnly, SerializeField]
    private FilterMode _filterMode = FilterMode.Bilinear;

    [EditTimeOnly, SerializeField]
    private TextureFormat _format = TextureFormat.ARGB32;

    [MinValue(16)]
    [MaxValue(8192)]
    [EditTimeOnly, SerializeField]
    private int _maxAtlasSize = 4096;

    public int border {
      get {
        return _border;
      }
      set {
        if (_border != value) {
          _border = value;
          _currHash++;
        }
      }
    }

    public int padding {
      get {
        return _padding;
      }
      set {
        if (_padding != value) {
          _padding = value;
          _currHash++;
        }
      }
    }

    public int maxAtlasSize {
      get {
        return _maxAtlasSize;
      }
      set {
        if (_maxAtlasSize != value) {
          _maxAtlasSize = value;
          _currHash++;
        }
      }
    }

    public FilterMode filterMode {
      get {
        return _filterMode;
      }
      set {
        if (_filterMode != value) {
          _filterMode = value;
          _currHash++;
        }
      }
    }

    public bool isDirty {
      get {
        return _currHash != _atlasHash;
      }
    }

    private static Material _cachedBlitMaterial = null;
    private static void enableBlitPass(Texture tex) {
      if (_cachedBlitMaterial == null) {
        _cachedBlitMaterial = new Material(Shader.Find("Hidden/LeapGui/InternalPack"));
        _cachedBlitMaterial.hideFlags = HideFlags.HideAndDontSave;
      }
      _cachedBlitMaterial.mainTexture = tex;
      _cachedBlitMaterial.SetPass(0);
    }

    private List<LeapTextureFeature> _features = new List<LeapTextureFeature>();
    private Texture2D[] _textureArray;
    private Hash _atlasHash = 1;
    private Hash _currHash = 0;

    public void UpdateTextureList(List<LeapTextureFeature> textureFeatures) {
      _features.Clear();
      _features.AddRange(textureFeatures);

      _textureArray = new Texture2D[_features[0].featureData.Count];

      _currHash = new Hash() {
      _border,
      _padding,
      _mipMap,
      _filterMode,
      _format,
      _maxAtlasSize
    };

      foreach (var feature in _features) {
        _currHash.Add(feature.channel);
        foreach (var dataObj in feature.featureData) {
          _currHash.Add(dataObj.texture);
        }
      }
    }

    public void RebuildAtlas(ProgressBar progress, out Texture2D[] packedTextures, out Rect[][] channelMapping) {
      _atlasHash = _currHash;

      packedTextures = new Texture2D[_features.Count];
      channelMapping = new Rect[4][];

      mainProgressLoop(progress, packedTextures, channelMapping);

      //Clear cache
      foreach (var texture in _cachedProcessedTextures.Values) {
        UnityEngine.Object.DestroyImmediate(texture);
      }
      _cachedProcessedTextures.Clear();

      foreach (var texture in _cachedDefaultTextures.Values) {
        UnityEngine.Object.DestroyImmediate(texture);
      }
      _cachedDefaultTextures.Clear();
    }

    private void mainProgressLoop(ProgressBar progress, Texture2D[] packedTextures, Rect[][] channelMapping) {
      progress.Begin(5, "", "", () => {
        foreach (var channel in MeshUtil.allUvChannels) {
          progress.Begin(1, "", channel + ": ", () => {
            doPerChannelPack(progress, channel, packedTextures, channelMapping);
          });
        }

        finalizeTextures(progress, packedTextures);
      });
    }

    private void doPerChannelPack(ProgressBar progress, UVChannelFlags channel, Texture2D[] packedTextures, Rect[][] channelMapping) {
      var mainTextureFeature = _features.Query().FirstOrDefault(f => f.channel == channel);
      if (mainTextureFeature == null) return;

      Texture2D defaultTexture, packedTexture;
      progress.Step("Prepare " + channel);
      prepareForPacking(mainTextureFeature, out defaultTexture, out packedTexture);

      progress.Step("Pack " + channel);
      var packedRects = packedTexture.PackTextures(_textureArray,
                                                    _padding,
                                                    _maxAtlasSize,
                                                    makeNoLongerReadable: false);

      packedTexture.Apply(updateMipmaps: true, makeNoLongerReadable: true);
      packedTextures[_features.IndexOf(mainTextureFeature)] = packedTexture;
      channelMapping[channel.Index()] = packedRects;

      packSecondaryTextures(progress, channel, mainTextureFeature, packedTexture, packedRects, packedTextures);

      //Correct uvs to account for the added border
      for (int i = 0; i < packedRects.Length; i++) {
        float dx = 1.0f / packedTexture.width;
        float dy = 1.0f / packedTexture.height;
        Rect r = packedRects[i];

        if (_textureArray[i] != defaultTexture) {
          dx *= _border;
          dy *= _border;
        }

        r.x += dx;
        r.y += dy;
        r.width -= dx * 2;
        r.height -= dy * 2;
        packedRects[i] = r;
      }
    }

    private void packSecondaryTextures(ProgressBar progress, UVChannelFlags channel, LeapTextureFeature mainFeature, Texture2D packedTexture, Rect[] packedRects, Texture2D[] packedTextures) {
      //All texture features that are NOT the main texture do not get their own atlas step
      //They are simply copied into a new texture
      var nonMainFeatures = _features.Query().Where(f => f.channel == channel).Skip(1).ToList();

      progress.Begin(nonMainFeatures.Count, "", "Copying secondary textures: ", () => {
        foreach (var secondaryFeature in nonMainFeatures) {

          RenderTexture secondaryRT = new RenderTexture(packedTexture.width, packedTexture.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);

          RenderTexture.active = secondaryRT;
          GL.Clear(clearDepth: false, clearColor: true, backgroundColor: Color.black);
          GL.LoadPixelMatrix(0, 1, 0, 1);

          progress.Begin(secondaryFeature.featureData.Count, "", secondaryFeature.propertyName, () => {
            for (int i = 0; i < secondaryFeature.featureData.Count; i++) {
              var mainTexture = mainFeature.featureData[i].texture;
              if (mainTexture == null) {
                progress.Step();
                continue;
              }

              var secondaryTexture = secondaryFeature.featureData[i].texture;
              if (secondaryTexture == null) {
                progress.Step();
                continue;
              }

              progress.Step(secondaryTexture.name);

              Rect rect = packedRects[i];
              //Use mainTexture instead of secondaryTexture here to calculate correct border to line up with main texture
              float borderDX = _border / (float)mainTexture.width;
              float borderDY = _border / (float)mainTexture.height;

              drawTexture(secondaryTexture, secondaryRT, rect, borderDX, borderDY);
            }
          });

          packedTextures[_features.IndexOf(secondaryFeature)] = convertToTexture2D(secondaryRT, mipmap: false);
        }
      });
    }

    private void finalizeTextures(ProgressBar progress, Texture2D[] packedTextures) {
      progress.Begin(packedTextures.Length, "", "Finalizing ", () => {
        for (int i = 0; i < packedTextures.Length; i++) {
          progress.Begin(2, "", _features[i].propertyName + ": ", () => {
            Texture2D tex = packedTextures[i];
            RenderTexture rt = new RenderTexture(tex.width, tex.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);

            GL.LoadPixelMatrix(0, 1, 0, 1);
            drawTexture(tex, rt, new Rect(0, 0, 1, 1), 0, 0);

            progress.Step("Copying Texture");
            tex = convertToTexture2D(rt, _mipMap);

            progress.Step("Compressing Texture");
#if UNITY_EDITOR
            UnityEditor.EditorUtility.CompressTexture(tex, _format, TextureCompressionQuality.Best);
#endif
            tex.filterMode = _filterMode;

            progress.Step("Updating Texture");
            tex.Apply(updateMipmaps: true, makeNoLongerReadable: true);

            packedTextures[i] = tex;
          });
        }
      });
    }

    private void prepareForPacking(LeapTextureFeature feature,
                                      out Texture2D defaultTexture,
                                      out Texture2D packedTexture) {
      feature.featureData.Query().Select(dataObj => processTexture(dataObj.texture)).FillArray(_textureArray);

      defaultTexture = getDefaultTexture(Color.white); //TODO, pull color from feature data
      for (int i = 0; i < _textureArray.Length; i++) {
        if (_textureArray[i] == null) {
          _textureArray[i] = defaultTexture;
        }
      }

      packedTexture = new Texture2D(1, 1, TextureFormat.ARGB32, mipmap: false, linear: true);
      packedTexture.filterMode = _filterMode;
    }

    private Dictionary<Texture2D, Texture2D> _cachedProcessedTextures = new Dictionary<Texture2D, Texture2D>();
    private Texture2D processTexture(Texture2D source) {
      using (new ProfilerSample("Process Texture")) {
        if (source == null) {
          return null;
        }

        Texture2D processed;
        if (_cachedProcessedTextures.TryGetValue(source, out processed)) {
          return processed;
        }

        RenderTexture destRT = new RenderTexture(source.width + border * 2, source.height + border * 2, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);

        GL.LoadPixelMatrix(0, 1, 0, 1);
        drawTexture(source, destRT, new Rect(0, 0, 1, 1), border / (float)source.width, border / (float)source.height);

        processed = convertToTexture2D(destRT, mipmap: false);
        _cachedProcessedTextures[source] = processed;
        return processed;
      }
    }

    private void drawTexture(Texture2D source, RenderTexture dst, Rect rect, float borderDX, float borderDY) {
      enableBlitPass(source);
      RenderTexture.active = dst;

      GL.Begin(GL.QUADS);
      GL.TexCoord(new Vector2(0 - borderDX, 0 - borderDY));
      GL.Vertex(rect.Corner00());
      GL.TexCoord(new Vector2(1 + borderDX, 0 - borderDY));
      GL.Vertex(rect.Corner10());
      GL.TexCoord(new Vector2(1 + borderDX, 1 + borderDY));
      GL.Vertex(rect.Corner11());
      GL.TexCoord(new Vector2(0 - borderDX, 1 + borderDY));
      GL.Vertex(rect.Corner01());
      GL.End();
    }

    private Texture2D convertToTexture2D(RenderTexture source, bool mipmap) {
      Texture2D tex = new Texture2D(source.width, source.height, TextureFormat.ARGB32, mipmap, linear: true);

      RenderTexture.active = source;
      tex.ReadPixels(new Rect(0, 0, tex.width, tex.height), 0, 0);
      tex.Apply(updateMipmaps: false, makeNoLongerReadable: false);
      RenderTexture.active = null;

      source.Release();
      UnityEngine.Object.DestroyImmediate(source);

      return tex;
    }

    private Dictionary<Color, Texture2D> _cachedDefaultTextures = new Dictionary<Color, Texture2D>();
    private Texture2D getDefaultTexture(Color color) {
      Texture2D texture;
      if (!_cachedDefaultTextures.TryGetValue(color, out texture)) {
        texture = new Texture2D(3, 3, TextureFormat.ARGB32, mipmap: false);
        texture.SetPixels(new Color[3 * 3].Fill(color));
        _cachedDefaultTextures[color] = texture;
      }
      return texture;
    }
  }
}
